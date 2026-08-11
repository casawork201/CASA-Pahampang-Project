using CASAPahampang.Interfaces;
using Microsoft.AspNetCore.SignalR;

public class ChatHub : Hub
{
    private static int _onlineUserCount = 0;
    private readonly IContentModerationService _moderationService;

    public ChatHub(IContentModerationService moderationService)
    {
        _moderationService = moderationService;
    }

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
        // Ensure the downloaded blocklists are initialized 🚀
        await _moderationService.InitializeAsync();

        // Check content with the local English & Tagalog moderation service 🔍
        bool isFlagged = _moderationService.IsFlagged(message);
        string finalMessage = isFlagged ? "⚠️ [Message flagged by moderation]" : message;

        await Clients.All.SendAsync("ReceiveChatMessage", user, finalMessage, avatarUrl, timestamp);
    }

    public async Task SendSignal(string targetConnectionId, string type, string payload)
    {
        await Clients.Client(targetConnectionId).SendAsync("ReceiveSignal", Context.ConnectionId, type, payload);
    }

    public async Task BroadcastWebcamStarted(string user)
    {
        await Clients.Others.SendAsync("WebcamStarted", Context.ConnectionId, user);
    }

    public async Task BroadcastWebcamStopped()
    {
        await Clients.Others.SendAsync("WebcamStopped", Context.ConnectionId);
    }
}