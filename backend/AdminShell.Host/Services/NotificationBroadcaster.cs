using AdminShell.Contracts;
using Microsoft.AspNetCore.SignalR;
using AdminShell.Host.Hubs;

namespace AdminShell.Host.Services;

public interface INotificationBroadcaster
{
    Task NotifyAllAsync(string method, object? payload, CancellationToken ct = default);
    Task NotifyUserAsync(string userId, string method, object? payload, CancellationToken ct = default);
    Task NotifyGroupAsync(string group, string method, object? payload, CancellationToken ct = default);
    Task NotifyPluginSubscribersAsync(string pluginId, string method, object? payload, CancellationToken ct = default);
}

public class NotificationBroadcaster : INotificationBroadcaster
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationBroadcaster(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyAllAsync(string method, object? payload, CancellationToken ct = default)
        => await _hubContext.Clients.All.SendAsync(method, payload, ct);

    public async Task NotifyUserAsync(string userId, string method, object? payload, CancellationToken ct = default)
        => await _hubContext.Clients.Group($"user:{userId}").SendAsync(method, payload, ct);

    public async Task NotifyGroupAsync(string group, string method, object? payload, CancellationToken ct = default)
        => await _hubContext.Clients.Group(group).SendAsync(method, payload, ct);

    public async Task NotifyPluginSubscribersAsync(string pluginId, string method, object? payload, CancellationToken ct = default)
        => await _hubContext.Clients.Group($"plugin:{pluginId}").SendAsync(method, payload, ct);
}
