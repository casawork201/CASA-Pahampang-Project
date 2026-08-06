using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using CASAPahampang.Data;
using CASAPahampang.Models;
using CASAPahampang.Client.Dtos;
using CASAPahampang.Hubs;

namespace CASAPahampang.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<MatchHub> _hubContext;

    public MatchesController(ApplicationDbContext context, IHubContext<MatchHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // 📋 GET: api/matches (All matches)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MatchDto>>> GetMatches()
    {
        var matches = await _context.Matches
            .Include(m => m.Team1)
            .Include(m => m.Team2)
            .Select(m => ToDto(m))
            .ToListAsync();

        return Ok(matches);
    }

    // 🏀 GET: api/matches/basketball
    [HttpGet("basketball")]
    public async Task<ActionResult<IEnumerable<MatchDto>>> GetBasketballMatches()
    {
        var matches = await _context.Matches
            .Include(m => m.Team1)
            .Include(m => m.Team2)
            .Where(m => m.Sport.ToLower() == "basketball")
            .Select(m => ToDto(m))
            .ToListAsync();

        return Ok(matches);
    }

    // 🏐 GET: api/matches/volleyball
    [HttpGet("volleyball")]
    public async Task<ActionResult<IEnumerable<MatchDto>>> GetVolleyballMatches()
    {
        var matches = await _context.Matches
            .Include(m => m.Team1)
            .Include(m => m.Team2)
            .Where(m => m.Sport.ToLower() == "volleyball")
            .Select(m => ToDto(m))
            .ToListAsync();

        return Ok(matches);
    }

    [HttpPost]
    public async Task<ActionResult<MatchDto>> CreateMatch([FromBody] MatchDto dto)
    {
        var match = new Match
        {
            Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
            GameNumber = dto.GameNumber,
            Venue = dto.Venue,
            Sport = string.IsNullOrWhiteSpace(dto.Sport) ? "Basketball" : dto.Sport,
            Team1Id = dto.Team1Id,
            Team1Score = dto.Team1Score,
            Team2Id = dto.Team2Id,
            Team2Score = dto.Team2Score,
            IsLive = dto.IsLive,
            StatusText = dto.StatusText,
            PeriodDetails = dto.PeriodDetails
        };

        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        await _context.Entry(match).Reference(m => m.Team1).LoadAsync();
        await _context.Entry(match).Reference(m => m.Team2).LoadAsync();

        var resultDto = ToDto(match);
        await _hubContext.Clients.All.SendAsync("ReceiveMatchAdded", resultDto);

        return CreatedAtAction(nameof(GetMatches), new { id = match.Id }, resultDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMatch(Guid id, [FromBody] MatchDto dto)
    {
        if (id != dto.Id) return BadRequest();

        var match = await _context.Matches.FindAsync(id);
        if (match == null) return NotFound();

        match.GameNumber = dto.GameNumber;
        match.Venue = dto.Venue;
        match.Sport = dto.Sport;
        match.Team1Id = dto.Team1Id;
        match.Team1Score = dto.Team1Score;
        match.Team2Id = dto.Team2Id;
        match.Team2Score = dto.Team2Score;
        match.IsLive = dto.IsLive;
        match.StatusText = dto.StatusText;
        match.PeriodDetails = dto.PeriodDetails;

        await _context.SaveChangesAsync();

        await _context.Entry(match).Reference(m => m.Team1).LoadAsync();
        await _context.Entry(match).Reference(m => m.Team2).LoadAsync();

        var resultDto = ToDto(match);
        await _hubContext.Clients.All.SendAsync("ReceiveMatchUpdated", resultDto);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMatch(Guid id)
    {
        var match = await _context.Matches.FindAsync(id);
        if (match == null) return NotFound();

        _context.Matches.Remove(match);
        await _context.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("ReceiveMatchDeleted", id);
        return NoContent();
    }

    private static MatchDto ToDto(Match m) => new MatchDto
    {
        Id = m.Id,
        GameNumber = m.GameNumber,
        Venue = m.Venue,
        Sport = m.Sport,
        Team1Id = m.Team1Id,
        Team1 = m.Team1 == null ? null : new TeamDto
        {
            Id = m.Team1.Id,
            Name = m.Team1.Name,
            Logo = m.Team1.Logo,
            Record = m.Team1.Record
        },
        Team1Score = m.Team1Score,
        Team2Id = m.Team2Id,
        Team2 = m.Team2 == null ? null : new TeamDto
        {
            Id = m.Team2.Id,
            Name = m.Team2.Name,
            Logo = m.Team2.Logo,
            Record = m.Team2.Record
        },
        Team2Score = m.Team2Score,
        IsLive = m.IsLive,
        StatusText = m.StatusText,
        PeriodDetails = m.PeriodDetails
    };
}