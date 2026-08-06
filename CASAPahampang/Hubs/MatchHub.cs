using Microsoft.AspNetCore.SignalR;
using CASAPahampang.Client.Dtos;

namespace CASAPahampang.Hubs;

public class MatchHub : Hub
{
    public async Task SendMatchAdded(MatchDto match)
    {
        await Clients.All.SendAsync("ReceiveMatchAdded", match);
    }

    public async Task SendMatchUpdated(MatchDto match)
    {
        await Clients.All.SendAsync("ReceiveMatchUpdated", match);
    }

    public async Task SendMatchDeleted(Guid matchId)
    {
        await Clients.All.SendAsync("ReceiveMatchDeleted", matchId);
    }
}