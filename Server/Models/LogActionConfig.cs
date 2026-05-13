// EF Core model for the LOG_ACTION_CONFIGS table mapping action names to log severity levels.
namespace Server.Models;

public class LogActionConfig
{
    public string  ActionName  { get; set; } = string.Empty;
    public int     LevelId     { get; set; }
    public string? Description { get; set; }

    public virtual LevelOfLog Level { get; set; } = null!;
}
