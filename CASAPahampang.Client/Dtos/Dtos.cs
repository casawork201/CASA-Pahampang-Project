namespace CASAPahampang.Client.Dtos;

public class TeamDto
{    
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // 🎨 Nullable byte array fixes the CS8601 build warnings in TeamController
    public byte[]? Logo { get; set; } 
    
    public string Record { get; set; } = "0-0";
}
public class MatchDto
{    
    public Guid Id { get; set; }
    public int GameNumber { get; set; }
    public string Venue { get; set; } = "Main Gym";

    // 🏆 Identifier for the sport/game
    public string Sport { get; set; } = "Basketball";

    // ⚽ Include Team IDs and make nested TeamDtos nullable
    public Guid Team1Id { get; set; }
    public TeamDto? Team1 { get; set; }
    public int Team1Score { get; set; }

    public Guid Team2Id { get; set; }
    public TeamDto? Team2 { get; set; }
    public int Team2Score { get; set; }

    public bool IsLive { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string PeriodDetails { get; set; } = string.Empty;    
}
public class ChatMessageDto
{    
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    
    // 🕒 Default to UtcNow instead of default DateTime (0001-01-01)
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow; 
    
    public bool IsUser { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
}

public class AvatarOptionDto
{    
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}