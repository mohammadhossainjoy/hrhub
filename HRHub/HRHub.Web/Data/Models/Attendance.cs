using System;
using System.Collections.Generic;

namespace HRHub.Web.Data.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly? InTime { get; set; }

    public TimeOnly? OutTime { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
