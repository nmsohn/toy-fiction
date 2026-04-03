using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using NarrativeEngine.Api.Services.Auth;
using NarrativeEngine.Api.Services.Interfaces;
using NarrativeEngine.Api.Settings;
using NarrativeEngine.Domain.Entities;

namespace NarrativeEngine.Api.Extensions;

public static class AuthInjections
{
    public static void AddAuthInjection(this IServiceCollection services, IConfiguration configuration)
    {
        // ES256 (ECDSA P-256) JWT Auth 
        var publicKeyBase64 = configuration["Jwt:PublicKeyBase64"]
                              ?? throw new InvalidOperationException("Jwt:PublicKeyBase64 config is not found.");

        var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(Encoding.UTF8.GetString(Convert.FromBase64String(publicKeyBase64)));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"], //iss
                    ValidAudience = configuration["Jwt:Audience"], //aud
                    IssuerSigningKey = new ECDsaSecurityKey(ecdsa),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    // SignalR: read an access token from a query string
                    OnMessageReceived = ctx =>
                    {
                        var token = ctx.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(token) &&
                            ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                            ctx.Token = token;
                        return Task.CompletedTask;
                    },
                    // When JWT auth is successful, the X-User-Id header is filled (ClientRateLimiting)
                    OnTokenValidated = ctx =>
                    {
                        var userId = ctx.Principal?.FindFirst("sub")?.Value; //sub
                        if (!string.IsNullOrEmpty(userId))
                            ctx.HttpContext.Request.Headers["X-User-Id"] = userId;
                        return Task.CompletedTask;
                    }
                };
            });

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        
        // Built-in RateLimiter — auth-ip policy for /api/auth/login and /api/auth/register
        services.AddRateLimiter(o =>
        {
            o.AddPolicy("auth-ip", ctx =>
                RateLimitPartition.GetTokenBucketLimiter(
                    ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 10, // max token
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1), // token frequency
                        TokensPerPeriod = 5, // number of tokens added
                        QueueLimit = 0
                    }));
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        }); 
        
        // Rate Limiting — per user (ClientRateLimiting, X-User-Id header based)
        services.Configure<ClientRateLimitOptions>(configuration.GetSection("ClientRateLimiting"));
        services.Configure<ClientRateLimitPolicies>(
            configuration.GetSection("ClientRateLimitPolicies"));
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        
        // Rate Limiting — based on IP (/auth/login, /auth/register endpoint protection)
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));
        services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
    }
}