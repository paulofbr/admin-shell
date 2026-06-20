using AdminShell.Contracts;
using AdminShell.Host.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminShell.Host.Controllers;

[Authorize]
public class LogsController : ApiControllerBase
{
    private readonly ILogFileReader _logFileReader;

    public LogsController(ILogFileReader logFileReader)
    {
        _logFileReader = logFileReader;
    }

    [HttpGet]
    [ProducesResponseType(typeof(LogFilePageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LogFilePageDto>> Get(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? type = null,
        [FromQuery] string? message = null,
        CancellationToken cancellationToken = default)
    {
        var query = new LogQueryDto(skip, take, type, message);
        var page = await _logFileReader.ReadAsync(query, cancellationToken);
        return Ok(page);
    }

    [HttpGet("levels")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<string>> GetLevels()
    {
        return Ok(_logFileReader.GetLevels());
    }
}
