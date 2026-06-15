using AdminShell.Contracts;

namespace AdminShell.Core.Interfaces;

public interface IPluginInstaller
{
    Task<PluginInstallResult> InstallAsync(
        Stream zipStream,
        string fileName,
        long length,
        bool activate,
        CancellationToken ct = default);
}
