using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.ClientDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.Services.Results;

namespace Tattoo_Project.Services
{
    public class ClientService(
        TattooDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
        : IClientService
    {
        public async Task<ResultService<ICollection<GetClientDto>>> GetAllClientsAsync()
        {
            var clients = await context.Clients
                .Select(c => new GetClientDto
                {
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    PhoneNumber = c.PhoneNumber
                })
                .ToListAsync();

            return ResultService<ICollection<GetClientDto>>.Ok(clients);
        }

        public async Task<ResultService<GetClientDto>> GetClientByIdAsync(int id)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                return ResultService<GetClientDto>.Fail("Client was not found.");
            }

            var dto = new GetClientDto
            {
                FirstName = client.FirstName,
                LastName = client.LastName,
                Email = client.Email,
                PhoneNumber = client.PhoneNumber,
                City = client.City,
                Country = client.Country,
            };

            return ResultService<GetClientDto>.Ok(dto);
        }

        public async Task<ResultService> CreateClientProfileAsync(
            CreateClientDto dto,
            string userId)
        {
            var alreadyHasClientProfile = await context.Clients
                .AnyAsync(c => c.UserId == userId);

            if (alreadyHasClientProfile)
            {
                return ResultService.Fail("Client profile already exists.");
            }

            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return ResultService.Fail("User was not found.");
            }

            if (!await roleManager.RoleExistsAsync(UserRoles.Client))
            {
                await roleManager.CreateAsync(new IdentityRole(UserRoles.Client));
            }

            if (!await userManager.IsInRoleAsync(user, UserRoles.Client))
            {
                await userManager.AddToRoleAsync(user, UserRoles.Client);
            }
            if (string.IsNullOrWhiteSpace(dto.City))
            {
                return ResultService.Fail("City is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Country))
            {
                return ResultService.Fail("Country is required.");
            }

            Client client = new()
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                PhoneNumber = dto.PhoneNumber,
                UserId = user.Id,
                City = dto.City,
                Country = dto.Country
            };

            context.Clients.Add(client);

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> UpdateClientProfileAsync(
            UpdateClientDto dto,
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return ResultService.Fail("Client profile was not found.");
            }

            client.PhoneNumber = dto.PhoneNumber;
            client.City = dto.City;
            client.Country = dto.Country;

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }

        public async Task<ResultService> DeleteClientAsync(int id)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                return ResultService.Fail("Client was not found.");
            }

            context.Clients.Remove(client);

            await context.SaveChangesAsync();

            return ResultService.Ok();
        }
    }
}