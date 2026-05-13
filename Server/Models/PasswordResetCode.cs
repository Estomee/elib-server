// EF Core model for the PASSWORD_RESET_CODES table storing one-time codes for password recovery.
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class PasswordResetCode
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Code { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public bool? Used { get; set; }

    public virtual User User { get; set; } = null!;
}
