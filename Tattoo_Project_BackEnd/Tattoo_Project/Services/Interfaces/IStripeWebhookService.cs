using Tattoo_Project.Services.Results;
namespace Tattoo_Project.Services.Interfaces;
public interface IStripeWebhookService { Task<ResultService> ProcessAsync(string payload, string signature); }
