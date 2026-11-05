using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskSprintBoard.Backend.Data.DbContext;

namespace TaskSprintBoard.Backend.Presentation.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var configuredJwtKey = configuration["Jwt:Key"];
        var jwtKey = !string.IsNullOrWhiteSpace(configuredJwtKey)
            ? configuredJwtKey
            : Environment.GetEnvironmentVariable("TASKSPRINTBOARD_JWT_KEY");
        if (string.IsNullOrWhiteSpace(jwtKey))
            throw new InvalidOperationException("JWT signing key is missing.");

        const string jwtIssuer = "TaskSprintBoard";
        const string jwtAudience = "TaskSprintBoardClients";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                NameClaimType = "username",
                RoleClaimType = ClaimTypes.Role,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromMinutes(1)
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var userIdValue = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!int.TryParse(userIdValue, out var userId) || userId <= 0)
                    {
                        context.Fail("The token does not identify a valid user.");
                        return;
                    }

                    var dbContext = context.HttpContext.RequestServices.GetRequiredService<TaskSprintBoardDbContext>();
                    var userIsActive = await dbContext.Users.AsNoTracking()
                        .AnyAsync(user => user.Id == userId && user.IsActive && !user.IsDeleted);
                    if (!userIsActive)
                        context.Fail("This account is inactive or has been deleted.");
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
            options.AddPolicy("RequireProjectManager", policy => policy.RequireRole("ProjectManager"));
            options.AddPolicy("RequireTeamMember", policy => policy.RequireRole("TeamMember"));
        });

        return services;
    }
}
