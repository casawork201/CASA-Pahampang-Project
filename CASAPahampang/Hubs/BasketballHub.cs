using Microsoft.AspNetCore.SignalR;

namespace CASAPahampang.Hubs;

public class BasketballHub : Hub
{
    // Broadcasts the current basketball game state to all connected scoreboards 🏀
    public async Task SendBasketballState(string stateJson)
    {
        await Clients.All.SendAsync("ReceiveBasketballState", stateJson);
    }

    // Requests an immediate state snapshot from the active scorekeeper console ⚡
    public async Task RequestBasketballState()
    {
        await Clients.Others.SendAsync("RequestBasketballStateFromHost");
    }
}