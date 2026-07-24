
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

namespace Tattoo_Project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
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
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IArtistReviewService, ArtistReviewService>();
            builder.Services.AddScoped<IClientFavoriteArtistService, ClientFavoriteArtistService>();
            builder.Services.AddScoped<IArtistUnavailableDateService, ArtistUnavailableDateService>();
            builder.Services.AddScoped<IProfileService, ProfileService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
            builder.Services.AddScoped<IAiTattooService, AiTattooService>();
            builder.Services.AddSingleton<IPromptFileProvider, PromptFileProvider>();
            builder.Services.AddScoped<IAiTattooPromptBuilder, AiTattooPromptBuilder>();
            builder.Services.AddScoped<IAiTattooPlanner, AiTattooPlanner>();
            builder.Services.AddHttpClient();

            builder.Services.AddDbContext<TattooDbContext>(options => 
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services
                .AddIdentity<ApplicationUser, IdentityRole>()
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
                });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ReactApp", policy =>
                {
                    policy
                        .WithOrigins(
                            "http://localhost:5173",
                            "http://127.0.0.1:5173"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();

            app.UseCors("ReactApp");

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseAuthorization();


            app.MapControllers();

            AdminSeedService.EnsureAdminAsync(app.Services, app.Configuration).GetAwaiter().GetResult();

            app.Run();
        }
    }
}
