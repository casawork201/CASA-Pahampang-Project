namespace CASAPahampang.Client.Dtos;


public class TeamDto
{    
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty!;
    public byte[] Logo { get; set; } = null!;
    public string Record { get; set; } = "0-0";
}
public class MatchDto
{    
    public Guid Id { get; set; }
    public int GameNumber { get; set; }
    public string Venue { get; set; } = "Main Gym";
    public TeamDto Team1 { get; set; } = new();
    public int Team1Score { get; set; }
    public TeamDto Team2 { get; set; } = new();
    public int Team2Score { get; set; }
    public bool IsLive { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string PeriodDetails { get; set; } = string.Empty;    
}

public class ChatMessageDto
{    
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty!;
    public string Message { get; set; } = string.Empty!;
    public DateTime TimeStamp { get; set; } = new();
    public bool IsUser { get; set; }
    public string AvatarUrl { get; set; } = string.Empty!;
}

public class AvatarOptionDto
{    
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty!;
    public string Url { get; set; } = string.Empty!;
}