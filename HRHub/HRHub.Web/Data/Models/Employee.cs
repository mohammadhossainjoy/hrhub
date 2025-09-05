using System;
using System.Collections.Generic;

namespace HRHub.Web.Data.Models;

public partial class Employee
{
    public int EmployeeId { get; set; }

    public string EmpNo { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateOnly JoinDate { get; set; }

    public int CompanyId { get; set; }

    public int DepartmentId { get; set; }

    public int DesignationId { get; set; }

    public bool IsActive { get; set; }

    public string? AspNetUserId { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Company Company { get; set; } = null!;

    public virtual Department Department { get; set; } = null!;

    public virtual Designation Designation { get; set; } = null!;

    public virtual ICollection<Leaf> LeafApprovedByEmployees { get; set; } = new List<Leaf>();

    public virtual ICollection<Leaf> LeafEmployees { get; set; } = new List<Leaf>();
}
