using AdminShell.Contracts;

namespace PluginName;

public sealed class PluginNamePlugin : AdminShellPluginBase
{
    public override string Id => "plugin-id";

    public override string Name => "Plugin Title";

    public override string Description => "__PluginDescription__";
}
