using Tattoo_Project.Services.Results;
namespace Tattoo_Project.Services.Interfaces;
public interface IAccountDeletionService { Task<ResultService> DeleteAsync(string userId,string password,string confirmation); }
