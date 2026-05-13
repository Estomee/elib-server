// EF Core model for the LEVEL_OF_LOGS table defining named log severity levels with display colors.
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class LevelOfLog
{
    public int LevelId { get; set; }

    public string LevelName { get; set; } = null!;

    public string Color { get; set; } = null!;

    public virtual ICollection<SystemLog> SystemLogs { get; set; } = new List<SystemLog>();
}
