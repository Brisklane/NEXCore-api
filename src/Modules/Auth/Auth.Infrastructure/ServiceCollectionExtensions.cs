using Auth.Application.Services.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Interfaces;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Infrastructure;

/// <summary>
/// Composition root for the Auth module: wires up the DbContext (with SQL retry) and registers
/// every auth service against its interface so the host only calls <see cref="AddAuthInfrastructure"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AuthDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b =>
                {
                    b.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    b.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName);
                }));

        // Domain Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IAuditService, AuditService>();

        // Application Services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserValidator, UserValidator>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        return services;
    }
}
