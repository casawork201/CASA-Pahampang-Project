using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using CASAPahampang.Data;
using CASAPahampang.Models;
using CASAPahampang.Hubs;
using CASAPahampang.Client.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TestWASM.AuthLib.Services;

namespace CASAPahampang.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
// [Authorize(Policy = "is-admin")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "is-admin")]
public class TeamController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<TeamHub> _teamHubContext;

    public TeamController(ApplicationDbContext context, IHubContext<TeamHub> teamHubContext)
    {
        _context = context;
        _teamHubContext = teamHubContext;
        Console.WriteLine("This was called");
    }

    [HttpGet("debug-auth")]
    [AllowAnonymous] // 👈 Crucial so it executes even if unauthorized
    public IActionResult DebugAuth()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        var user = HttpContext.User;

        var claims = user.Claims.Select(c => new { c.Type, c.Value }).ToList();

        return Ok(new
        {
            Timestamp = DateTime.UtcNow,
            HasAuthHeader = !string.IsNullOrEmpty(authHeader),
            RawAuthHeaderPreview = string.IsNullOrEmpty(authHeader) 
                ? "NONE" 
                : (authHeader.Length > 25 ? authHeader[..25] + "..." : authHeader),
            IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
            AuthenticationType = user.Identity?.AuthenticationType ?? "None",
            UserName = user.Identity?.Name ?? "Anonymous",
            ClaimsCount = claims.Count,
            Claims = claims
        });
    }
    // ---------------------------------------------------------
    // 1. READ ALL: GET api/team
    // ---------------------------------------------------------
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TeamDto>>> GetAllTeams()
    {
        try
        {            
            var teams = await _context.Teams
                .AsNoTracking()
                .Select(t => new TeamDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Record = t.Record,
                    Logo = t.Logo
                })
                .ToListAsync();

            return Ok(teams);
        }
        catch (Exception ex)
        {
            // 🚨 Returns the exact server-side exception to the response body
            return StatusCode(500, new 
            { 
                Error = ex.Message, 
                InnerError = ex.InnerException?.Message,
                Type = ex.GetType().Name
            });
        }
    }

    // ---------------------------------------------------------
    // 2. READ ONE: GET api/team/{id}
    // ---------------------------------------------------------
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TeamDto>> GetTeamById(Guid id)
    {
        var team = await _context.Teams
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TeamDto
            {
                Id = t.Id,
                Name = t.Name,
                Record = t.Record,
                Logo = t.Logo
            })
            .FirstOrDefaultAsync();

        if (team == null)
        {
            return NotFound(new { Message = $"Team with ID {id} was not found." });
        }

        return Ok(team);
    }

    // ---------------------------------------------------------
    // 3. CREATE: POST api/team
    // ---------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<TeamDto>> CreateTeam([FromBody] TeamDto newTeamDto)
    {
        if (newTeamDto == null || string.IsNullOrWhiteSpace(newTeamDto.Name))
        {
            return BadRequest(new { Message = "Invalid team data provided." });
        }

        var teamEntity = new Team
        {
            Id = newTeamDto.Id != Guid.Empty ? newTeamDto.Id : Guid.NewGuid(),
            Name = newTeamDto.Name,
            Record = string.IsNullOrWhiteSpace(newTeamDto.Record) ? "0-0" : newTeamDto.Record,
            Logo = newTeamDto.Logo
        };

        _context.Teams.Add(teamEntity);
        await _context.SaveChangesAsync();

        var createdDto = new TeamDto
        {
            Id = teamEntity.Id,
            Name = teamEntity.Name,
            Record = teamEntity.Record,
            Logo = teamEntity.Logo
        };

        // ⚡ Broadcast creation via SignalR
        await _teamHubContext.Clients.All.SendAsync("ReceiveTeamAdded", createdDto);

        return CreatedAtAction(nameof(GetTeamById), new { id = createdDto.Id }, createdDto);
    }

    // ---------------------------------------------------------
    // 4. UPDATE: PUT api/team/{id}
    // ---------------------------------------------------------
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTeam(Guid id, [FromBody] TeamDto updatedTeamDto)
    {
        if (updatedTeamDto == null)
        {
            return BadRequest(new { Message = "Updated team payload cannot be null." });
        }

        var existingTeam = await _context.Teams.FindAsync(id);
        if (existingTeam == null)
        {
            return NotFound(new { Message = $"Team with ID {id} was not found." });
        }

        // Apply field updates
        existingTeam.Name = updatedTeamDto.Name;
        existingTeam.Record = updatedTeamDto.Record;

        // Preserve existing logo if no new image binary was uploaded
        if (updatedTeamDto.Logo != null && updatedTeamDto.Logo.Length > 0)
        {
            existingTeam.Logo = updatedTeamDto.Logo;
        }

        await _context.SaveChangesAsync();

        var resultDto = new TeamDto
        {
            Id = existingTeam.Id,
            Name = existingTeam.Name,
            Record = existingTeam.Record,
            Logo = existingTeam.Logo
        };

        // ⚡ Broadcast update via SignalR
        await _teamHubContext.Clients.All.SendAsync("ReceiveTeamUpdated", resultDto);

        return Ok(resultDto);
    }

    // ---------------------------------------------------------
    // 5. DELETE: DELETE api/team/{id}
    // ---------------------------------------------------------
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTeam(Guid id)
    {
        var team = await _context.Teams.FindAsync(id);
        if (team == null)
        {
            return NotFound(new { Message = $"Team with ID {id} was not found." });
        }

        _context.Teams.Remove(team);
        await _context.SaveChangesAsync();

        // ⚡ Broadcast deletion via SignalR
        await _teamHubContext.Clients.All.SendAsync("ReceiveTeamDeleted", id);

        return NoContent();
    }
}