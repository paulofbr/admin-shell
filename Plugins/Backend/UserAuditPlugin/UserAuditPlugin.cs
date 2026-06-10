using AdminShell.Contracts;
using AdminShell.Core.Interfaces;

[assembly: PluginDependency(typeof(ReportingPlugin.ReportingPlugin), ">= 1.0.0")]

namespace UserAuditPlugin;

public class UserAuditPlugin : IAdminShellPlugin, IApiPlugin, ITabPlugin, ISearchProviderPlugin, IPageExtensionPlugin, IMenuPlugin
{
    public string Id => "useraudit";
    public string Name => "User Audit Plugin";
    public string Version => "1.0.0";
    public string Description => "Provides user audit trail, activity tabs on user pages, and global search integration";

    public void Initialize(IServiceCollection services, IConfiguration configuration)
    {
        var loggerFactory = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("UserAuditPlugin");
        logger.LogInformation("UserAuditPlugin initialized — audit + tabs + search + page resources ready");
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }

    // ───── IApiPlugin ─────

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("UserAuditPlugin");

        var group = endpoints.MapGroup("/api/plugins/useraudit")
            .WithTags("UserAudit (Plugin)");

        group.MapGet("/audit", async (CancellationToken ct) =>
        {
            logger.LogInformation("GET /api/plugins/useraudit/audit called");
            var auditLog = endpoints.ServiceProvider.GetRequiredService<IAuditLogService>();
            var logs = await auditLog.GetRecentAsync(50, 0, ct);
            var total = await auditLog.GetTotalCountAsync(ct);

            return Results.Ok(new
            {
                Data = logs.Select(l => new
                {
                    l.Id,
                    l.Action,
                    l.EntityType,
                    l.EntityId,
                    l.PerformedBy,
                    l.Details,
                    l.IpAddress,
                    Timestamp = l.CreatedAt
                }),
                Total = total
            });
        })
        .WithName("GetAuditTrail");

        group.MapGet("/audit/{action}", async (string action, CancellationToken ct) =>
        {
            logger.LogInformation("GET /api/plugins/useraudit/audit/{Action} called", action);
            var auditLog = endpoints.ServiceProvider.GetRequiredService<IAuditLogService>();
            var logs = await auditLog.GetByActionAsync(action, 50, 0, ct);

            return Results.Ok(new
            {
                Data = logs.Select(l => new
                {
                    l.Id,
                    l.Action,
                    l.EntityType,
                    l.EntityId,
                    l.PerformedBy,
                    l.Details,
                    l.IpAddress,
                    Timestamp = l.CreatedAt
                }),
                Total = logs.Count
            });
        })
        .WithName("GetAuditTrailByAction");
    }

    // ───── ITabPlugin ─────
    // Adds an "Activity" tab to the user detail/profile page

    public IEnumerable<TabDescriptor> GetTabs()
    {
        yield return new TabDescriptor
        {
            Id = "user-activity",
            Label = "Activity",
            Icon = "Timer",
            TargetPage = "users.detail",
            Order = 20,
            ComponentPath = "UserActivityTab",
            RequiredPermissions = new[] { "audit:read", "users:read" }
        };
    }

    // ───── ISearchProviderPlugin ─────
    // Contributes audit log entries to the global search

    public async Task<IEnumerable<SearchResultItem>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<SearchResultItem>();

        try
        {
            // Lowercased, trimmed query for matching
            var searchLower = query.Trim().ToLowerInvariant();
            var results = new List<SearchResultItem>();

            // Common audit actions mapped to search results
            var auditActions = new Dictionary<string, (string Title, string Icon)>
            {
                ["login"] = ("Login Event", "User"),
                ["login_failed"] = ("Failed Login Attempt", "Warning"),
                ["logout"] = ("Logout Event", "User"),
                ["user_create"] = ("User Created", "Plus"),
                ["user_update"] = ("User Updated", "Edit"),
                ["user_delete"] = ("User Deleted", "Delete"),
                ["user_register"] = ("User Registration", "UserFilled"),
            };

            foreach (var (action, (title, icon)) in auditActions)
            {
                if (action.Contains(searchLower))
                {
                    results.Add(new SearchResultItem
                    {
                        Title = title,
                        Description = $"Audit event: {action}. Click to view audit details.",
                        Url = $"/audit?action={action}",
                        Category = "Audit Log",
                        Icon = icon,
                        Score = 80
                    });

                    if (results.Count >= maxResults)
                        break;
                }
            }

            return await Task.FromResult(results.AsEnumerable());
        }
        catch
        {
            return Enumerable.Empty<SearchResultItem>();
        }
    }

    // ───── IPageExtensionPlugin ─────
    // Injects a small script that adds activity indicators to the user list

    public IEnumerable<PageResourceDescriptor> GetPageResources()
    {
        yield return new PageResourceDescriptor
        {
            Type = "style",
            Src = "/api/plugins/useraudit/resources/audit-badge.css",
            IncludePages = new[] { "/users" },
            Position = "head"
        };
    }

    // ───── IMenuPlugin ─────

    public IEnumerable<MenuItem> GetMenuItems()
    {
        yield return new MenuItem
        {
            Id = "audit",
            Label = "Audit Log",
            Icon = "List",
            Path = "/audit",
            Order = 40
        };
    }
}