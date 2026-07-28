using Microsoft.AspNetCore.SignalR;
using CASAPahampang.Client.Dtos; // Adjust namespace to match your Dto location

namespace CASAPahampang.Hubs;

public class TeamHub : Hub
{
    public async Task SendTeamAdded(TeamDto team)
    {
        await Clients.All.SendAsync("ReceiveTeamAdded", team);
    }

    public async Task SendTeamUpdated(TeamDto team)
    {
        await Clients.All.SendAsync("ReceiveTeamUpdated", team);
    }

    public async Task SendTeamDeleted(Guid teamId)
    {
        await Clients.All.SendAsync("ReceiveTeamDeleted", teamId);
    }
}