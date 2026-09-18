
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Tattoo_Project.Data;
using Tattoo_Project.Services;
using Tattoo_Project.Services.Interfaces;
using Tattoo_Project.AI.Builders;
using Tattoo_Project.AI.Providers;
using Tattoo_Project.AI.Planning;
using Microsoft.AspNetCore.Identity;
using Tattoo_Project.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Stripe;
using Tattoo_Project.Authorization;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using Tattoo_Project.Security;
using Tattoo_Project.Middleware;

namespace Tattoo_Project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ProductionConfigurationValidator.Validate(builder);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<ApiExceptionHandler>();
            builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 16_000_000);
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
                options.AddPolicy("sensitive", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    $"{httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anon"}:{httpContext.Connection.RemoteIpAddress}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 8,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
                options.AddPolicy("ai", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    $"{httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anon"}:{httpContext.Connection.RemoteIpAddress}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 6,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
                options.AddPolicy("billing-webhook", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 120,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
            });
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });

            builder.Services.AddScoped<IClientService, ClientService>();
            builder.Services.AddScoped<ITattooArtistService, TattooArtistService>();
            builder.Services.AddScoped<IStudioService, StudioService>();
            builder.Services.AddScoped<ITattooRequestService, TattooRequestService>();
            builder.Services.AddScoped<IArtistResponseService, ArtistResponseService>();
            builder.Services.AddScoped<IConsultationService, ConsultationService>();
            builder.Services.AddScoped<ITattooSessionService, TattooSessionService>();
            builder.Services.AddScoped<ITokenService, Tattoo_Project.Services.TokenService>();
            builder.Services.AddScoped<IArtistReviewService, ArtistReviewService>();
            builder.Services.AddScoped<IClientFavoriteStudioService, ClientFavoriteStudioService>();
            builder.Services.AddScoped<IArtistUnavailableDateService, ArtistUnavailableDateService>();
            builder.Services.AddScoped<IProfileService, ProfileService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
            builder.Services.AddScoped<IAiTattooService, AiTattooService>();
            builder.Services.AddScoped<IArtistSubscriptionService, ArtistSubscriptionService>();
            builder.Services.AddScoped<IStripeWebhookService, StripeWebhookService>();
            builder.Services.AddScoped<IAccountDeletionService, AccountDeletionService>();
            builder.Services.AddScoped<IPurchasePayloadProtector, PurchasePayloadProtector>();
            builder.Services.AddScoped<IProviderWebhookEventService, ProviderWebhookEventService>();
            builder.Services.AddScoped<IMobileBillingService, MobileBillingService>();
            builder.Services.AddScoped<IFileStorage, LocalFileStorage>();
            builder.Services.AddScoped<IImageSanitizer, ImageSanitizer>();
            builder.Services.AddScoped<IPrivateMediaUrlService, PrivateMediaUrlService>();
            builder.Services.AddHostedService<AccountDeletionRecoveryHostedService>();
            builder.Services.AddHostedService<AiDraftCleanupHostedService>();
            builder.Services.AddHostedService<AiGenerationRecoveryHostedService>();
            builder.Services.AddHostedService<FileCleanupHostedService>();
            builder.Services.AddScoped<AppleJwsVerifier>();
            builder.Services.AddScoped<AppleServerApiClient>();
            builder.Services.AddScoped<IAuthorizationHandler, ActiveArtistSubscriptionHandler>();
            builder.Services.AddAuthorization(options => options.AddPolicy("ActiveArtistSubscription", policy => policy.Requirements.Add(new ActiveArtistSubscriptionRequirement())));
            builder.Services.AddSingleton<IPromptFileProvider, PromptFileProvider>();
            builder.Services.AddScoped<IAiTattooPromptBuilder, AiTattooPromptBuilder>();
            builder.Services.AddScoped<IAiTattooPlanner, AiTattooPlanner>();
            builder.Services.AddHttpClient();
            var forwardedHeadersEnabled = builder.Configuration.GetValue<bool>("Proxy:Enabled");
            if (forwardedHeadersEnabled)
            {
                builder.Services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    options.ForwardLimit = builder.Configuration.GetValue<int?>("Proxy:ForwardLimit") ?? 1;
                    foreach (var value in builder.Configuration.GetSection("Proxy:KnownProxies").Get<string[]>() ?? [])
                    {
                        if (!IPAddress.TryParse(value, out var address))
                            throw new InvalidOperationException($"Proxy:KnownProxies contains an invalid IP address: {value}");
                        options.KnownProxies.Add(address);
                    }
                    if (options.KnownProxies.Count == 0)
                        throw new InvalidOperationException("Proxy:Enabled requires at least one trusted Proxy:KnownProxies address.");
                });
            }
            var dataProtection=builder.Services.AddDataProtection().SetApplicationName("InkRoute");
            var keyPath=builder.Configuration["DataProtection:KeysPath"];
            if(!string.IsNullOrWhiteSpace(keyPath))
            {
                if(!Path.IsPathRooted(keyPath))throw new InvalidOperationException("DataProtection:KeysPath must be an absolute durable shared path.");
                dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyPath));
                var certificatePath=builder.Configuration["DataProtection:CertificatePath"];
                if(!string.IsNullOrWhiteSpace(certificatePath))dataProtection.ProtectKeysWithCertificate(X509CertificateLoader.LoadPkcs12FromFile(certificatePath,builder.Configuration["DataProtection:CertificatePassword"]));
            }
            StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

            builder.Services.AddDbContext<TattooDbContext>(options => 
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services
                .AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    // Email is the account identity and must never be shared by two users.
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<TattooDbContext>()
                .AddDefaultTokenProviders();

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],

                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                            var tokenVersion = context.Principal?.FindFirst("token_version")?.Value;
                            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(tokenVersion))
                            {
                                context.Fail("Session token is no longer valid.");
                                return;
                            }
                            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                            var user = await userManager.FindByIdAsync(userId);
                            if (user == null || !int.TryParse(tokenVersion, out var parsed) || parsed != user.TokenVersion)
                                context.Fail("Session token is no longer valid.");
                        }
                    };
                });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ReactApp", policy =>
                {
                    var localOrigins = builder.Environment.IsDevelopment()
                        ? new[] { "http://localhost:5173", "http://127.0.0.1:5173" }
                        : Array.Empty<string>();
                    var configuredOrigins = builder.Configuration
                        .GetSection("Cors:AllowedOrigins")
                        .GetChildren()
                        .Select(item => item.Value?.Trim().TrimEnd('/'))
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Cast<string>();

                    policy
                        .WithOrigins(localOrigins.Concat(configuredOrigins).Distinct(StringComparer.OrdinalIgnoreCase).ToArray())
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            if (forwardedHeadersEnabled) app.UseForwardedHeaders();

            // Configure the HTTP request pipeline.
            app.UseExceptionHandler();
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }
            else
            {
                app.UseHsts();
            }

            app.Use(async (context, next) =>
            {
                var incoming = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
                var correlationId = !string.IsNullOrWhiteSpace(incoming) &&
                                    incoming.Length <= 64 &&
                                    incoming.All(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.')
                    ? incoming
                    : Guid.NewGuid().ToString("N");
                context.TraceIdentifier = correlationId;
                context.Response.Headers["X-Correlation-ID"] = correlationId;
                var requestLogger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                using (requestLogger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
                {
                    await next();
                }
            });

            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["X-Frame-Options"] = "DENY";
                context.Response.Headers["Referrer-Policy"] = "no-referrer";
                context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
                await next();
            });

            app.UseHttpsRedirection();

            app.UseCors("ReactApp");

            app.UseAuthentication();

            // Rate-limit partitions may include the authenticated user id, so authentication
            // must run before the rate limiter.
            app.UseRateLimiter();

            app.Use(async (context, next) =>
            {
                if (PrivateMediaUrlService.IsPrivateStoredPath(context.Request.Path.Value))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
                await next();
            });

            app.UseStaticFiles();

            app.UseAuthorization();


            app.MapControllers();

            app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
            app.MapGet("/health/ready", async (TattooDbContext db) =>
            {
                try
                {
                    return await db.Database.CanConnectAsync()
                        ? Results.Ok(new { status = "ready" })
                        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                }
                catch
                {
                    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                }
            });

            if (app.Environment.IsDevelopment())
                AdminSeedService.EnsureAdminAsync(app.Services, app.Configuration).GetAwaiter().GetResult();

            app.Run();
        }
    }
}
