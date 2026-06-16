using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tattoo_Project.Data;
using Tattoo_Project.DTOs.ClientDTOs;
using Tattoo_Project.Models;
using Tattoo_Project.Services.Interfaces;

namespace Tattoo_Project.Services
{
    public class ClientService(
        TattooDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
        : IClientService
    {
        public async Task<ICollection<GetClientDto>> GetAllClientsAsync()
        {
            return await context.Clients
                .Select(c => new GetClientDto
                {
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    PhoneNumber = c.PhoneNumber
                })
                .ToListAsync();
        }

        public async Task<GetClientDto?> GetClientByIdAsync(int id)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                return null;
            }

            return new GetClientDto
            {
                FirstName = client.FirstName,
                LastName = client.LastName,
                Email = client.Email,
                PhoneNumber = client.PhoneNumber
            };
        }

        public async Task<bool> CreateClientProfileAsync(
            CreateClientDto dto,
            string userId)
        {
            var alreadyHasClientProfile = await context.Clients
                .AnyAsync(c => c.UserId == userId);

            if (alreadyHasClientProfile)
            {
                return false;
            }

            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return false;
            }

            if (!await roleManager.RoleExistsAsync(UserRoles.Client))
            {
                await roleManager.CreateAsync(new IdentityRole(UserRoles.Client));
            }

            if (!await userManager.IsInRoleAsync(user, UserRoles.Client))
            {
                await userManager.AddToRoleAsync(user, UserRoles.Client);
            }

            Client client = new()
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                PhoneNumber = dto.PhoneNumber,
                UserId = user.Id
            };

            context.Clients.Add(client);

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateClientProfileAsync(
            UpdateClientDto dto,
            string userId)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                return false;
            }

            client.PhoneNumber = dto.PhoneNumber;

            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteClientAsync(int id)
        {
            var client = await context.Clients
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                return false;
            }

            context.Clients.Remove(client);

            await context.SaveChangesAsync();

            return true;
        }
    }
}