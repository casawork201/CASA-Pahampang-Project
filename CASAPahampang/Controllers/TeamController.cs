using CASAPahampang.Client.Dtos;
using CASAPahampang.Data;
using CASAPahampang.Hubs;
using CASAPahampang.Models;
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
    public async Task<ActionResult<IEnumerable<TeamDto>>> GetTeams()
    {
        var teams = await _context.Teams
            .AsNoTracking()
            .Select(t => t.ToDto())
            .ToListAsync();

        return Ok(teams);
    }

    // GET: api/team/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TeamDto>> GetTeam(Guid id)
    {
        var team = await _context.Teams.FindAsync(id);

        if (team == null)
        {
            return NotFound();
        }

        return Ok(team.ToDto());
    }

    // POST: api/team
    [HttpPost]
    public async Task<ActionResult<TeamDto>> CreateTeam([FromBody] TeamDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var team = new Team
        {
            Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
            Name = dto.Name,
            Logo = dto.Logo,
            Record = dto.Record
        };

        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        var createdDto = team.ToDto();

        // ⚡ Broadcast new team creation via SignalR
        await _basketballHub.Clients.All.SendAsync("ReceiveTeamCreated", createdDto);

        return CreatedAtAction(nameof(GetTeam), new { id = createdDto.Id }, createdDto);
    }

    // PUT: api/team/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTeam(Guid id, [FromBody] TeamDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest("Team ID mismatch.");
        }

        var team = await _context.Teams.FindAsync(id);
        if (team == null)
        {
            return NotFound();
        }

        // Update properties
        team.Name = dto.Name;
        team.Logo = dto.Logo;
        team.Record = dto.Record;

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

        // ⚡ Broadcast real-time update to all connected clients
        await _basketballHub.Clients.All.SendAsync("ReceiveTeamUpdated", dto);

        return NoContent();
    }

    // DELETE: api/team/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTeam(Guid id)
    {
        var team = await _context.Teams.FindAsync(id);
        if (team == null)
        {
            return NotFound();
        }

        _context.Teams.Remove(team);
        await _context.SaveChangesAsync();

        // ⚡ Broadcast deletion
        await _basketballHub.Clients.All.SendAsync("ReceiveTeamDeleted", id);

        return NoContent();
    }

    private bool TeamExists(Guid id)
    {
        return _context.Teams.Any(e => e.Id == id);
    }
}

// 🛠️ Internal Entity-to-DTO Mapping Extensions
internal static class TeamMappingExtensions
{
    public static TeamDto ToDto(this Team team)
    {
        return new TeamDto
        {
            Id = team.Id,
            Name = team.Name,
            Logo = team.Logo,
            Record = team.Record
        };
    }
}