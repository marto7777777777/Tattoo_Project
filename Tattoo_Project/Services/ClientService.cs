using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs;
using Tattoo_Project.Models;

namespace Tattoo_Project.Services
{
    public class ClientService(TattooDbContext context) : IClientService
    {
        public async Task<bool> CreateClient(CreateClientDto dto)
        {
             context.Clients.Add(new Client
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber
            });
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteClient(int id)
        {
            var client = await context.Clients.FirstOrDefaultAsync(x => x.Id == id);
            if (client == null)
            {
                return false;
            }
            context.Remove(client);
            await context.SaveChangesAsync();
            return true;
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
        
        public async Task<GetClientDto> GetClientsByIdAsync(int id)
        {
            var client = await context.Clients.FirstOrDefaultAsync(x => x.Id == id);
            if (client == null)
            {
                return null;
            }
            var clientDto = new GetClientDto
            {
                FirstName = client.FirstName,
                LastName = client.LastName,
                Email = client.Email,
                PhoneNumber = client.PhoneNumber,
                ClientTattooRequestsDto = client.TattooRequests == null
                ? null
                : client.TattooRequests.Select(c => new ClientTattooRequestsDto
                {
                    Description = c.Description,
                    Status = c.Status,
                    CreatedOn = c.CreatedOn,
                    Placement = c.Placement,
                    Images = c.Images.Select(c => new TattooReferenceImageDto
                    {
                        ImageUrl = c.ImageUrl
                    }).ToList(),
                    TattooSessions = c.TattooSessions.Select(c => new TattooSessionDto
                    {
                        StartTime = c.StartTime,
                        EndTime = c.EndTime,
                        DurationHours = c.DurationHours,
                        FinalPrice = c.FinalPrice
                    }).ToList(),
                    ArtistResponse = new ArtistResponseDto
                    {
                        EstimatedHours = c.ArtistResponse.EstimatedHours,
                        CreatedOn = c.ArtistResponse.CreatedOn,
                        EstimatedPrice = c.ArtistResponse.EstimatedPrice,
                        ResponseMessage = c.ArtistResponse.ResponseMessage
                    },
                    Consultation = new ConsultationDto
                    {
                        StartTime = c.Consultation.StartTime,
                        EndTime = c.Consultation.EndTime,
                        IsOnline = c.Consultation.IsOnline,
                        Notes = c.Consultation.Notes
                    }
                }).ToList()
            };
            return clientDto;
        }

        public async Task<bool> UpdateClient(int id, UpdateClientDto dto)
        {
            var client = await context.Clients.FirstOrDefaultAsync(c => c.Id == id);
            if (client == null)
            {
                return false;
            }
            client.FirstName = dto.FirstName;
            client.LastName = dto.LastName;
            client.PhoneNumber = dto.PhoneNumber;
            client.Email = dto.Email;

            await context.SaveChangesAsync();
            return true;
        }
    }
}
