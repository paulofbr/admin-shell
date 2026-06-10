using AdminShell.Contracts;
using AdminShell.Core.Interfaces;
using AdminShell.Infrastructure.Data;
using AdminShell.Infrastructure.Data.Repositories;
using AdminShell.Infrastructure.PluginSystem;
using AdminShell.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AdminShell.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=AdminShell;Trusted_Connection=true;TrustServerCertificate=true;";

        // Database connection factory (SQL Server)
        services.AddSingleton<IDbConnectionFactory>(sp =>
            new SqlConnectionFactory(connectionString));

        // Database initializer (creates tables + seeds on startup)
        services.AddSingleton<DatabaseInitializer>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPluginRepository, PluginRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<IPluginLoader, PluginLoader>();
        services.AddSingleton<IHealthCheckService, HealthCheckService>();
        services.AddHostedService<PluginWatcher>();

        // Plugin Extension Registry
        services.AddSingleton<IPluginExtensionRegistry>(sp =>
        {
            var pluginLoader = sp.GetRequiredService<IPluginLoader>();
            var logger = sp.GetRequiredService<ILogger<PluginExtensionRegistry>>();

            // Manually collect plugin instances after DI is ready
            var instances = pluginLoader.GetPluginInstances();
            // Wrap in a lazy-resolving collection since DI plugins may not be registered yet
            return new PluginExtensionRegistry(instances, logger);
        });

        return services;
    }
}