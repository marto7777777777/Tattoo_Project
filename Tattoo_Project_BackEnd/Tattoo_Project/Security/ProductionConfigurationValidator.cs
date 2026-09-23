namespace Tattoo_Project.Security;

public static class ProductionConfigurationValidator
{
    public static void Validate(WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsProduction()) return;

        var configuration = builder.Configuration;
        var missing = new List<string>();
        Require(configuration, missing,
            "ConnectionStrings:DefaultConnection",
            "Jwt:Key", "Jwt:Issuer", "Jwt:Audience",
            "Verification:CodeHashSecret",
            "FrontendUrl", "BackendUrl",
            "EmailSettings:SmtpHost", "EmailSettings:SmtpUsername", "EmailSettings:SmtpPassword", "EmailSettings:SenderEmail",
            "DataProtection:KeysPath", "Storage:RootPath",
            "Legal:TermsVersion", "Legal:PrivacyVersion");

        var stripeEnabled = configuration.GetValue<bool>("PaymentProviders:StripeEnabled");
        var googleEnabled = configuration.GetValue<bool>("PaymentProviders:GooglePlayEnabled");
        var appleEnabled = configuration.GetValue<bool>("PaymentProviders:AppleEnabled");
        var aiEnabled = configuration.GetValue<bool>("Features:AiEnabled");

        if (configuration.GetValue<bool>("Billing:AllowSandboxEntitlements"))
            throw new InvalidOperationException("Billing:AllowSandboxEntitlements must be false in Production.");

        if (aiEnabled) Require(configuration, missing, "OpenAI:ApiKey");
        if (stripeEnabled) Require(configuration, missing,
            "Stripe:SecretKey", "Stripe:WebhookSecret", "Stripe:ArtistMonthlyPriceId", "Stripe:AiProjectPassPriceId");
        if (googleEnabled) Require(configuration, missing,
            "Billing:AccountObfuscationSecret", "GooglePlay:PackageName", "GooglePlay:ArtistSubscriptionProductId",
            "GooglePlay:ArtistBasePlanId", "GooglePlay:ArtistTrialOfferId", "GooglePlay:AiProjectPassProductId",
            "GooglePlay:ServiceAccountJsonPath", "GooglePlay:PubSubAudience", "GooglePlay:PubSubServiceAccountEmail");
        if (appleEnabled) Require(configuration, missing,
            "Billing:AccountObfuscationSecret", "Apple:BundleId", "Apple:ArtistSubscriptionProductId",
            "Apple:AiProjectPassProductId", "Apple:IssuerId", "Apple:KeyId", "Apple:PrivateKeyPath", "Apple:RootCertificatePath");

        if (missing.Count > 0)
            throw new InvalidOperationException("Production configuration is incomplete. Missing keys: " + string.Join(", ", missing.Distinct()));

        if (configuration["Jwt:Key"]!.Length < 32)
            throw new InvalidOperationException("Jwt:Key must contain at least 32 characters in Production.");
        if (configuration["Verification:CodeHashSecret"]!.Length < 32)
            throw new InvalidOperationException("Verification:CodeHashSecret must contain at least 32 characters in Production.");
        if ((googleEnabled || appleEnabled) && configuration["Billing:AccountObfuscationSecret"]!.Length < 32)
            throw new InvalidOperationException("Billing:AccountObfuscationSecret must contain at least 32 characters in Production.");
        if (stripeEnabled && configuration.GetValue<long>("Stripe:AiProjectPassAmount") != 1249)
            throw new InvalidOperationException("Stripe:AiProjectPassAmount must be 1249 euro cents.");

        ValidateHttpsUrl(configuration, "FrontendUrl");
        ValidateHttpsUrl(configuration, "BackendUrl");

        var frontend = new Uri(configuration["FrontendUrl"]!);
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (!origins.Any(origin => string.Equals(origin.TrimEnd('/'), frontend.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Cors:AllowedOrigins must contain the Production FrontendUrl origin.");

        if (string.IsNullOrWhiteSpace(configuration["AllowedHosts"]) || configuration["AllowedHosts"] == "*")
            throw new InvalidOperationException("AllowedHosts must be explicitly restricted in Production.");

        if (googleEnabled) ValidateAbsoluteFile(configuration, "GooglePlay:ServiceAccountJsonPath");
        if (appleEnabled)
        {
            ValidateAbsoluteFile(configuration, "Apple:PrivateKeyPath");
            ValidateAbsoluteFile(configuration, "Apple:RootCertificatePath");
        }

        ValidateWritableAbsoluteDirectory(configuration, "DataProtection:KeysPath");
        ValidateWritableAbsoluteDirectory(configuration, "Storage:RootPath");

        if (configuration.GetValue<bool>("Proxy:Enabled") &&
            (configuration.GetSection("Proxy:KnownProxies").Get<string[]>()?.Length ?? 0) == 0)
            throw new InvalidOperationException("Proxy:Enabled requires at least one trusted Proxy:KnownProxies address.");
    }

    private static void Require(IConfiguration configuration, ICollection<string> missing, params string[] keys)
    {
        foreach (var key in keys)
            if (string.IsNullOrWhiteSpace(configuration[key])) missing.Add(key);
    }

    private static void ValidateHttpsUrl(IConfiguration configuration, string key)
    {
        if (!Uri.TryCreate(configuration[key], UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException($"{key} must be an absolute HTTPS URL in Production.");
    }

    private static void ValidateAbsoluteFile(IConfiguration configuration, string key)
    {
        var path = configuration[key]!;
        if (!Path.IsPathRooted(path) || !File.Exists(path))
            throw new InvalidOperationException($"{key} must point to an existing absolute mounted file.");
    }

    private static void ValidateWritableAbsoluteDirectory(IConfiguration configuration, string key)
    {
        var path = configuration[key]!;
        if (!Path.IsPathRooted(path))
            throw new InvalidOperationException($"{key} must be an absolute durable shared path.");
        Directory.CreateDirectory(path);
        var probe = Path.Combine(path, $".inkroute-write-probe-{Guid.NewGuid():N}");
        try { File.WriteAllText(probe, "ok"); File.Delete(probe); }
        catch (Exception ex) { throw new InvalidOperationException($"{key} must be writable by the application user.", ex); }
    }
}
