using HRHub.Web.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace HRHub.Web.Services
{
    /// <summary>
    /// Validates promotion rules:
    /// - EffectiveDate >= JoinDate
    /// - EffectiveDate > last promotion date (if exists)
    /// - OldDesignationId must match current employee designation
    /// - NewDesignationId must be different
    /// </summary>
    public class PromotionService
    {
        private readonly HrHubContext _db;
        public PromotionService(HrHubContext db) => _db = db;

        public async Task<(bool ok, string? error, DateOnly? last)> ValidateAsync(
            int employeeId, int oldDesignationId, int newDesignationId, DateOnly effectiveDate)
        {
            var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
            if (emp == null) return (false, "Employee not found.", null);

            if (oldDesignationId != emp.DesignationId)
                return (false, "Old designation must match employee's current designation.", null);

            if (newDesignationId == oldDesignationId)
                return (false, "New designation cannot be the same as old designation.", null);

            // JoinDate is DateOnly in your model
            var join = emp.JoinDate;
            if (effectiveDate < join)
                return (false, "Effective date cannot be earlier than Join Date.", null);

            // Last promotion date (if any)
            var last = await _db.Promotions
                .Where(p => p.EmployeeId == employeeId)
                .OrderByDescending(p => p.EffectiveDate)
                .Select(p => (DateOnly?)p.EffectiveDate)
                .FirstOrDefaultAsync();

            if (last.HasValue && effectiveDate <= last.Value)
                return (false, $"Effective date must be later than last promotion date ({last.Value:yyyy-MM-dd}).", last);

            return (true, null, last);
        }
    }
}
