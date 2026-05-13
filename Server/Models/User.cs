// EF Core model for the USERS table with personal data, credentials, and navigation properties.
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string Phone { get; set; } = null!;

    public int YearOfBirth { get; set; }

    public bool Employee { get; set; }

    public DateTime RegistrationDate { get; set; }

    public DateTime LastLogin { get; set; }

    public DateTime LastPasswordChange { get; set; }

    public virtual Employee? EmployeeNavigation { get; set; }

    public virtual ICollection<MediaFile> MediaFiles { get; set; } = new List<MediaFile>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<PasswordResetCode> PasswordResetCodes { get; set; } = new List<PasswordResetCode>();

    public virtual ICollection<SystemLog> SystemLogs { get; set; } = new List<SystemLog>();

    public virtual ICollection<UserBook> UserBooks { get; set; } = new List<UserBook>();

    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
}
