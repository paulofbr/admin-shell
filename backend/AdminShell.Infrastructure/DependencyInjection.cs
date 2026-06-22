using AdminShell.Contracts;
using AdminShell.Core.Interfaces;
using AdminShell.Infrastructure.Data;
using AdminShell.Infrastructure.Data.Repositories;
using AdminShell.Infrastructure.Mappings;
using AdminShell.Infrastructure.PluginSystem;
using AdminShell.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
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
        services.AddSingleton<IManagedEntityProvider, CoreManagedEntityProvider>();
        services.AddSingleton<IManagedEntitySchemaManager, ManagedEntitySchemaManager>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPluginRepository, PluginRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // Mapper
        services.AddSingleton<AppMapper>();

        // Services
        services.AddSingleton<ICacheService, CacheService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<IPluginLoader, PluginLoader>();
        services.AddSingleton<IPluginInstaller, PluginInstaller>();
        services.AddSingleton<IPermissionDefinitionRegistry>(sp =>
        {
            var registry = new PermissionDefinitionRegistry();
            registry.Discover(AppDomain.CurrentDomain.GetAssemblies());
            return registry;
        });
        services.AddSingleton<IHealthCheckService, HealthCheckService>();
        services.AddSingleton<IQueryRegistry, PluginQueryRegistry>();
        services.AddHostedService<PluginWatcher>();
        services.AddOptions<AuthorizationOptions>()
            .Configure<IPermissionDefinitionRegistry>((options, registry) =>
            {
                foreach (var permission in registry.GetAll())
                {
                    if (options.GetPolicy(permission.PolicyName) is not null)
                        continue;

                    options.AddPolicy(permission.PolicyName, policy =>
                        policy.RequireAssertion(context => context.User.HasPermission(permission.Code)));
                }
            });

        // Plugin Extension Registry
        services.AddSingleton<IPluginExtensionRegistry>(sp =>
        {
            var pluginLoader = sp.GetRequiredService<IPluginLoader>();
            var logger = sp.GetRequiredService<ILogger<PluginExtensionRegistry>>();

            // Manually collect plugin components after DI is ready
            var components = pluginLoader.GetPluginComponents();
            // Wrap in a lazy-resolving collection since DI plugins may not be registered yet
            return new PluginExtensionRegistry(components, logger, pluginLoader);
        });

        services.AddSingleton<ISettingsRegistry>(sp =>
        {
            var pluginLoader = sp.GetRequiredService<IPluginLoader>();
            var connectionFactory = sp.GetRequiredService<IDbConnectionFactory>();
            var logger = sp.GetRequiredService<ILogger<SettingsRegistry>>();
            return new SettingsRegistry(connectionFactory, logger, pluginLoader);
        });
        services.AddScoped(typeof(ISettingsAccessor<>), typeof(SettingsAccessor<>));

        // Health checks
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["db", "core"]);

        return services;
    }
}