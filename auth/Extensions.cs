using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;

namespace MicroAuth
{
    public static class Extensions
    {
        public static void UseMicroAuth(this IApplicationBuilder builder)
        {
            builder.UseAuthentication();
            builder.UseAuthorization();
        }

        public static void AddMicroAuth(this IServiceCollection services)
        {
            string? tokenKey = Environment.GetEnvironmentVariable("TOKEN_KEY");

            if (string.IsNullOrEmpty(tokenKey))
            {
                throw new ApplicationException("Token key is not specified");
            }

            byte[] keyData = Encoding.UTF8.GetBytes(tokenKey);

            SymmetricSecurityKey key = new SymmetricSecurityKey(keyData);

            services.AddAuthorization(options => {
                options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();
            });

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            RequireAudience = true,
                            RequireExpirationTime = true,
                            RequireSignedTokens = true,
                            ValidateAudience = true,
                            ValidAudience = Environment.GetEnvironmentVariable("TOKEN_AUDIENCE"),
                            ValidateIssuer = true,
                            ValidIssuer = Environment.GetEnvironmentVariable("TOKEN_ISSUER"),
                            ValidateActor = false,
                            ValidateLifetime = true,
                            IssuerSigningKey = key
                        };
                });
        }
    }
}