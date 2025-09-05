using System;
using System.Collections.Generic;

namespace HRHub.Web.Data.Models;

public partial class Company
{
    public int CompanyId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
