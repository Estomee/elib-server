// EF Core model for the NOTIFICATIONS table storing email notifications sent to users via SendSay.
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public string Subject { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? Status { get; set; }

    public string? SentVia { get; set; }

    public DateTime SentAt { get; set; }

    public DateTime ReadAt { get; set; }

    public virtual User User { get; set; } = null!;
}
