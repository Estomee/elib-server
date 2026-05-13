// EF Core model for the USER_SESSIONS table storing JWT access and refresh tokens per device session.
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class UserSession
{
    public int SessionId { get; set; }

    public int UserId { get; set; }

    public string AccessToken { get; set; } = null!;

    public string RefreshToken { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public string DeviceInfo { get; set; } = null!;

    public string IpAddress { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
