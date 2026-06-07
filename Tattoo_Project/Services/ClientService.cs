using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs;

namespace Tattoo_Project.Services
{
    public class ClientService(TattooDbContext context) : IClientService
    {
        public async Task<bool> AddClient(AddClientDto dto)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> DeleteClient(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<List<GetClientDto>> GetAllClientsAsync()
            => await context.Clients.Select(x => new GetClientDto
            {
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                ClientTattooRequestsDto = x.TattooRequests.Select(x => new ClientTattooRequestsDto
                {
                    Description = x.Description,
                    Placement = x.Placement,
                    CreatedOn = x.CreatedOn,
                    Status = x.Status,
                    Consultation = x.Consultation == null
                    ? null
                    : new ConsultationDto
                    {
                        StartTime = x.Consultation.StartTime,
                        EndTime = x.Consultation.EndTime,
                        IsOnline = x.Consultation.IsOnline,
                        Notes = x.Consultation.Notes
                    },
                    TattooSessions = x.TattooSessions == null
                    ? null
                    : x.TattooSessions.Select(x => new TattooSessionDto
                    {
                        StartTime = x.StartTime,
                        EndTime = x.EndTime,
                        FinalPrice = x.FinalPrice,
                        DurationHours = x.DurationHours
                    }).ToList(),
                    Images = x.Images.Select(x => new TattooReferenceImageDto
                    {
                        ImageUrl = x.ImageUrl
                    }).ToList(),
                    ArtistResponse = x.ArtistResponse == null
                    ? null
                    : new ArtistResponseDto
                    {
                        EstimatedPrice = x.ArtistResponse.EstimatedPrice,
                        EstimatedHours = x.ArtistResponse.EstimatedHours,
                        CreatedOn = x.ArtistResponse.CreatedOn,
                        ResponseMessage = x.ArtistResponse.ResponseMessage
                    }
                }).ToList()
            })
            .ToListAsync();
        
        public async Task<ActionResult<GetClientDto>> GetClientsByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> UpdateClient(int id, UpdateClientDto dto)
        {
            throw new NotImplementedException();
        }
    }
}
