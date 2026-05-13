// EF Core model for the SYSTEM_LOGS table recording user actions with timestamps, levels, and encrypted IPs.
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class SystemLog
{
    public long LogId { get; set; }

    public DateTime? Timestamp { get; set; }

    public int LevelId { get; set; }

    public int? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string? IpAddress { get; set; }

    public virtual LevelOfLog Level { get; set; } = null!;

    public virtual User? User { get; set; }
}
