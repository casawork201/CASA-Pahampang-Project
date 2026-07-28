using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CASAPahampang. Models;

[Table("Team")]
public class Team
{
    [Key]
    public Guid Name { get; set; } = Guid.NewGuid();
    public byte[] Logo { get; set; } = null!;
    public string Record { get; set; } = "0-0";
}
[Table("Match")]
public class Match
{
    public int GameNumber { get; set; }
    public string Venue { get; set; } = "Main Gym";
    public Team Team1 { get; set; } = new();
    public int Team1Score { get; set; }
    public Team Team2 { get; set; } = new();
    public int Team2Score { get; set; }
    public bool IsLive { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string PeriodDetails { get; set; } = string.Empty;    
}

[Table("ChatMessage")]
public class ChatMessage
{
    public string Username { get; set; } = string.Empty!;
    public string Message { get; set; } = string.Empty!;
    public DateTime TimeStamp { get; set; } = new();
    public bool IsUser { get; set; }
    public string AvatarUrl { get; set; } = string.Empty!;
}
[Table("AvatarOption")]
public class AvatarOption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty!;
    public string Url { get; set; } = string.Empty!;
}

