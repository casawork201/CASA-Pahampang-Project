using CASAPahampang.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class ChatHub : Hub
{
    private static int _onlineUserCount = 0;
    
    // 💡 Track active broadcasters per room: RoomName -> (ConnectionId, UserName)
    private static readonly ConcurrentDictionary<string, (string ConnectionId, string UserName)> _activeBroadcasters = new();
    
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

        // 💡 Clean up if a broadcaster disconnects unexpectedly 🛑
        foreach (var kvp in _activeBroadcasters)
        {
            if (kvp.Value.ConnectionId == Context.ConnectionId)
            {
                _activeBroadcasters.TryRemove(kvp.Key, out _);
                await Clients.OthersInGroup(kvp.Key).SendAsync("WebcamStopped", Context.ConnectionId);
                break;
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRoom(string roomName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

        // 💡 If a broadcast is already active in this room, immediately notify the newly joined viewer! 📺✨
        if (_activeBroadcasters.TryGetValue(roomName, out var broadcaster))
        {
            await Clients.Caller.SendAsync("WebcamStarted", broadcaster.ConnectionId, broadcaster.UserName);
        }
    }

    public async Task LeaveRoom(string roomName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
    }

    // public async Task SendChatMessage(string user, string message, string avatarUrl, DateTime timestamp)
    // {
    //     await _moderationService.InitializeAsync();
    //     bool isFlagged = _moderationService.IsFlagged(message);
    //     string finalMessage = isFlagged ? "⚠️ [Message flagged by moderation]" : message;

    //     await Clients.All.SendAsync("ReceiveChatMessage", user, finalMessage, avatarUrl, timestamp);
    // }
// 🌟 1. Global/Legacy Chat (Safe for Dashboard and other pages)
    // 🌍 1. Global/Legacy Chat for your Dashboard
    public async Task SendChatMessage(string user, string message, string avatarUrl, DateTime timestamp)
    {
        await _moderationService.InitializeAsync();
        bool isFlagged = _moderationService.IsFlagged(message);
        string finalMessage = isFlagged ? "⚠️ [Message flagged by moderation]" : message;

        await Clients.All.SendAsync("ReceiveChatMessage", user, finalMessage, avatarUrl, timestamp);
    }

    // 🏟️ 2. Room-Scoped Chat for Mobile Legends (Distinct method name!)
    public async Task SendRoomChatMessage(string roomName, string user, string message, string avatarUrl, DateTime timestamp)
    {
        await _moderationService.InitializeAsync();
        bool isFlagged = _moderationService.IsFlagged(message);
        string finalMessage = isFlagged ? "⚠️ [Message flagged by moderation]" : message;

        await Clients.Group(roomName).SendAsync("ReceiveChatMessage", user, finalMessage, avatarUrl, timestamp);
    }
    public async Task SendSignal(string targetConnectionId, string type, string payload)
    {
        await Clients.Client(targetConnectionId).SendAsync("ReceiveSignal", Context.ConnectionId, type, payload);
    }

    public async Task BroadcastWebcamStarted(string roomName, string user)
    {
        // Store the active broadcast state for late-joining viewers 📝
        _activeBroadcasters[roomName] = (Context.ConnectionId, user);
        await Clients.OthersInGroup(roomName).SendAsync("WebcamStarted", Context.ConnectionId, user);
    }

    public async Task BroadcastWebcamStopped(string roomName)
    {
        // Clear the active broadcast state 🛑
        _activeBroadcasters.TryRemove(roomName, out _);
        await Clients.OthersInGroup(roomName).SendAsync("WebcamStopped", Context.ConnectionId);
    }
}