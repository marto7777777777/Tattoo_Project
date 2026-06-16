using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.TattooReferenceImageDTOs;
using Tattoo_Project.DTOs.TattooRequestDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Services
{
    public class TattooRequestService(TattooDbContext context)
        : ITattooRequestService
    {
        public async Task<ICollection<GetTattooRequestDto>> GetAllTattooRequestsAsync()
        {
            return await context.TattooRequests
                .Include(r => r.Images)
                .Select(r => new GetTattooRequestDto
                {
                    TattooArtistId = r.TattooArtistId,
                    Description = r.Description,
                    Placement = r.Placement,
                    Status = r.Status,
                    CreatedOn = r.CreatedOn,
                    Images = r.Images.Select(i => new TattooReferenceImageDto
                    {
                        ImageUrl = i.ImageUrl
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<GetTattooRequestDto?> GetTattooRequestByIdAsync(
            int id,
            string userId,
            bool isAdmin,
            bool isClient,
            bool isArtist)
        {
            var tattooRequest = await context.TattooRequests
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (tattooRequest == null)
            {
                return null;
            }

            if (isAdmin)
            {
                return MapToDto(tattooRequest);
            }

            if (isClient)
            {
                var client = await context.Clients
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (client != null &&
                    tattooRequest.ClientId == client.Id)
                {
                    return MapToDto(tattooRequest);
                }
            }

            if (isArtist)
            {
                var tattooArtist = await context.TattooArtists
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (tattooArtist != null &&
                    tattooRequest.TattooArtistId == tattooArtist.Id)
                {
                    return MapToDto(tattooRequest);
                }
            }

            return null;
        }

        public async Task<ICollection<GetTattooRequestDto>> GetMyTattooRequestsAsync(
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return new List<GetTattooRequestDto>();
            }

            return await context.TattooRequests
                .Include(r => r.Images)
                .Where(r => r.ClientId == client.Id)
                .Select(r => new GetTattooRequestDto
                {
                    TattooArtistId = r.TattooArtistId,
                    Description = r.Description,
                    Placement = r.Placement,
                    Status = r.Status,
                    CreatedOn = r.CreatedOn,
                    Images = r.Images.Select(i => new TattooReferenceImageDto
                    {
                        ImageUrl = i.ImageUrl
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<bool> CreateTattooRequest(
            CreateTattooRequestDto dto,
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return false;
            }

            var tattooArtistExists = await context.TattooArtists
                .AnyAsync(a => a.Id == dto.TattooArtistId);

            if (!tattooArtistExists)
            {
                return false;
            }

            TattooRequest tattooRequest = new()
            {
                ClientId = client.Id,
                TattooArtistId = dto.TattooArtistId,
                Description = dto.Description,
                Placement = dto.Placement,
                Status = RequestStatus.Submitted,
                CreatedOn = DateTime.UtcNow,
                Images = dto.Images.Select(i => new TattooReferenceImage
                {
                    ImageUrl = i.ImageUrl
                }).ToList()
            };

            context.TattooRequests.Add(tattooRequest);

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateTattooRequestAsync(
            int id,
            UpdateTattooRequestDto dto,
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return false;
            }

            var tattooRequest = await context.TattooRequests
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (tattooRequest == null)
            {
                return false;
            }

            if (tattooRequest.ClientId != client.Id)
            {
                return false;
            }

            if (tattooRequest.Status != RequestStatus.Submitted)
            {
                return false;
            }

            tattooRequest.Description = dto.Description;

            tattooRequest.Images.Clear();

            foreach (var image in dto.Images)
            {
                tattooRequest.Images.Add(new TattooReferenceImage
                {
                    ImageUrl = image.ImageUrl
                });
            }

            await context.SaveChangesAsync();

            return true;
        }

        private static GetTattooRequestDto MapToDto(TattooRequest tattooRequest)
        {
            return new GetTattooRequestDto
            {
                TattooArtistId = tattooRequest.TattooArtistId,
                Description = tattooRequest.Description,
                Placement = tattooRequest.Placement,
                Status = tattooRequest.Status,
                CreatedOn = tattooRequest.CreatedOn,
                Images = tattooRequest.Images.Select(i => new TattooReferenceImageDto
                {
                    ImageUrl = i.ImageUrl
                }).ToList()
            };
        }
    }
}