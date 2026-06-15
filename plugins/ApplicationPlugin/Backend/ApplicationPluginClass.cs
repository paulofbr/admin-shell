using AdminShell.Contracts;

namespace ApplicationPlugin;

public sealed class ApplicationPlugin : AdminShellPluginBase
{
    public override string Id => "application";

    public override string Name => "Application Configuration Plugin";

    public override string Version => "1.0.0";

    public override string Description => "Plugin que permite alterar configurações base da aplicação, como nome, subtítulo, ícone e favicon.";
}
