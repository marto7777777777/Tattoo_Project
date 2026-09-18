namespace Tattoo_Project.Services.Interfaces;
public interface IPurchasePayloadProtector { string Protect(string value); string Unprotect(string value); string Hash(string value); }
