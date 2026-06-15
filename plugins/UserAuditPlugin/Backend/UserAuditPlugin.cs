using AdminShell.Contracts;
using AdminShell.Core.Interfaces;

namespace UserAuditPlugin;

public class UserAuditPlugin : AdminShellPluginBase
{
    public override string Id => "useraudit";

    public override string Name => "User Audit Plugin";

    public override string Description => "Provides user audit trail, activity tabs on user pages, and global search integration";

    public override void Initialize(IServiceCollection services, IConfiguration configuration)
    {
        var loggerFactory = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("UserAuditPlugin");
        logger.LogInformation("UserAuditPlugin initialized — audit + tabs + search + page resources ready");
    }
}