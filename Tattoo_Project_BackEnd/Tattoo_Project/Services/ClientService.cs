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

            if (string.IsNullOrWhiteSpace(dto.City))
            {
                return ResultService.Fail("City is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Country))
            {
                return ResultService.Fail("Country is required.");
            }

            if (!PhoneNumberNormalizer.TryNormalize(dto.PhoneNumber, out var normalizedPhone))
            {
                return ResultService.Fail("Phone number must include a valid international country code, for example +359888123456.");
            }

            if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                if (!PhoneNumberNormalizer.TryNormalize(user.PhoneNumber, out var currentPhone) || currentPhone != normalizedPhone)
                    return ResultService.Fail("Phone number cannot be changed after it has been registered to the account.");
            }
            else
            {
                var numberAlreadyUsed = await context.Users
                    .AnyAsync(other => other.Id != userId && other.PhoneNumber == normalizedPhone);
                if (numberAlreadyUsed)
                    return ResultService.Fail("Phone number is already registered to another account.");

                user.PhoneNumber = normalizedPhone;
            }

            if (!await roleManager.RoleExistsAsync(UserRoles.Client))
            {
                await roleManager.CreateAsync(new IdentityRole(UserRoles.Client));
            }

            Client client = new()
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                PhoneNumber = normalizedPhone,
                UserId = user.Id,
                City = dto.City,
                Country = dto.Country
            };

            context.Clients.Add(client);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is Microsoft.Data.SqlClient.SqlException sqlException &&
                sqlException.Number is 2601 or 2627)
            {
                // The unique user phone index is the final guard against two
                // simultaneous requests claiming the same number.
                return ResultService.Fail("Phone number or email is already registered to another account.");
            }

            if (!await userManager.IsInRoleAsync(user, UserRoles.Client))
            {
                var roleResult = await userManager.AddToRoleAsync(user, UserRoles.Client);
                if (!roleResult.Succeeded)
                    return ResultService.Fail("Client profile was created, but the Client role could not be assigned.");
            }

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
