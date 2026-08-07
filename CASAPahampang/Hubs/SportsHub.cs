using Microsoft.AspNetCore.SignalR;

namespace CASAPahampang.Hubs;

public class SportsHub : Hub
{
    // Broadcasts real-time state snapshots for ANY sport 🏆
    public async Task SendMatchState(string matchStateJson)
    {
        await Clients.All.SendAsync("ReceiveMatchState", matchStateJson);
    }

    // Requests immediate live snapshot from scorekeeper consoles ⚡
    public async Task RequestMatchState(string? sport = null)
    {
        await Clients.Others.SendAsync("RequestStateFromHost", sport);
    }
}