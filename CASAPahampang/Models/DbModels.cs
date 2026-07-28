using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CASAPahampang.Models;

[Table("Team")]
public class Team
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    // 🎨 Nullable byte array for PostgreSQL bytea mapping
    public byte[]? Logo { get; set; } 
    
    public string Record { get; set; } = "0-0";
}

[Table("Match")]
public class Match
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public int GameNumber { get; set; }
    public string Venue { get; set; } = "Main Gym";

    // ⚽ Explicit Foreign Key & Navigation for Team 1
    public Guid Team1Id { get; set; }
    [ForeignKey(nameof(Team1Id))]
    public Team? Team1 { get; set; }

    public int Team1Score { get; set; }

    // ⚽ Explicit Foreign Key & Navigation for Team 2
    public Guid Team2Id { get; set; }
    [ForeignKey(nameof(Team2Id))]
    public Team? Team2 { get; set; }

    public int Team2Score { get; set; }

    public bool IsLive { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string PeriodDetails { get; set; } = string.Empty;    
}

[Table("ChatMessage")]
public class ChatMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string Message { get; set; } = string.Empty;
    
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    public bool IsUser { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
}

[Table("AvatarOption")]
public class AvatarOption
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Url { get; set; } = string.Empty;
}