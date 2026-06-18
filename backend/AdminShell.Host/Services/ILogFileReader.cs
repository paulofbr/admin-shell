using AdminShell.Contracts;

namespace AdminShell.Host.Services;

public interface ILogFileReader
{
    Task<LogFilePageDto> ReadAsync(LogQueryDto query, CancellationToken cancellationToken = default);

    IReadOnlyList<string> GetLevels();
}
