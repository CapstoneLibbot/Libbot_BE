using Microsoft.AspNetCore.SignalR;

namespace LiBot.Api.Hubs;

public class RobotHub : Hub
{
    public const string LibrarianGroup = "librarians";

    public static string RuntimeGroup(string runtimeId) => $"runtime:{runtimeId.ToLowerInvariant()}";

    public Task SubscribeLibrarian() => Groups.AddToGroupAsync(Context.ConnectionId, LibrarianGroup);

    public Task UnsubscribeLibrarian() => Groups.RemoveFromGroupAsync(Context.ConnectionId, LibrarianGroup);

    public Task SubscribeRuntime(string runtimeId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, RuntimeGroup(runtimeId));

    public Task UnsubscribeRuntime(string runtimeId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, RuntimeGroup(runtimeId));

    public async Task SendHeartbeat(string robotId, string status)
    {
        await Clients.All.SendAsync("ReceiveHeartbeat", robotId, status, DateTime.UtcNow);
    }
}
