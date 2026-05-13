// EF Core model for the POSITIONS table storing employee job titles, salaries, and JSON permissions.
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class Position
{
    public int PositionId { get; set; }

    public string Title { get; set; } = null!;

    public decimal Salary { get; set; }

    public string Permissions { get; set; } = null!;

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
