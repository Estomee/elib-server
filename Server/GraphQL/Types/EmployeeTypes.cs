// GraphQL DTO types for employee-related data: permissions, positions, work schedules, and employees.
namespace Server.GraphQL.Types;

public class PermissionDto
{
    public bool ToRead { get; set; }
    public bool ToWrite { get; set; }
    public bool ToDelete { get; set; }
    public bool ToUpdate { get; set; }
}

public class PositionDto
{
    public int PositionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public PermissionDto Permissions { get; set; } = new();
}

public class WorkScheduleDto
{
    public string ScheduleId { get; set; } = string.Empty;
    public int WorkDays { get; set; }
    public int Holidays { get; set; }
    public DateOnly ShiftStart { get; set; }
    public DateOnly ShiftEnd { get; set; }
}

public class EmployeeDto
{
    public int EmployeeId { get; set; }
    public UserDto User { get; set; } = new();
    public PositionDto Position { get; set; } = new();
    public DateOnly HireDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public string PassportNumber { get; set; } = string.Empty;
    public string PassportSeries { get; set; } = string.Empty;
    public WorkScheduleDto WorkSchedule { get; set; } = new();
}
