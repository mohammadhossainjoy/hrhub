using System;
using System.Collections.Generic;

namespace HRHub.Web.Data.Models;

public partial class Leaf
{
    public int LeaveId { get; set; }

    public int EmployeeId { get; set; }

    public int LeaveTypeId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal Days { get; set; }

    public string Status { get; set; } = null!;

    public string? Reason { get; set; }

    public int? ApprovedByEmployeeId { get; set; }

    public virtual Employee? ApprovedByEmployee { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual LeaveType LeaveType { get; set; } = null!;
}
