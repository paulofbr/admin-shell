// <copyright file="Plugin.cs" company="Admin Shell">
// Copyright (c) Admin Shell. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using AdminShell.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdminShellPlugin;

/// <summary>
/// Main plugin class implementing the admin-shell plugin contract.
///
/// This template demonstrates all available plugin extension points.
/// To customise, edit the properties and method bodies below, then add
/// your own business logic, services, and endpoints.
///
/// Supported interfaces (from AdminShell.Contracts):
///   - <see cref="IAdminShellPlugin"/> (required) — base lifecycle and metadata
///   - <see cref="IApiPlugin"/>          — registers minimal API endpoints
///   - <see cref="IDataPlugin"/>         — contributes EF Core entity configuration
///   - <see cref="IWidgetPlugin"/>       — provides dashboard widgets
///   - <see cref="IMenuPlugin"/>         — contributes sidebar / navigation items
///
/// Remove any interfaces your plugin does not need.
/// </summary>
public class Plugin : IAdminShellPlugin, IApiPlugin
{
    // ──────────────────────────────────────────────
    //  IAdminShellPlugin — Required
    // ──────────────────────────────────────────────

    /// <summary>
    /// Gets the unique identifier for this plugin.
    /// Must be lowercase, no spaces. Used by the plugin loader for
    /// dependency resolution and internal routing.
    /// </summary>
    public string Id => "PLUGIN_ID_PLACEHOLDER";

    /// <summary>
    /// Gets the human-readable display name shown in the admin UI.
    /// </summary>
    public string Name => "PLUGIN_DISPLAY_NAME_PLACEHOLDER";

    /// <summary>
    /// Gets the semantic version of this plugin (SemVer).
    /// </summary>
    public string Version => "PLUGIN_VERSION_PLACEHOLDER";

    /// <summary>
    /// Gets a short description of the plugin's purpose and capabilities.
    /// </summary>
    public string Description => "PLUGIN_DESCRIPTION_PLACEHOLDER";

    /// <summary>
    /// Called during application startup to register services in the DI container.
    /// Use this method to add scoped/transient/singleton services, configure
    /// options from IConfiguration, or register background tasks.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration root.</param>
    public void Initialize(IServiceCollection services, IConfiguration configuration)
    {
        // Example: register a scoped service
        // services.AddScoped<IMyService, MyService>();

        // Example: bind a configuration section
        // services.Configure<MyPluginOptions>(configuration.GetSection("MyPlugin"));

        var loggerFactory = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(Name);
        logger.LogInformation("{PluginName} v{Version} initialized", Name, Version);
    }

    /// <summary>
    /// Called after the application pipeline is built. Use this to register
    /// custom middleware or perform post-build configuration.
    /// Most plugins can leave this empty and use <see cref="MapEndpoints"/> instead.
    /// </summary>
    /// <param name="app">The application builder for middleware configuration.</param>
    /// <param name="env">The current web hosting environment.</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Example: add custom middleware
        // app.UseMiddleware<MyPluginMiddleware>();
    }

    // ──────────────────────────────────────────────
    //  IApiPlugin — Optional
    // ──────────────────────────────────────────────

    /// <summary>
    /// Registers minimal API endpoints contributed by this plugin.
    /// Endpoints are grouped under /api/plugins/{pluginId} by convention.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(Name);

        var group = endpoints.MapGroup($"/api/plugins/{Id}")
            .WithTags($"{Name} (Plugin)");

        // Health check endpoint
        group.MapGet("/health", () =>
        {
            logger.LogDebug("Health check called for {PluginName}", Name);
            return Results.Ok(new
            {
                Plugin = Id,
                Status = "healthy",
                Version = Version
            });
        })
        .WithName($"Get{Name.Replace(" ", "")}Health")
        .WithDescription("Health check endpoint for the plugin");

        // ── Add your custom endpoints below ──
        //
        // group.MapGet("/items", async (CancellationToken ct) =>
        // {
        //     var items = await someService.GetAllAsync(ct);
        //     return Results.Ok(items);
        // })
        // .WithName("GetItems")
        // .WithDescription("Retrieves all items");
        //
        // group.MapPost("/items", async (CreateItemRequest request, CancellationToken ct) =>
        // {
        //     var item = await someService.CreateAsync(request, ct);
        //     return Results.Created($"/api/plugins/{Id}/items/{item.Id}", item);
        // })
        // .WithName("CreateItem")
        // .WithDescription("Creates a new item");
    }
}