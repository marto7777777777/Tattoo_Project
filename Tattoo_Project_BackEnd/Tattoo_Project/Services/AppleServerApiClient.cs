using System.Security.Cryptography;using System.Text;using System.Text.Json;
namespace Tattoo_Project.Services;
public class AppleServerApiClient(IConfiguration config,IHttpClientFactory clients)
{
 public async Task<string?> GetStatusAsync(string transactionId,bool sandbox)
 {
  var baseUrl=sandbox?"https://api.storekit-sandbox.itunes.apple.com":"https://api.storekit.itunes.apple.com";using var request=new HttpRequestMessage(HttpMethod.Get,$"{baseUrl}/inApps/v1/subscriptions/{Uri.EscapeDataString(transactionId)}");request.Headers.Authorization=new("Bearer",CreateToken());var response=await clients.CreateClient().SendAsync(request);if(response.StatusCode==System.Net.HttpStatusCode.NotFound)return null;if(!response.IsSuccessStatusCode)throw new InvalidOperationException($"Apple status API returned {(int)response.StatusCode}.");return await response.Content.ReadAsStringAsync();
 }
 public async Task<string?> GetTransactionAsync(string transactionId,bool sandbox)
 {
  var baseUrl=sandbox?"https://api.storekit-sandbox.itunes.apple.com":"https://api.storekit.itunes.apple.com";using var request=new HttpRequestMessage(HttpMethod.Get,$"{baseUrl}/inApps/v1/transactions/{Uri.EscapeDataString(transactionId)}");request.Headers.Authorization=new("Bearer",CreateToken());var response=await clients.CreateClient().SendAsync(request);if(response.StatusCode==System.Net.HttpStatusCode.NotFound)return null;if(!response.IsSuccessStatusCode)throw new InvalidOperationException($"Apple transaction API returned {(int)response.StatusCode}.");return await response.Content.ReadAsStringAsync();
 }
 private string CreateToken()
 {
  var issuer=config["Apple:IssuerId"];var keyId=config["Apple:KeyId"];var bundle=config["Apple:BundleId"];var keyPath=config["Apple:PrivateKeyPath"];if(new[]{issuer,keyId,bundle,keyPath}.Any(string.IsNullOrWhiteSpace)||!Path.IsPathRooted(keyPath!)||!System.IO.File.Exists(keyPath))throw new InvalidOperationException("Apple Server API credentials are not configured.");
  var now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();var h=Encode(JsonSerializer.SerializeToUtf8Bytes(new{alg="ES256",kid=keyId,typ="JWT"}));var p=Encode(JsonSerializer.SerializeToUtf8Bytes(new{iss=issuer,iat=now,exp=now+1200,aud="appstoreconnect-v1",bid=bundle}));using var ecdsa=ECDsa.Create();ecdsa.ImportFromPem(System.IO.File.ReadAllText(keyPath!));var sig=ecdsa.SignData(Encoding.ASCII.GetBytes(h+"."+p),HashAlgorithmName.SHA256,DSASignatureFormat.IeeeP1363FixedFieldConcatenation);return h+"."+p+"."+Encode(sig);
 }
 private static string Encode(byte[] bytes)=>Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_');
}
