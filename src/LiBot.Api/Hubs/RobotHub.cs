using Microsoft.AspNetCore.SignalR;

namespace LiBot.Api.Hubs;

public class RobotHub : Hub
{
    public async Task SendHeartbeat(string robotId, string status)
    {
        await Clients.All.SendAsync("ReceiveHeartbeat", robotId, status, DateTime.UtcNow);
    }
}
