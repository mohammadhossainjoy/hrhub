using System;
using System.Collections.Generic;

namespace HRHub.Web.Data.Models._promo_tmp;

public partial class Designation
{
    public int DesignationId { get; set; }

    public string Title { get; set; } = null!;

    public int? Grade { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<Promotion> PromotionNewDesignations { get; set; } = new List<Promotion>();

    public virtual ICollection<Promotion> PromotionOldDesignations { get; set; } = new List<Promotion>();
}
