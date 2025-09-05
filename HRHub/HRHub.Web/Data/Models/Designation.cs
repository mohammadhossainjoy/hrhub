using System;
using System.Collections.Generic;

namespace HRHub.Web.Data.Models;

public partial class Designation
{
    public int DesignationId { get; set; }

    public string Title { get; set; } = null!;

    public int? Grade { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
