using HRHub.Web.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace HRHub.Web.Services
{
    /// <summary>
    /// Validates leave requests and returns working-day count (Fri/Sat excluded).
    /// Rules:
    /// - Start <= End
    /// - Backdated limit: Start >= Today - 7 days
    /// - Overlap with any Approved leave not allowed
    /// - Working-day count excludes Fri/Sat (Bangladesh weekend)
    /// - If attendance exists in range -> reject
    /// - Quota: used(Approved, same year, same type) + currentDays <= AnnualQuota
    /// </summary>
    public class LeaveService
    {
        private readonly HrHubContext _db;
        private static readonly HashSet<DayOfWeek> Weekend = new() { DayOfWeek.Friday, DayOfWeek.Saturday };

        public LeaveService(HrHubContext db) => _db = db;

        private static int CountWorkingDays(DateOnly start, DateOnly end)
        {
            int days = 0;
            for (var d = start; d <= end; d = d.AddDays(1))
            {
                if (Weekend.Contains(d.DayOfWeek)) continue; // Fri/Sat excluded
                days++;
            }
            return days;
        }

        public async Task<(bool ok, string? error, decimal days)> ValidateAsync(
            int employeeId, int leaveTypeId, DateOnly start, DateOnly end)
        {
            // 1) Range valid
            if (start > end)
                return (false, "Start date must be before or equal to end date.", 0);

            // 2) Backdated limit (<= 7 days)
            var sevenDaysAgo = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
            if (start < sevenDaysAgo)
                return (false, "Backdated requests beyond 7 days are not allowed.", 0);

            // 3) Overlap with any Approved leave
            bool overlap = await _db.Leaves.AnyAsync(l =>
                l.EmployeeId == employeeId &&
                l.Status == "Approved" &&
                l.StartDate <= end && l.EndDate >= start);
            if (overlap)
                return (false, "Overlaps with an approved leave.", 0);

            // 4) Working-days
            var workDays = CountWorkingDays(start, end);
            if (workDays <= 0)
                return (false, "No working days in the selected range.", 0);

            // 5) Attendance exists within range
            bool hasAttendance = await _db.Attendances.AnyAsync(a =>
                a.EmployeeId == employeeId &&
                a.Date >= start && a.Date <= end);
            if (hasAttendance)
                return (false, "Attendance exists in the selected range.", 0);

            // 6) Quota check
            var quota = await _db.LeaveTypes
                .Where(t => t.LeaveTypeId == leaveTypeId)
                .Select(t => t.AnnualQuota)
                .FirstAsync();

            var year = start.Year;
            var used = await _db.Leaves
                .Where(l => l.EmployeeId == employeeId
                            && l.LeaveTypeId == leaveTypeId
                            && l.Status == "Approved"
                            && l.StartDate.Year == year)
                .SumAsync(l => (decimal?)l.Days) ?? 0m;

            if (used + workDays > quota)
                return (false, $"Quota exceeded. Used {used}, request {workDays}, quota {quota}.", 0);

            return (true, null, workDays);
        }
    }
}
