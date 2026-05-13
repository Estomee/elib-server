// EF Core model for the EMPLOYEES table representing library staff with position and schedule data.
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class Employee
{
    public int EmployeeId { get; set; }

    public int UserId { get; set; }

    public int PositionId { get; set; }

    public DateOnly HireDate { get; set; }

    public DateOnly? TerminationDate { get; set; }

    public string PassportNumber { get; set; } = null!;

    public string PassportSeries { get; set; } = null!;

    public string WorkSchedule { get; set; } = null!;

    public virtual Position Position { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
