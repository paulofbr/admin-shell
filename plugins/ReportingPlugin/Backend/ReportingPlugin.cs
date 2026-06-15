using AdminShell.Contracts;

namespace ReportingPlugin;

public class ReportingPlugin : AdminShellPluginBase
{
    public override string Id => "reporting";

    public override string Name => "Reporting Plugin";

    public override string Description => "Provides reporting capabilities with widgets, reports API, and menu items";

    public override void Initialize(IServiceCollection services, IConfiguration configuration)
    {
        var loggerFactory = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("ReportingPlugin");
        logger.LogInformation("ReportingPlugin initialized — widgets + menu + API + reports + toolbar ready");
    }
}