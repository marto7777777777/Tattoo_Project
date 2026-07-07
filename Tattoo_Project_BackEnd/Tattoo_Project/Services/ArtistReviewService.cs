using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.ArtistReviewDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services
{
    public class ArtistReviewService : IArtistReviewService
    {
        private readonly TattooDbContext context;

        public ArtistReviewService(TattooDbContext context)
        {
            this.context = context;
        }

        public async Task<ResultService> CreateArtistReviewAsync(CreateArtistReviewDto dto, string userId)
        {
            if (dto.Rating < 1 || dto.Rating > 5)
            {
                return ResultService.Fail("Rating must be between 1 and 5.");
            }

            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return ResultService.Fail("Client profile not found.");
            }

            var tattooRequest = await context.TattooRequests
                .Include(r => r.ArtistReview)
                .FirstOrDefaultAsync(r => r.Id == dto.TattooRequestId);

            if (tattooRequest == null)
            {
                return ResultService.Fail("Tattoo request not found.");
            }

            if (tattooRequest.ClientId != client.Id)
            {
                return ResultService.Fail("You can review only your own tattoo requests.");
            }

            if (tattooRequest.Status != RequestStatus.Completed)
            {
                return ResultService.Fail("You can review only completed tattoo requests.");
            }

            if (tattooRequest.ArtistReview != null)
            {
                return ResultService.Fail("This tattoo request already has a review.");
            }

            var review = new ArtistReview
            {
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedOn = DateTime.UtcNow,
                TattooRequestId = tattooRequest.Id,
                ClientId = client.Id,
                TattooArtistId = tattooRequest.TattooArtistId
            };

            await context.ArtistReviews.AddAsync(review);
            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService<ICollection<GetArtistReviewDto>>> GetArtistReviewsByArtistIdAsync(int tattooArtistId)
        {
            var artistExists = await context.TattooArtists
                .AnyAsync(a => a.Id == tattooArtistId);

            if (!artistExists)
            {
                return ResultService<ICollection<GetArtistReviewDto>>.Fail("Tattoo artist not found.");
            }

            var reviews = await context.ArtistReviews
                .Where(r => r.TattooArtistId == tattooArtistId)
                .OrderByDescending(r => r.CreatedOn)
                .Select(r => new GetArtistReviewDto
                {
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedOn = r.CreatedOn
                })
                .ToListAsync();

            return ResultService<ICollection<GetArtistReviewDto>>.Ok(reviews);
        }
    }
}