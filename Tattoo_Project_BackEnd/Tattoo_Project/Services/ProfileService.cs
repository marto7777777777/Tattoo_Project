using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.ProfileDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services
{
    public class ProfileService(
        TattooDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment)
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
                ProfileImageUrl = user.ProfileImageUrl,
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
                    RequiresDeposit = artist.RequiresDeposit,
                    DepositAmount = artist.DepositAmount,
                    Requirements = artist.Requirements.Select(r => new ProfileRequirementDto
                    {
                        Id = r.Id,
                        Description = r.Description
                    }).ToList(),
                    PortfolioImages = artist.PortfolioImages.Select(p => new ProfilePortfolioImageDto
                    {
                        Id = p.Id,
                        ImageUrl = p.ImageUrl
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

        public async Task<ResultService> UpdateEmailAsync(string userId, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ResultService.Fail("Email is required.");
            }

            var user = await FindUser(userId);
            if (user == null) return ResultService.Fail("User was not found.");

            var email = value.Trim();
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                return ResultService.Fail("Email is already taken.");
            }

            user.Email = email;
            user.UserName = email;
            user.EmailConfirmed = false;
            var updateResult = await userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                return ResultService.Fail(string.Join(" ", updateResult.Errors.Select(e => e.Description)));
            }

            await SyncEmailAsync(userId, email);
            return ResultService.Ok();
        }

        public async Task<ResultService<string>> UpdateProfileImageAsync(string userId, IFormFile image)
        {
            var user = await FindUser(userId);
            if (user == null) return ResultService<string>.Fail("User was not found.");

            var validation = ValidateImage(image);
            if (!validation.Success) return ResultService<string>.Fail(validation.ErrorMessage!);

            var imageUrl = await SaveImageAsync(image, "profile-images");

            DeleteOldFileIfLocal(user.ProfileImageUrl);
            user.ProfileImageUrl = imageUrl;
            await userManager.UpdateAsync(user);

            return ResultService<string>.Ok(imageUrl);
        }

        public async Task<ResultService> UpdatePhoneNumberAsync(string userId, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return ResultService.Fail("Phone number is required.");
            if (value.Trim().Length > 40) return ResultService.Fail("Phone number cannot exceed 40 characters.");

            var client = await context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            var artist = await context.TattooArtists.FirstOrDefaultAsync(a => a.UserId == userId);

            if (client == null && artist == null) return ResultService.Fail("Profile was not found.");

            if (client != null) client.PhoneNumber = value.Trim();
            if (artist != null) artist.PhoneNumber = value.Trim();

            await context.SaveChangesAsync();
            return ResultService.Ok();
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

            var validation = ValidateImage(image);
            if (!validation.Success) return ResultService<ProfilePortfolioImageDto>.Fail(validation.ErrorMessage!);

            var imageUrl = await SaveImageAsync(image, "portfolio-images");
            var portfolioImage = new PortfolioImage
            {
                TattooArtistId = artist.Id,
                ImageUrl = imageUrl
            };

            context.Add(portfolioImage);
            await context.SaveChangesAsync();

            return ResultService<ProfilePortfolioImageDto>.Ok(new ProfilePortfolioImageDto
            {
                Id = portfolioImage.Id,
                ImageUrl = portfolioImage.ImageUrl
            });
        }

        public async Task<ResultService> DeletePortfolioImageAsync(string userId, int imageId)
        {
            var artist = await FindArtist(userId);
            if (artist == null) return ResultService.Fail("Tattoo artist profile was not found.");

            var image = await context.Set<PortfolioImage>()
                .FirstOrDefaultAsync(p => p.Id == imageId && p.TattooArtistId == artist.Id);

            if (image == null) return ResultService.Fail("Portfolio image was not found.");

            DeleteOldFileIfLocal(image.ImageUrl);
            context.Remove(image);
            await context.SaveChangesAsync();
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

            return ResultService.Ok();
        }

        private async Task<string> SaveImageAsync(IFormFile image, string folderName)
        {
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            var webRootPath = environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(environment.ContentRootPath, "wwwroot");
            }

            var folderPath = Path.Combine(webRootPath, "uploads", folderName);
            Directory.CreateDirectory(folderPath);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(folderPath, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await image.CopyToAsync(stream);

            return $"/uploads/{folderName}/{fileName}";
        }

        private void DeleteOldFileIfLocal(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("/uploads/"))
            {
                return;
            }

            var webRootPath = environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(environment.ContentRootPath, "wwwroot");
            }

            var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var filePath = Path.Combine(webRootPath, relativePath);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
