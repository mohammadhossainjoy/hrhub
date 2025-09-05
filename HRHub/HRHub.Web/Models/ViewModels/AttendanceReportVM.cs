using System.Collections.Generic;

namespace HRHub.Web.Models.ViewModels
{
    /// <summary>Single row for the admin report.</summary>
    public class AttendanceRowVM
    {
        public string Employee { get; set; } = "";
        public DateOnly Date { get; set; }
        public TimeOnly? InTime { get; set; }
        public TimeOnly? OutTime { get; set; }
    }

    /// <summary>Filter + results for admin attendance report.</summary>
    public class AttendanceReportVM
    {
        public DateOnly? Date { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public List<AttendanceRowVM> Rows { get; set; } = new();
    }
}
