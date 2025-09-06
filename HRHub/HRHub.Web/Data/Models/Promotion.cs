namespace HRHub.Web.Data.Models
{
    /// <summary>Promotion record: from old to new designation on a given effective date.</summary>
    public partial class Promotion
    {
        public int PromotionId { get; set; }
        public int EmployeeId { get; set; }
        public int OldDesignationId { get; set; }
        public int NewDesignationId { get; set; }
        public DateOnly EffectiveDate { get; set; }
        public string? Notes { get; set; }

        public virtual Employee Employee { get; set; } = null!;
        public virtual Designation OldDesignation { get; set; } = null!;
        public virtual Designation NewDesignation { get; set; } = null!;
    }
}
