using System.Net.Mime;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RepairShop.Application.Auth;
using RepairShop.Infrastructure.Identity;

namespace RepairShop.Api.Auth;

/// <summary>Authentication, authorisation and login throttling for the API.</summary>
public static class AuthSetup
{
    public const string LoginRateLimit = "login";

    public static IServiceCollection AddApiAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured from the same JwtOptions the issuer uses, so the two cannot disagree.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.SigningKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = AuthClaims.Name,
                    RoleClaimType = AuthClaims.Role,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                bearer.Events = new JwtBearerEvents
                {
                    // Same problem-details shape as every other error of this API.
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await WriteProblemAsync(context.HttpContext, StatusCodes.Status401Unauthorized,
                            "Not signed in", "A valid bearer token is required. Sign in via POST /api/auth/token.");
                    },
                    OnForbidden = context => WriteProblemAsync(context.HttpContext, StatusCodes.Status403Forbidden,
                        "Forbidden", "Your role may not do this.")
                };
            });

        services.AddAuthorizationBuilder()
            // Closed by default: an endpoint without [AllowAnonymous] needs a login,
            // so a new controller can never be public by accident.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        // Slows down password guessing: per client address, 10 attempts a minute
        // unless configured otherwise (Auth:LoginAttemptsPerMinute).
        var attemptsPerMinute = configuration.GetValue("Auth:LoginAttemptsPerMinute", 10);
        services.AddRateLimiter(limiter =>
        {
            limiter.AddPolicy(LoginRateLimit, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = attemptsPerMinute, Window = TimeSpan.FromMinutes(1) }));
            limiter.OnRejected = (context, _) => new ValueTask(WriteProblemAsync(
                context.HttpContext, StatusCodes.Status429TooManyRequests,
                "Too many attempts", "Too many sign-in attempts. Please wait a minute."));
        });

        return services;
    }

    private static Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = MediaTypeNames.Application.ProblemJson;
        return context.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path,
                Extensions = { ["traceId"] = context.TraceIdentifier }
            },
            options: null,
            contentType: MediaTypeNames.Application.ProblemJson);
    }
}
