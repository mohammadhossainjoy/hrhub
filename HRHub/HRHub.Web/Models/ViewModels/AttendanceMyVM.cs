namespace HRHub.Web.Models.ViewModels
{
    /// <summary>View model for the "My Attendance" page.</summary>
    public class AttendanceMyVM
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = "";
        public DateOnly Date { get; set; }
        public TimeOnly? InTime { get; set; }
        public TimeOnly? OutTime { get; set; }
        public bool HasRow => InTime.HasValue || OutTime.HasValue;
        public string? Message { get; set; }
    }
}
