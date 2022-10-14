using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Text;
using MicroAuth;
using MicroIdentity.Infrastructure;
using MicroIdentity.Models;
using MicroIdentity.Services;
using System;

namespace MicroIdentity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Additional configuration is required to successfully run gRPC on macOS.
            // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

            string connectionString = builder.Configuration.GetConnectionString("MicroIdentity");
            builder.Services.AddDbContext<AppDbContext>(options =>
                //options.UseSqlServer(connectionString));
                options.UseSqlite(connectionString));

            builder.Services.AddIdentity<AppUser, AppRole>(options =>
                {
                    options.ClaimsIdentity.UserIdClaimType = ClaimTypes.NameIdentifier;
                    options.User.RequireUniqueEmail = true;

                    // Default Lockout settings.
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;

                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = 8;
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            if(builder.Environment.EnvironmentName.ToLower() == "dbcontext") {
                builder.Build();
                return;
            }

            builder.Services.AddGrpc();

            builder.Services.AddMicroAuth();

            var app = builder.Build();

            app.UseMicroAuth();

            app.MapGrpcService<AccountsService>();
            app.MapGrpcService<ManageService>();
            app.MapGrpcService<UsersService>();
            app.MapGrpcService<RolesService>();

            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
            var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
            var url = $"http://0.0.0.0:{port}";
            app.Run(url);
        }

        /// <summary>
        /// Generates Identity API errors description
        /// </summary>
        /// <param name="result">Result of Identity API method call</param>
        public static string GetErrorsFromResult(IdentityResult result)
        {
            StringBuilder sb = new StringBuilder();
            foreach (IdentityError error in result.Errors)
            {
                sb.AppendFormat(" {0}", error.Description);
            }

            return sb.ToString();
        }
    }
}