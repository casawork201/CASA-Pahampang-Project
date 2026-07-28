using Microsoft.AspNetCore.SignalR;

public class ChatHub : Hub
{
    private static int _onlineUserCount = 0;

    public override async Task OnConnectedAsync()
    {
        Interlocked.Increment(ref _onlineUserCount);
        await Clients.All.SendAsync("UpdateOnlineCount", _onlineUserCount);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Interlocked.Decrement(ref _onlineUserCount);
        await Clients.All.SendAsync("UpdateOnlineCount", Math.Max(0, _onlineUserCount));
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendChatMessage(string user, string message, string avatarUrl, DateTime timestamp)
    {
        await Clients.All.SendAsync("ReceiveChatMessage", user, message, avatarUrl, timestamp);
    }
}