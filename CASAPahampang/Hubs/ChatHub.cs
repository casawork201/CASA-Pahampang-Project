using Microsoft.AspNetCore.SignalR;

namespace CASAPahampang.Hubs;
public class ChatHub : Hub
{
    public async Task SendChatMessage(string user, string message, string avatarUrl, DateTime timestamp)
    {
        // Broadcasts the incoming chat message to all connected clients 📢
        await Clients.All.SendAsync("ReceiveChatMessage", user, message, avatarUrl, timestamp);
    }
}
