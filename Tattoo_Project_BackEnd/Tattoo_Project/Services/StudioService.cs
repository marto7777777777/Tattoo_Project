using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.StudioDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services
{
    public class StudioService(TattooDbContext context) : IStudioService
    {
        public async Task<ResultService<ICollection<StudioDto>>> GetStudiosAsync(string? query = null)
        {
            var studiosQuery = BaseStudioQuery();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var normalized = query.Trim().ToLower();
                studiosQuery = studiosQuery.Where(s =>
                    s.Name.ToLower().Contains(normalized) ||
                    s.City.ToLower().Contains(normalized) ||
                    s.Country.ToLower().Contains(normalized) ||
                    s.Address.ToLower().Contains(normalized) ||
                    s.Artists.Any(a =>
                        a.FirstName.ToLower().Contains(normalized) ||
                        a.LastName.ToLower().Contains(normalized)));
            }

            var studios = await studiosQuery
                .Where(s => s.Artists.Any())
                .OrderBy(s => s.Name)
                .ThenBy(s => s.City)
                .ToListAsync();

            return ResultService<ICollection<StudioDto>>.Ok(studios.Select(MapStudio).ToList());
        }

        public async Task<ResultService<ICollection<StudioDto>>> SearchOpenStudiosForJoinAsync(string? query, string userId)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            {
                return ResultService<ICollection<StudioDto>>.Ok(new List<StudioDto>());
            }

            var existingArtist = await context.TattooArtists.AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (existingArtist?.StudioId != null)
            {
                return ResultService<ICollection<StudioDto>>.Fail("You already belong to a studio.");
            }

            var normalized = query.Trim().ToLower();
            var studios = await BaseStudioQuery()
                .Where(s => s.IsOpenForJoinRequests)
                .Where(s =>
                    s.Name.ToLower().Contains(normalized) ||
                    s.City.ToLower().Contains(normalized) ||
                    s.Country.ToLower().Contains(normalized))
                .OrderBy(s => s.Name)
                .ThenBy(s => s.City)
                .Take(30)
                .ToListAsync();

            return ResultService<ICollection<StudioDto>>.Ok(studios.Select(MapStudio).ToList());
        }

        public async Task<ResultService<StudioDto>> GetStudioByIdAsync(int studioId)
        {
            var studio = await BaseStudioQuery().FirstOrDefaultAsync(s => s.Id == studioId);
            if (studio == null)
            {
                return ResultService<StudioDto>.Fail("Studio was not found.");
            }

            return ResultService<StudioDto>.Ok(MapStudio(studio));
        }

        public async Task<ResultService<MyStudioDto>> GetMyStudioAsync(string userId)
        {
            var artist = await context.TattooArtists
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (artist == null)
            {
                return ResultService<MyStudioDto>.Fail("Tattoo artist profile was not found.");
            }

            var latestRequest = await context.StudioJoinRequests
                .AsNoTracking()
                .Include(r => r.Studio)
                .Include(r => r.TattooArtist).ThenInclude(a => a.User)
                .Where(r => r.TattooArtistId == artist.Id)
                .OrderByDescending(r => r.CreatedOn)
                .FirstOrDefaultAsync();

            if (artist.StudioId == null)
            {
                return ResultService<MyStudioDto>.Ok(new MyStudioDto
                {
                    CurrentArtistId = artist.Id,
                    HasStudio = false,
                    IsOwner = false,
                    PendingJoinRequest = latestRequest?.Status == StudioJoinRequestStatus.Pending ? MapJoinRequest(latestRequest) : null,
                    LatestJoinRequest = latestRequest == null ? null : MapJoinRequest(latestRequest)
                });
            }

            var studio = await BaseStudioQuery().FirstOrDefaultAsync(s => s.Id == artist.StudioId.Value);
            if (studio == null)
            {
                return ResultService<MyStudioDto>.Fail("Your studio could not be found.");
            }

            var isOwner = studio.OwnerArtistId == artist.Id;
            var pendingRequests = new List<StudioJoinRequestDto>();

            if (isOwner)
            {
                var requests = await context.StudioJoinRequests
                    .AsNoTracking()
                    .Include(r => r.Studio)
                    .Include(r => r.TattooArtist).ThenInclude(a => a.User)
                    .Where(r => r.StudioId == studio.Id && r.Status == StudioJoinRequestStatus.Pending)
                    .OrderBy(r => r.CreatedOn)
                    .ToListAsync();

                pendingRequests = requests.Select(MapJoinRequest).ToList();
            }

            return ResultService<MyStudioDto>.Ok(new MyStudioDto
            {
                CurrentArtistId = artist.Id,
                HasStudio = true,
                IsOwner = isOwner,
                Studio = MapStudio(studio),
                PendingRequests = pendingRequests
            });
        }

        public async Task<ResultService> CreateStudioForExistingArtistAsync(CreateStudioDto dto, string userId)
        {
            var artist = await context.TattooArtists.FirstOrDefaultAsync(a => a.UserId == userId);
            if (artist == null) return ResultService.Fail("Tattoo artist profile was not found.");
            if (artist.StudioId != null) return ResultService.Fail("You already belong to a studio.");

            var pendingExists = await context.StudioJoinRequests.AnyAsync(r =>
                r.TattooArtistId == artist.Id && r.Status == StudioJoinRequestStatus.Pending);
            if (pendingExists) return ResultService.Fail("Cancel or wait for your pending studio join request before creating a studio.");

            var validation = ValidateStudio(dto);
            if (!validation.Success) return validation;

            var name = dto.Name.Trim();
            var city = dto.City.Trim();
            var address = dto.Address.Trim();
            var duplicateStudio = await context.Studios.AnyAsync(s =>
                s.Name.ToLower() == name.ToLower() &&
                s.City.ToLower() == city.ToLower() &&
                s.Address.ToLower() == address.ToLower());
            if (duplicateStudio) return ResultService.Fail("A studio with the same name and address already exists.");

            await using var transaction = await context.Database.BeginTransactionAsync();
            var now = DateTime.UtcNow;
            var studio = new Studio
            {
                Name = name, Description = dto.Description.Trim(), Address = address, City = city,
                Country = dto.Country.Trim(), Latitude = dto.Latitude, Longitude = dto.Longitude,
                IsOpenForJoinRequests = true, CreatedOn = now, OwnerArtistId = artist.Id
            };
            context.Studios.Add(studio);
            await context.SaveChangesAsync();
            artist.StudioId = studio.Id;
            artist.JoinedStudioOn = now;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService> RequestJoinAsync(int studioId, string userId)
        {
            var artist = await context.TattooArtists.FirstOrDefaultAsync(a => a.UserId == userId);
            if (artist == null)
            {
                return ResultService.Fail("Create your tattoo artist profile before joining a studio.");
            }

            if (artist.StudioId != null)
            {
                return ResultService.Fail("You already belong to a studio.");
            }

            var studio = await context.Studios.FirstOrDefaultAsync(s => s.Id == studioId);
            if (studio == null)
            {
                return ResultService.Fail("Studio was not found.");
            }

            if (!studio.IsOpenForJoinRequests)
            {
                return ResultService.Fail("This studio is not accepting new artists.");
            }

            var pendingExists = await context.StudioJoinRequests.AnyAsync(r =>
                r.TattooArtistId == artist.Id && r.Status == StudioJoinRequestStatus.Pending);

            if (pendingExists)
            {
                return ResultService.Fail("You already have a pending studio join request.");
            }

            context.StudioJoinRequests.Add(new StudioJoinRequest
            {
                StudioId = studioId,
                TattooArtistId = artist.Id,
                Status = StudioJoinRequestStatus.Pending,
                CreatedOn = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService> AcceptJoinRequestAsync(int requestId, string ownerUserId)
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            var request = await context.StudioJoinRequests
                .Include(r => r.Studio).ThenInclude(s => s.OwnerArtist)
                .Include(r => r.TattooArtist)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                return ResultService.Fail("Join request was not found.");

            if (request.Studio.OwnerArtist?.UserId != ownerUserId)
                return ResultService.Fail("Only the studio owner can approve join requests.");

            if (request.Status != StudioJoinRequestStatus.Pending)
                return ResultService.Fail("This join request has already been handled.");

            if (request.TattooArtist.StudioId != null)
            {
                request.Status = StudioJoinRequestStatus.Cancelled;
                request.RespondedOn = DateTime.UtcNow;
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                return ResultService.Fail("This artist already belongs to a studio.");
            }

            request.TattooArtist.StudioId = request.StudioId;
            request.TattooArtist.JoinedStudioOn = DateTime.UtcNow;
            request.Status = StudioJoinRequestStatus.Accepted;
            request.RespondedOn = DateTime.UtcNow;

            // Defensive cleanup in case older data contains more than one pending request.
            var otherPending = await context.StudioJoinRequests
                .Where(r => r.TattooArtistId == request.TattooArtistId &&
                            r.Id != request.Id &&
                            r.Status == StudioJoinRequestStatus.Pending)
                .ToListAsync();

            foreach (var other in otherPending)
            {
                other.Status = StudioJoinRequestStatus.Cancelled;
                other.RespondedOn = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService> RejectJoinRequestAsync(int requestId, string ownerUserId)
        {
            var request = await context.StudioJoinRequests
                .Include(r => r.Studio).ThenInclude(s => s.OwnerArtist)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                return ResultService.Fail("Join request was not found.");

            if (request.Studio.OwnerArtist?.UserId != ownerUserId)
                return ResultService.Fail("Only the studio owner can reject join requests.");

            if (request.Status != StudioJoinRequestStatus.Pending)
                return ResultService.Fail("This join request has already been handled.");

            request.Status = StudioJoinRequestStatus.Rejected;
            request.RespondedOn = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService> RemoveMemberAsync(int artistId, string ownerUserId)
        {
            var owner = await context.TattooArtists.FirstOrDefaultAsync(a => a.UserId == ownerUserId);
            if (owner?.StudioId == null)
                return ResultService.Fail("Studio owner profile was not found.");

            var studio = await context.Studios.FirstOrDefaultAsync(s => s.Id == owner.StudioId.Value);
            if (studio == null || studio.OwnerArtistId != owner.Id)
                return ResultService.Fail("Only the studio owner can remove members.");

            if (artistId == owner.Id)
                return ResultService.Fail("The studio owner cannot remove themselves.");

            var member = await context.TattooArtists.FirstOrDefaultAsync(a => a.Id == artistId && a.StudioId == studio.Id);
            if (member == null)
                return ResultService.Fail("Studio member was not found.");

            member.StudioId = null;
            member.JoinedStudioOn = null;
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService> SetOpenForJoinRequestsAsync(bool isOpen, string ownerUserId)
        {
            var studio = await FindOwnedStudioAsync(ownerUserId);
            if (studio == null)
                return ResultService.Fail("Only the studio owner can change join-request settings.");

            studio.IsOpenForJoinRequests = isOpen;
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService> UpdateStudioAsync(UpdateStudioDto dto, string ownerUserId)
        {
            var validation = ValidateStudio(dto);
            if (!validation.Success) return validation;

            var studio = await FindOwnedStudioAsync(ownerUserId);
            if (studio == null)
                return ResultService.Fail("Only the studio owner can edit studio information.");

            var duplicateExists = await context.Studios.AnyAsync(s =>
                s.Id != studio.Id &&
                s.Name.ToLower() == dto.Name.Trim().ToLower() &&
                s.City.ToLower() == dto.City.Trim().ToLower() &&
                s.Address.ToLower() == dto.Address.Trim().ToLower());

            if (duplicateExists)
                return ResultService.Fail("A studio with the same name and address already exists.");

            studio.Name = dto.Name.Trim();
            studio.Description = dto.Description.Trim();
            studio.Address = dto.Address.Trim();
            studio.City = dto.City.Trim();
            studio.Country = dto.Country.Trim();
            studio.Latitude = dto.Latitude;
            studio.Longitude = dto.Longitude;

            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        private IQueryable<Studio> BaseStudioQuery()
            => context.Studios
                .AsNoTracking()
                .Include(s => s.Artists).ThenInclude(a => a.User)
                .Include(s => s.Artists).ThenInclude(a => a.Reviews)
                .Include(s => s.Artists).ThenInclude(a => a.PortfolioImages);

        private async Task<Studio?> FindOwnedStudioAsync(string ownerUserId)
        {
            return await context.Studios
                .Include(s => s.OwnerArtist)
                .FirstOrDefaultAsync(s => s.OwnerArtist != null && s.OwnerArtist.UserId == ownerUserId);
        }

        public static ResultService ValidateStudio(CreateStudioDto dto)
        {
            if (dto == null) return ResultService.Fail("Studio information is required.");
            if (string.IsNullOrWhiteSpace(dto.Name)) return ResultService.Fail("Studio name is required.");
            if (dto.Name.Trim().Length > 120) return ResultService.Fail("Studio name cannot exceed 120 characters.");
            if (string.IsNullOrWhiteSpace(dto.Description)) return ResultService.Fail("Studio description is required.");
            if (dto.Description.Trim().Length > 1500) return ResultService.Fail("Studio description cannot exceed 1500 characters.");
            if (string.IsNullOrWhiteSpace(dto.Address)) return ResultService.Fail("Studio address is required.");
            if (dto.Address.Trim().Length > 220) return ResultService.Fail("Studio address cannot exceed 220 characters.");
            if (string.IsNullOrWhiteSpace(dto.City)) return ResultService.Fail("Studio city is required.");
            if (dto.City.Trim().Length > 100) return ResultService.Fail("Studio city cannot exceed 100 characters.");
            if (string.IsNullOrWhiteSpace(dto.Country)) return ResultService.Fail("Studio country is required.");
            if (dto.Country.Trim().Length > 100) return ResultService.Fail("Studio country cannot exceed 100 characters.");
            if (dto.Latitude is < -90 or > 90) return ResultService.Fail("Studio latitude must be between -90 and 90.");
            if (dto.Longitude is < -180 or > 180) return ResultService.Fail("Studio longitude must be between -180 and 180.");
            return ResultService.Ok();
        }

        private static StudioDto MapStudio(Studio studio)
        {
            var artists = studio.Artists
                .OrderBy(a => a.Id == studio.OwnerArtistId ? 0 : 1)
                .ThenBy(a => a.JoinedStudioOn ?? DateTime.MaxValue)
                .ThenBy(a => a.Id)
                .Select(a => new StudioArtistDto
                {
                    Id = a.Id,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    ProfileImageUrl = a.User?.ProfileImageUrl,
                    Description = a.Description,
                    PhoneNumber = a.PhoneNumber,
                    IsVerified = a.IsVerified,
                    AverageRating = a.Reviews.Any() ? Math.Round(a.Reviews.Average(r => r.Rating), 1) : 0,
                    ReviewCount = a.Reviews.Count,
                    JoinedStudioOn = a.JoinedStudioOn,
                    PortfolioImageUrls = a.PortfolioImages.OrderBy(p => p.Id).Take(3).Select(p => p.ImageUrl).ToList()
                })
                .ToList();

            return new StudioDto
            {
                Id = studio.Id,
                Name = studio.Name,
                Description = studio.Description,
                Address = studio.Address,
                City = studio.City,
                Country = studio.Country,
                Latitude = studio.Latitude,
                Longitude = studio.Longitude,
                IsOpenForJoinRequests = studio.IsOpenForJoinRequests,
                ArtistCount = artists.Count,
                Artists = artists,
                PortfolioPreviewUrls = artists.SelectMany(a => a.PortfolioImageUrls).Take(6).ToList()
            };
        }

        private static StudioJoinRequestDto MapJoinRequest(StudioJoinRequest request)
        {
            return new StudioJoinRequestDto
            {
                Id = request.Id,
                StudioId = request.StudioId,
                StudioName = request.Studio.Name,
                TattooArtistId = request.TattooArtistId,
                ArtistName = $"{request.TattooArtist.FirstName} {request.TattooArtist.LastName}",
                ArtistDescription = request.TattooArtist.Description,
                ArtistPhoneNumber = request.TattooArtist.PhoneNumber,
                ArtistProfileImageUrl = request.TattooArtist.User?.ProfileImageUrl,
                Status = request.Status,
                CreatedOn = request.CreatedOn,
                RespondedOn = request.RespondedOn
            };
        }
    }
}
