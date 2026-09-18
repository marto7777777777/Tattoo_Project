using System.Security.Cryptography;using System.Security.Cryptography.X509Certificates;using System.Text;using System.Text.Json;
namespace Tattoo_Project.Services;
public class AppleJwsVerifier(IConfiguration config)
{
 public JsonDocument Verify(string jws)
 {
  if(string.IsNullOrWhiteSpace(jws)||jws.Length>1_048_576)throw new InvalidOperationException("Invalid Apple JWS.");
  var parts=jws.Split('.');if(parts.Length!=3)throw new InvalidOperationException("Invalid Apple JWS.");
  using var header=JsonDocument.Parse(Decode(parts[0]));
  if(!header.RootElement.TryGetProperty("alg",out var algorithm)||algorithm.GetString()!="ES256")throw new InvalidOperationException("Unexpected Apple JWS algorithm.");
  if(!header.RootElement.TryGetProperty("x5c",out var chainElement)||chainElement.ValueKind!=JsonValueKind.Array)throw new InvalidOperationException("Apple JWS certificate is missing.");
  var certificates=chainElement.EnumerateArray().Take(6).Select(x=>X509CertificateLoader.LoadCertificate(Convert.FromBase64String(x.GetString()!))).ToList();
  if(certificates.Count==0||chainElement.GetArrayLength()!=certificates.Count)throw new InvalidOperationException("Apple JWS certificate chain is invalid.");
  try
  {
   var rootPath=config["Apple:RootCertificatePath"];if(string.IsNullOrWhiteSpace(rootPath)||!Path.IsPathRooted(rootPath)||!System.IO.File.Exists(rootPath))throw new InvalidOperationException("Apple Root CA is not configured.");
   using var root=X509CertificateLoader.LoadCertificateFromFile(rootPath);using var chain=new X509Chain();chain.ChainPolicy.RevocationMode=X509RevocationMode.Online;chain.ChainPolicy.RevocationFlag=X509RevocationFlag.ExcludeRoot;chain.ChainPolicy.TrustMode=X509ChainTrustMode.CustomRootTrust;chain.ChainPolicy.CustomTrustStore.Add(root);foreach(var cert in certificates.Skip(1))chain.ChainPolicy.ExtraStore.Add(cert);if(!chain.Build(certificates[0]))throw new InvalidOperationException("Apple JWS certificate chain is invalid.");
   using var key=certificates[0].GetECDsaPublicKey()??throw new InvalidOperationException("Apple JWS key is invalid.");var data=Encoding.ASCII.GetBytes(parts[0]+"."+parts[1]);var signature=Decode(parts[2]);if(!key.VerifyData(data,signature,HashAlgorithmName.SHA256,DSASignatureFormat.IeeeP1363FixedFieldConcatenation))throw new InvalidOperationException("Apple JWS signature is invalid.");return JsonDocument.Parse(Decode(parts[1]));
  }
  finally{foreach(var certificate in certificates)certificate.Dispose();}
 }
 private static byte[] Decode(string value){var s=value.Replace('-','+').Replace('_','/');s+=new string('=',(4-s.Length%4)%4);return Convert.FromBase64String(s);}
}
