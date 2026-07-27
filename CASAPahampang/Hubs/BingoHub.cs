using Microsoft.AspNetCore.SignalR;

namespace CASAPahampang.Hubs;

public class BingoHub : Hub
{
    public async Task SendGameState(string jsonState)
    {
        // Broadcasts to all connected clients (Display screens)
        await Clients.All.SendAsync("ReceiveGameState", jsonState);
    }
}