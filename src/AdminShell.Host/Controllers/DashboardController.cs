using AdminShell.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AdminShell.Host.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IPluginRepository _pluginRepo;
    private readonly IAuditLogRepository _auditLogRepo;

    public DashboardController(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IPluginRepository pluginRepo,
        IAuditLogRepository auditLogRepo)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _pluginRepo = pluginRepo;
        _auditLogRepo = auditLogRepo;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        // Run all queries in parallel
        var totalUsersTask = _userRepo.GetCountAsync();
        var activeUsersTask = _userRepo.GetActiveCountAsync();
        var totalRolesTask = _roleRepo.GetCountAsync();
        var totalPluginsTask = _pluginRepo.GetCountAsync();
        var activePluginsTask = _pluginRepo.GetActiveCountAsync();
        var auditTodayTask = _auditLogRepo.GetCountSinceAsync(DateTime.UtcNow.Date);
        var loginTodayTask = _auditLogRepo.GetCountByActionSinceAsync("LOGIN", DateTime.UtcNow.Date);
        var failedLoginTodayTask = _auditLogRepo.GetCountByActionSinceAsync("LOGIN_FAILED", DateTime.UtcNow.Date);
        var userGrowthTask = _userRepo.GetMonthlyGrowthAsync(6);
        var auditByActionTask = _auditLogRepo.GetCountByActionGroupAsync(DateTime.UtcNow.AddDays(-30));

        await Task.WhenAll(
            totalUsersTask, activeUsersTask, totalRolesTask,
            totalPluginsTask, activePluginsTask, auditTodayTask,
            loginTodayTask, failedLoginTodayTask, userGrowthTask,
            auditByActionTask);

        return Ok(new
        {
            users = new
            {
                total = totalUsersTask.Result,
                active = activeUsersTask.Result,
                inactive = totalUsersTask.Result - activeUsersTask.Result,
                monthlyGrowth = userGrowthTask.Result
            },
            roles = new
            {
                total = totalRolesTask.Result
            },
            plugins = new
            {
                total = totalPluginsTask.Result,
                active = activePluginsTask.Result
            },
            audit = new
            {
                today = auditTodayTask.Result,
                loginsToday = loginTodayTask.Result,
                failedLoginsToday = failedLoginTodayTask.Result,
                byAction = auditByActionTask.Result
            }
        });
    }
}