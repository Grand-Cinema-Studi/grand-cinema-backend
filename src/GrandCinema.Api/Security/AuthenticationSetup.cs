using System.Threading.RateLimiting;
using GrandCinema.Domain.Enums;
using GrandCinema.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace GrandCinema.Api.Security;

internal static class AuthenticationSetup
{
    public const string AuthenticationRateLimitingPolicy = "authentication";
    public const string ManagerPolicy = "manager";
    public const string ReservationTrackingPolicy = "reservation-tracking";
    public const string TicketInspectionPolicy = "ticket-inspection";

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Configured from JwtOptions when the first request arrives, not at startup (see AddInfrastructure).
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtOptions>((options, jwt) =>
            {
                // Keep the claim names of the token ("sub", "role") instead of the long .NET claim URIs.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = jwt.CreateSigningKey(),
                    NameClaimType = AppClaimTypes.UserId,
                    RoleClaimType = AppClaimTypes.Role,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        // An endpoint that declares nothing is reserved to signed-in users: a public endpoint is never public by mistake.
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(ManagerPolicy, policy => policy.RequireRole(nameof(UserRole.Manager)))
            .AddPolicy(ReservationTrackingPolicy, policy => policy.RequireRole(nameof(UserRole.Manager), nameof(UserRole.Cashier)))
            .AddPolicy(TicketInspectionPolicy, policy => policy.RequireRole(nameof(UserRole.TicketInspector)));


        return services;
    }

    public static IServiceCollection AddAuthenticationRateLimiting(this IServiceCollection services)
    {
        // Against brute force on passwords: 10 attempts per minute and per client IP address on sign-up and sign-in.
        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthenticationRateLimitingPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });
    }
}
