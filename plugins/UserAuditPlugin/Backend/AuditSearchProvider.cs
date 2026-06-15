using AdminShell.Contracts;

namespace UserAuditPlugin;

[PluginComponent("user-audit")]
public sealed class AuditSearchProvider : ISearchProviderPlugin
{
    public async Task<IEnumerable<SearchResultItem>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<SearchResultItem>();

        try
        {
            var searchLower = query.Trim().ToLowerInvariant();
            var results = new List<SearchResultItem>();

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
}
