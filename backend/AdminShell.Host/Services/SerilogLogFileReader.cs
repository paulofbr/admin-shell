using System.Text;
using System.Text.Json;
using AdminShell.Contracts;
using AdminShell.Host.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdminShell.Host.Services;

public sealed class SerilogLogFileReader : ILogFileReader
{
    private const int DefaultTake = 50;
    private const int MaxTake = 200;
    private const int DefaultMaxScanBytes = 10 * 1024 * 1024;
    private const int MinMaxScanBytes = 1024 * 1024;
    private const int MaxMaxScanBytes = 100 * 1024 * 1024;
    private const int BufferSize = 64 * 1024;

    private static readonly string[] Levels =
    [
        "Verbose",
        "Debug",
        "Information",
        "Warning",
        "Error",
        "Fatal"
    ];

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SerilogLogFileReader> _logger;
    private readonly UTF8Encoding _utf8 = new(false, true);
    private readonly int _maxScanBytes;

    public SerilogLogFileReader(IConfiguration configuration, IHostEnvironment environment, ILogger<SerilogLogFileReader> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
        _maxScanBytes = Math.Clamp(
            configuration.GetValue("Logs:MaxScanBytes", DefaultMaxScanBytes),
            MinMaxScanBytes,
            MaxMaxScanBytes);
    }

    public IReadOnlyList<string> GetLevels() => Levels;

    public async Task<LogFilePageDto> ReadAsync(LogQueryDto query, CancellationToken cancellationToken = default)
    {
        var skip = Math.Max(query.Skip, 0);
        var take = Math.Clamp(query.Take <= 0 ? DefaultTake : query.Take, 1, MaxTake);
        var type = NormalizeFilter(query.Type);
        var message = NormalizeFilter(query.Message);

        var files = ResolveLogFiles();
        if (files.Length == 0)
        {
            return new LogFilePageDto([], false, 0, "No Serilog file sink path was found.");
        }

        var results = new List<LogEntryDto>(take);
        var skipped = 0;
        var scannedBytes = 0;
        var reachedScanLimit = false;

        foreach (var file in files)
        {
            await foreach (var line in ReadLinesFromTailAsync(file, cancellationToken))
            {
                scannedBytes += Encoding.UTF8.GetByteCount(line);
                var entry = ParseLine(line);
                if (entry is null || !Matches(entry, type, message))
                {
                    continue;
                }

                if (skipped < skip)
                {
                    skipped++;
                    continue;
                }

                results.Add(entry);
                if (results.Count >= take)
                {
                    return new LogFilePageDto(results, true, scannedBytes, null);
                }

                if (scannedBytes >= _maxScanBytes)
                {
                    reachedScanLimit = true;
                    break;
                }
            }

            if (reachedScanLimit)
            {
                break;
            }
        }

        var hasMore = reachedScanLimit;
        var warning = reachedScanLimit
            ? $"Scan stopped after {scannedBytes:N0} bytes. Narrow the filters or increase Logs:MaxScanBytes."
            : null;

        return new LogFilePageDto(results, hasMore, scannedBytes, warning);
    }

    private string[] ResolveLogFiles()
    {
        var configuredPath = ResolveLogFilePath();
        var directory = Path.GetDirectoryName(configuredPath);
        var fileName = Path.GetFileName(configuredPath);

        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
        {
            return [];
        }

        if (!Directory.Exists(directory))
        {
            return [];
        }

        var pattern = ToRollingFilePattern(fileName);
        return Directory
            .GetFiles(directory, pattern, SearchOption.TopDirectoryOnly)
            .Where(File.Exists)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToArray();
    }

    private string ResolveLogFilePath()
    {
        var configured = _configuration["Logs:Path"]
            ?? _configuration["Serilog:File:Path"]
            ?? FindSerilogFileSinkPath()
            ?? "logs/adminshell-.json";

        return Path.GetFullPath(configured, _environment.ContentRootPath);
    }

    private string? FindSerilogFileSinkPath()
    {
        foreach (var sink in _configuration.GetSection("Serilog:WriteTo").GetChildren())
        {
            var sinkName = sink["Name"] ?? string.Empty;
            if (!sinkName.Contains("File", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var path = sink["Args:path"] ?? sink["Args:Path"];
            if (!string.IsNullOrWhiteSpace(path))
            {
                return path;
            }
        }

        return null;
    }

    private static string ToRollingFilePattern(string fileName)
    {
        return fileName.Contains("-.", StringComparison.Ordinal)
            ? fileName.Replace("-.", "*.", StringComparison.Ordinal)
            : fileName;
    }

    private async IAsyncEnumerable<string> ReadLinesFromTailAsync(
        string path,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(path);
        if (!fileInfo.Exists || fileInfo.Length == 0)
        {
            yield break;
        }

        var length = fileInfo.Length;
        var stopPosition = Math.Max(0, length - _maxScanBytes);
        var position = length;
        var pending = string.Empty;

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        var buffer = new byte[BufferSize];

        while (position > stopPosition)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var readSize = (int)Math.Min(buffer.Length, position - stopPosition);
            position -= readSize;

            stream.Seek(position, SeekOrigin.Begin);
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, readSize), cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            var text = _utf8.GetString(buffer, 0, bytesRead);
            var combined = text + pending;
            var parts = combined.Split('\n');

            pending = parts[0].TrimEnd('\r');

            for (var i = 1; i < parts.Length; i++)
            {
                var line = parts[i].TrimEnd('\r');
                if (line.Length > 0)
                {
                    yield return line;
                }
            }
        }

        if (stopPosition == 0 && pending.Length > 0)
        {
            yield return pending.TrimEnd('\r');
        }
    }

    private static LogEntryDto? ParseLine(string line)
    {
        if (line.Length == 0)
        {
            return null;
        }

        if (line[0] == '{')
        {
            try
            {
                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;

                var timestamp = GetString(root, "@t") ?? GetString(root, "Timestamp");
                var level = GetString(root, "@l") ?? GetString(root, "Level") ?? "Information";
                var source = GetString(root, "SourceContext") ?? GetString(root, "Source");
                var message = GetRenderedMessage(root) ?? GetString(root, "@mt") ?? GetString(root, "MessageTemplate") ?? line;
                var exception = GetString(root, "@x") ?? GetString(root, "Exception");

                return new LogEntryDto(timestamp, level, source, message, exception);
            }
            catch (JsonException)
            {
                // Fall through to text parsing.
            }
        }

        return new LogEntryDto(null, TryParseLevel(line), null, line, null);
    }

    private static string? GetRenderedMessage(JsonElement root)
    {
        if (!root.TryGetProperty("@r", out var rendered) || rendered.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in rendered.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && item.GetString() is { Length: > 0 } value)
            {
                return value;
            }
        }

        return null;
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool Matches(LogEntryDto entry, string? type, string? message)
    {
        if (!string.IsNullOrWhiteSpace(type) && !string.Equals(entry.Level, type, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return true;
        }

        var haystack = $"{entry.Message} {entry.Exception} {entry.Source}";
        return haystack.Contains(message, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeFilter(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string TryParseLevel(string line)
    {
        foreach (var level in Levels)
        {
            if (line.Contains(level, StringComparison.OrdinalIgnoreCase))
            {
                return level;
            }
        }

        return "Information";
    }
}
