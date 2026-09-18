using Tattoo_Project.Services.Results;
namespace Tattoo_Project.Services.Interfaces;
public interface IProviderWebhookEventService{Task<ResultService> ExecuteOnceAsync(string provider,string eventId,string eventType,string payload,Func<Task<ResultService>> handler);}
