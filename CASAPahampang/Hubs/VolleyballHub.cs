using Microsoft.AspNetCore.SignalR;

namespace CASAPahampang.Hubs;
public class VolleyballHub : Hub
{
        // Broadcasts the current match state snapshot to all connected clients
    public async Task SendMatchState(string matchStateJson)
    {
        await Clients.All.SendAsync("ReceiveMatchState", matchStateJson);
    }

        // Allows newly connected scoreboards to request an immediate state update from the console
    public async Task RequestMatchState()
    {
        await Clients.Others.SendAsync("RequestStateFromHost");
    }
}