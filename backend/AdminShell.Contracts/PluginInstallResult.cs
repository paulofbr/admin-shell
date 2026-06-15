namespace AdminShell.Contracts;

public sealed record PluginInstallResult(
    string PluginId,
    string PluginName,
    string Version,
    string PluginDirectory,
    bool Activated,
    IReadOnlyList<string> Messages);
