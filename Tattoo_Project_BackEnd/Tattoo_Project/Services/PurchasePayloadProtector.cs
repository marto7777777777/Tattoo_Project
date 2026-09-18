using System.Security.Cryptography;using System.Text;using Microsoft.AspNetCore.DataProtection;using Tattoo_Project.Services.Interfaces;
namespace Tattoo_Project.Services;
public class PurchasePayloadProtector(IDataProtectionProvider provider):IPurchasePayloadProtector
{
 private readonly IDataProtector protector=provider.CreateProtector("InkRoute.Billing.PurchasePayload.v1");
 public string Protect(string value)=>protector.Protect(value);public string Unprotect(string value)=>protector.Unprotect(value);public string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
