using System;
using System.Collections.Generic;

namespace HRHub.Web.Data.Models;

public partial class Department
{
    public int DepartmentId { get; set; }

    public int CompanyId { get; set; }

    public string Name { get; set; } = null!;

    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
