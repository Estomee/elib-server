// GraphQL DTO types for system data: admin stats, log levels, system logs, notifications, and auth payload.
namespace Server.GraphQL.Types;

public class AdminStatsDto
{
    public int BooksCount    { get; set; }
    public int UsersCount    { get; set; }
    public int ReadingNow    { get; set; }
    public int UploadedToday { get; set; }
}

public class LevelOfLogDto
{
    public string LevelId { get; set; } = string.Empty;
    public string LevelName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class SystemLogDto
{
    public string LogId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public LevelOfLogDto Level { get; set; } = new();
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
}

public class NotificationDto
{
    public string NotificationId { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SentVia { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public DateTime ReadAt { get; set; }
}

public class AuthPayload
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
}
