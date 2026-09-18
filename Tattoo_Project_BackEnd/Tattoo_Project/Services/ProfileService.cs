using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.ProfileDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;
using Tattoo_Project.Security;

namespace Tattoo_Project.Services
{
    public class ProfileService(
        TattooDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment,
        IFileStorage storage,
        IImageSanitizer imageSanitizer,
        IPrivateMediaUrlService mediaUrls)
        : IProfileService
    {
        private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
        private const long MaxImageSize = 5 * 1024 * 1024;

        public async Task<ResultService<CurrentProfileDto>> GetMyProfileAsync(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return ResultService<CurrentProfileDto>.Fail("User was not found.");
            }

            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            var artist = await context.TattooArtists
                .Include(a => a.Studio)
                .Include(a => a.Requirements)
                .Include(a => a.PortfolioImages)
                .Include(a => a.Schedules)
                .Include(a => a.SpecialtyStyles)
                .FirstOrDefaultAsync(a => a.UserId == userId);

            var hasPendingJoinRequest = artist != null && artist.StudioId == null &&
                await context.StudioJoinRequests.AnyAsync(r =>
                    r.TattooArtistId == artist.Id && r.Status == StudioJoinRequestStatus.Pending);

            var dto = new CurrentProfileDto
            {
                IsTattooArtist = artist != null,
                IsClient = client != null,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                ProfileImageUrl = string.IsNullOrWhiteSpace(user.ProfileImageUrl) ? null : mediaUrls.CreateReadUrl(user.ProfileImageUrl),
                PhoneNumber = artist?.PhoneNumber ?? client?.PhoneNumber,
                City = client?.City,
                Country = client?.Country,
                Artist = artist == null ? null : new ArtistProfileSectionDto
                {
                    StudioId = artist.StudioId,
                    StudioName = artist.Studio?.Name,
                    Description = artist.Description,
                    StudioAddress = artist.Studio?.Address,
                    StudioCity = artist.Studio?.City,
                    StudioCountry = artist.Studio?.Country,
                    HasStudio = artist.StudioId != null,
                    IsStudioOwner = artist.Studio?.OwnerArtistId == artist.Id,
                    HasPendingStudioJoinRequest = hasPendingJoinRequest,
                    ConsultationDurationMinutes = artist.ConsultationDurationMinutes,
                    OffersOnlineConsultation = artist.OffersOnlineConsultation,
                    ShowPhoneNumberOnPublicProfile = artist.ShowPhoneNumberOnPublicProfile,
                    RequiresDeposit = artist.RequiresDeposit,
                    DepositAmount = artist.DepositAmount,
                    SpecialtyStyles = artist.SpecialtyStyles.OrderBy(x => x.Name).Select(x => x.Name).ToList(),
                    Requirements = artist.Requirements.Select(r => new ProfileRequirementDto
                    {
                        Id = r.Id,
                        Description = r.Description
                    }).ToList(),
                    PortfolioImages = artist.PortfolioImages.Select(p => new ProfilePortfolioImageDto
                    {
                        Id = p.Id,
                        ImageUrl = mediaUrls.CreateReadUrl(p.ImageUrl)
                    }).ToList(),
                    Schedules = artist.Schedules.Select(s => new Tattoo_Project.DTOs.TattooArtistDTOs.TattooArtistScheduleDto
                    {
                        DayOfWeek = s.DayOfWeek,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        ScheduleType = s.ScheduleType
                    }).ToList()
                }
            };

            return ResultService<CurrentProfileDto>.Ok(dto);
        }

        public async Task<ResultService> UpdateFirstNameAsync(string userId, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ResultService.Fail("First name is required.");
            }

            var user = await FindUser(userId);
            if (user == null) return ResultService.Fail("User was not found.");

            user.FirstName = value.Trim();
            await userManager.UpdateAsync(user);
            await SyncNamesAsync(userId, user.FirstName, user.LastName);
            return ResultService.Ok();
        }

        public async Task<ResultService> UpdateLastNameAsync(string userId, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ResultService.Fail("Last name is required.");
            }

            var user = await FindUser(userId);
            if (user == null) return ResultService.Fail("User was not found.");

            user.LastName = value.Trim();
            await userManager.UpdateAsync(user);
            await SyncNamesAsync(userId, user.FirstName, user.LastName);
            return ResultService.Ok();
        }

        public Task<ResultService> UpdateEmailAsync(string userId, string value)
        {
            // Direct email mutation is intentionally disabled. Changing Email/UserName
            // without proving ownership of the new mailbox can lock out or weaken an account.
            return Task.FromResult(ResultService.Fail("Direct email change is disabled. Use the verified email-change flow."));
        }

        public async Task<ResultService<string>> UpdateProfileImageAsync(string userId, IFormFile image)
        {
            var user = await FindUser(userId);
            if (user == null) return ResultService<string>.Fail("User was not found.");

            var sanitized = await imageSanitizer.SanitizeAsync(image, MaxImageSize, 30_000_000);
            if (!sanitized.Success) return ResultService<string>.Fail(sanitized.ErrorMessage!);
            var imageKey = await storage.SaveAsync(sanitized.Data!.Bytes, "profile-images", sanitized.Data.Extension, StoredFileVisibility.Public);
            var oldKey = user.ProfileImageUrl;
            user.ProfileImageUrl = imageKey;
            var identityResult = await userManager.UpdateAsync(user);
            if (!identityResult.Succeeded)
            {
                await storage.DeleteAsync(imageKey);
                return ResultService<string>.Fail(string.Join("; ", identityResult.Errors.Select(e => e.Description)));
            }
            await DeleteStoredFileAsync(oldKey);
            return ResultService<string>.Ok(mediaUrls.CreateReadUrl(imageKey));
        }

        public async Task<ResultService> UpdateCityAsync(string userId, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return ResultService.Fail("City is required.");
            var client = await context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client == null) return ResultService.Fail("Contact profile was not found.");
            client.City = value.Trim();
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService> UpdateCountryAsync(string userId, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return ResultService.Fail("Country is required.");
            var client = await context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client == null) return ResultService.Fail("Contact profile was not found.");
            client.Country = value.Trim();
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        public Task<ResultService> UpdateDescriptionAsync(string userId, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Task.FromResult(ResultService.Fail("Artist description is required."));
            if (value.Trim().Length > 1200)
                return Task.FromResult(ResultService.Fail("Artist description cannot exceed 1200 characters."));
            return UpdateArtistStringAsync(userId, value, "Artist description is required.", a => a.Description = value.Trim());
        }

        public async Task<ResultService> UpdateSpecialtyStylesAsync(string userId, ICollection<string> values)
        {
            var artist = await context.TattooArtists
                .Include(x => x.SpecialtyStyles)
                .FirstOrDefaultAsync(x => x.UserId == userId);
            if (artist == null) return ResultService.Fail("Tattoo artist profile was not found.");

            var normalized = TattooStyleCatalog.Normalize(values);
            var suppliedCount = (values ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase).Count();
            if (suppliedCount != normalized.Count)
                return ResultService.Fail("One or more specialty styles are invalid.");

            artist.SpecialtyStyles.Clear();
            foreach (var style in normalized)
                artist.SpecialtyStyles.Add(new ArtistSpecialtyStyle { Name = style });
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService> UpdateConsultationDurationAsync(string userId, int value)
        {
            if (value < 15 || value > 180)
            {
                return ResultService.Fail("Consultation duration must be between 15 and 180 minutes.");
            }

            var artist = await FindArtist(userId);
            if (artist == null) return ResultService.Fail("Tattoo artist profile was not found.");
            artist.ConsultationDurationMinutes = value;
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService> UpdateOffersOnlineConsultationAsync(string userId, bool value)
        {
            var artist = await FindArtist(userId);
            if (artist == null) return ResultService.Fail("Tattoo artist profile was not found.");
            artist.OffersOnlineConsultation = value;
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService> UpdatePhoneNumberVisibilityAsync(string userId, bool value)
        {
            var artist = await FindArtist(userId);
            if (artist == null) return ResultService.Fail("Tattoo artist profile was not found.");
            artist.ShowPhoneNumberOnPublicProfile = value;
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService> UpdateRequiresDepositAsync(string userId, bool value)
        {
            var artist = await FindArtist(userId);
            if (artist == null) return ResultService.Fail("Tattoo artist profile was not found.");
            artist.RequiresDeposit = value;
            if (!value) artist.DepositAmount = null;
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService> UpdateDepositAmountAsync(string userId, decimal? value)
        {
            var artist = await FindArtist(userId);
            if (artist == null) return ResultService.Fail("Tattoo artist profile was not found.");
            if (artist.RequiresDeposit && (value == null || value <= 0))
            {
                return ResultService.Fail("Deposit amount must be greater than zero when deposit is required.");
            }
            artist.DepositAmount = artist.RequiresDeposit ? value : null;
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService<ProfileRequirementDto>> AddRequirementAsync(string userId, string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return ResultService<ProfileRequirementDto>.Fail("Requirement description is required.");
            }

            var artist = await FindArtist(userId);
            if (artist == null) return ResultService<ProfileRequirementDto>.Fail("Tattoo artist profile was not found.");

            var requirement = new ArtistRequirement
            {
                TattooArtistId = artist.Id,
                Description = description.Trim()
            };

            context.Add(requirement);
            await context.SaveChangesAsync();

            return ResultService<ProfileRequirementDto>.Ok(new ProfileRequirementDto
            {
                Id = requirement.Id,
                Description = requirement.Description
            });
        }

        public async Task<ResultService> UpdateRequirementAsync(string userId, int requirementId, string description)
        {
            if (string.IsNullOrWhiteSpace(description)) return ResultService.Fail("Requirement description is required.");

            var artist = await FindArtist(userId);
            if (artist == null) return ResultService.Fail("Tattoo artist profile was not found.");

            var requirement = await context.Set<ArtistRequirement>()
                .FirstOrDefaultAsync(r => r.Id == requirementId && r.TattooArtistId == artist.Id);

            if (requirement == null) return ResultService.Fail("Requirement was not found.");

            requirement.Description = description.Trim();
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService> DeleteRequirementAsync(string userId, int requirementId)
        {
            var artist = await FindArtist(userId);
            if (artist == null) return ResultService.Fail("Tattoo artist profile was not found.");

            var requirement = await context.Set<ArtistRequirement>()
                .FirstOrDefaultAsync(r => r.Id == requirementId && r.TattooArtistId == artist.Id);

            if (requirement == null) return ResultService.Fail("Requirement was not found.");

            context.Remove(requirement);
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        public async Task<ResultService<ProfilePortfolioImageDto>> AddPortfolioImageAsync(string userId, IFormFile image)
        {
            var artist = await FindArtist(userId);
            if (artist == null) return ResultService<ProfilePortfolioImageDto>.Fail("Tattoo artist profile was not found.");

            var sanitized = await imageSanitizer.SanitizeAsync(image, MaxImageSize, 30_000_000);
            if (!sanitized.Success) return ResultService<ProfilePortfolioImageDto>.Fail(sanitized.ErrorMessage!);
            var imageKey = await storage.SaveAsync(sanitized.Data!.Bytes, "portfolio-images", sanitized.Data.Extension, StoredFileVisibility.Public);
            var portfolioImage = new PortfolioImage { TattooArtistId = artist.Id, ImageUrl = imageKey };
            try
            {
                context.Add(portfolioImage);
                await context.SaveChangesAsync();
            }
            catch
            {
                await storage.DeleteAsync(imageKey);
                throw;
            }
            return ResultService<ProfilePortfolioImageDto>.Ok(new ProfilePortfolioImageDto
            {
                Id = portfolioImage.Id,
                ImageUrl = mediaUrls.CreateReadUrl(portfolioImage.ImageUrl)
            });
        }

        public async Task<ResultService> DeletePortfolioImageAsync(string userId, int imageId)
        {
            var artist = await FindArtist(userId);
            if (artist == null) return ResultService.Fail("Tattoo artist profile was not found.");

            var image = await context.Set<PortfolioImage>()
                .FirstOrDefaultAsync(p => p.Id == imageId && p.TattooArtistId == artist.Id);

            if (image == null) return ResultService.Fail("Portfolio image was not found.");

            var imageKey = image.ImageUrl;
            context.Remove(image);
            await context.SaveChangesAsync();
            await DeleteStoredFileAsync(imageKey);
            return ResultService.Ok();
        }

        private async Task<ResultService> UpdateArtistStringAsync(string userId, string value, string errorMessage, Action<TattooArtist> update)
        {
            if (string.IsNullOrWhiteSpace(value)) return ResultService.Fail(errorMessage);
            var artist = await FindArtist(userId);
            if (artist == null) return ResultService.Fail("Tattoo artist profile was not found.");
            update(artist);
            await context.SaveChangesAsync();
            return ResultService.Ok();
        }

        private async Task<ApplicationUser?> FindUser(string userId)
            => await userManager.FindByIdAsync(userId);

        private async Task<TattooArtist?> FindArtist(string userId)
            => await context.TattooArtists.FirstOrDefaultAsync(a => a.UserId == userId);

        private async Task SyncNamesAsync(string userId, string firstName, string lastName)
        {
            var client = await context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client != null)
            {
                client.FirstName = firstName;
                client.LastName = lastName;
            }

            var artist = await context.TattooArtists.FirstOrDefaultAsync(a => a.UserId == userId);
            if (artist != null)
            {
                artist.FirstName = firstName;
                artist.LastName = lastName;
            }

            await context.SaveChangesAsync();
        }

        private async Task SyncEmailAsync(string userId, string email)
        {
            var client = await context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client != null) client.Email = email;

            var artist = await context.TattooArtists.FirstOrDefaultAsync(a => a.UserId == userId);
            if (artist != null) artist.Email = email;

            await context.SaveChangesAsync();
        }

        private static ResultService ValidateImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return ResultService.Fail("Image is required.");
            }

            if (image.Length > MaxImageSize)
            {
                return ResultService.Fail("Image size cannot be bigger than 5MB.");
            }

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
            {
                return ResultService.Fail("Only JPG, JPEG, PNG and WEBP images are allowed.");
            }
            if (!ImageUploadSignatureValidator.MatchesExtension(image, extension))
                return ResultService.Fail("The uploaded file is not a valid image of the selected type.");

            return ResultService.Ok();
        }

        private async Task DeleteStoredFileAsync(string? imageKey)
        {
            if (string.IsNullOrWhiteSpace(imageKey)) return;
            if (storage.IsManagedKey(imageKey))
            {
                await storage.DeleteAsync(imageKey);
                return;
            }
            if (!imageKey.StartsWith("/uploads/", StringComparison.Ordinal)) return;
            var webRootPath = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
            var root = Path.GetFullPath(webRootPath) + Path.DirectorySeparatorChar;
            var path = Path.GetFullPath(Path.Combine(webRootPath, imageKey.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
            if (path.StartsWith(root, StringComparison.Ordinal) && File.Exists(path)) File.Delete(path);
        }
    }
}
