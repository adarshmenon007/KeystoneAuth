using Keystone.Application.Common.Interfaces;
using Keystone.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Keystone.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("KeystoneDb");

        services.AddDbContext<KeystoneIdentityDbContext>(options => options.UseSqlite(connectionString));

        // Demo-baseline password policy: min length 8, no complexity rules, default lockout.
        // No email confirmation / password reset in this reference implementation.
        services.AddIdentityCore<KeystoneUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<KeystoneIdentityDbContext>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => o.SigningKey.Length >= 32,
                "Jwt:SigningKey is missing or too short (need >= 32 chars). Set it via user-secrets.")
            .ValidateOnStart();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IAuthTokenService, AuthTokenService>();

        return services;
    }

    public static void ApplyMigrations(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<KeystoneIdentityDbContext>().Database.Migrate();
    }
}
