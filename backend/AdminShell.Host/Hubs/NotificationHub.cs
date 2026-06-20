using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AdminShell.Host.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

        await base.OnConnectedAsync();
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task SubscribePlugin(string pluginId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"plugin:{pluginId}");
    }

    public async Task UnsubscribePlugin(string pluginId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"plugin:{pluginId}");
    }
}
