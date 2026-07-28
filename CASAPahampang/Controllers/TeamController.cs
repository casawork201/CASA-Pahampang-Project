using CASAPahampang.Data;
using CASAPahampang.Hubs;
using CASAPahampang.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CASAPahampang.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TeamController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<BasketballHub> _basketballHub;

    public TeamController(
        ApplicationDbContext context, 
        IHubContext<BasketballHub> basketballHub)
    {
        _context = context;
        _basketballHub = basketballHub;
    }

    // GET: api/team
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Team>>> GetTeams()
    {
        return await _context.Teams.AsNoTracking().ToListAsync();
    }

    // GET: api/team/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Team>> GetTeam(int id)
    {
        var team = await _context.Teams.FindAsync(id);

        if (team == null)
        {
            return NotFound();
        }

        return team;
    }

    // POST: api/team
    [HttpPost]
    public async Task<ActionResult<Team>> CreateTeam([FromBody] Team team)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        // ⚡ Broadcast new team creation to all SignalR clients
        await _basketballHub.Clients.All.SendAsync("ReceiveTeamCreated", team);

        return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, team);
    }

    // PUT: api/team/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTeam(Guid id, [FromBody] Team team)
    {
        if (id != team.Id)
        {
            return BadRequest("Team ID mismatch.");
        }

        _context.Entry(team).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TeamExists(id))
            {
                return NotFound();
            }
            throw;
        }

        // ⚡ Broadcast real-time team update (score, logo, record, etc.)
        await _basketballHub.Clients.All.SendAsync("ReceiveTeamUpdated", team);

        return NoContent();
    }

    // DELETE: api/team/5
    [HttpDelete("{id:Guid}")]
    public async Task<IActionResult> DeleteTeam(Guid id)
    {
        var team = await _context.Teams.FindAsync(id);
        if (team == null)
        {
            return NotFound();
        }

        _context.Teams.Remove(team);
        await _context.SaveChangesAsync();

        // ⚡ Broadcast team deletion to all SignalR clients
        await _basketballHub.Clients.All.SendAsync("ReceiveTeamDeleted", id);

        return NoContent();
    }

    private bool TeamExists(Guid id)
    {
        return _context.Teams.Any(e => e.Id == id);
    }
}