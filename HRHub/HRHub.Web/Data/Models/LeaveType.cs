using System;
using System.Collections.Generic;

namespace HRHub.Web.Data.Models;

public partial class LeaveType
{
    public int LeaveTypeId { get; set; }

    public string Name { get; set; } = null!;

    public decimal AnnualQuota { get; set; }

    public virtual ICollection<Leaf> Leaves { get; set; } = new List<Leaf>();
}
