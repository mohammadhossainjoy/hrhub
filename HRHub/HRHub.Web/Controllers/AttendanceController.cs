using HRHub.Web.Data.Models;
using HRHub.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRHub.Web.Controllers
{
    /// <summary>
    /// Attendance:
    /// - My: first visit auto-links Identity user -> Employee by email, then renders status/actions
    /// - AdminReport: consolidated admin view (date or month)
    /// Uses EF Core 8 DateOnly/TimeOnly and DbSet Attendances.
    /// </summary>
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly HrHubContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AttendanceController(
            HrHubContext db,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<IdentityUser> signInManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }

        /// <summary>Ensure current Identity user is linked to an Employee by matching Email; also ensure "Employee" role and refresh cookie.</summary>
        private async Task<Employee?> EnsureLinkCurrentUserAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            // Already linked?
            var linked = await _db.Employees.FirstOrDefaultAsync(e => e.AspNetUserId == user.Id);
            if (linked != null)
            {
                await EnsureEmployeeRoleAsync(user);
                return linked;
            }

            // Match by email (case-insensitive)
            var email = await _userManager.GetEmailAsync(user);
            if (string.IsNullOrWhiteSpace(email)) return null;

            var candidate = await _db.Employees
                .FirstOrDefaultAsync(e => e.AspNetUserId == null &&
                                          e.Email != null &&
                                          e.Email.ToLower() == email.ToLower());
            if (candidate == null) return null;

            candidate.AspNetUserId = user.Id;
            await _db.SaveChangesAsync();

            await EnsureEmployeeRoleAsync(user);
            return candidate;
        }

        /// <summary>Create "Employee" role if missing, add to user if missing, then refresh sign-in so role claim is live.</summary>
        private async Task EnsureEmployeeRoleAsync(IdentityUser user)
        {
            if (!await _roleManager.RoleExistsAsync("Employee"))
                await _roleManager.CreateAsync(new IdentityRole("Employee"));

            if (!await _userManager.IsInRoleAsync(user, "Employee"))
                await _userManager.AddToRoleAsync(user, "Employee");

            await _signInManager.RefreshSignInAsync(user);
        }

        /// <summary>Resolve linked employee; if not linked yet, auto-link by email.</summary>
        private async Task<Employee?> GetOrLinkCurrentEmployeeAsync()
        {
            var uid = _userManager.GetUserId(User);
            if (!string.IsNullOrWhiteSpace(uid))
            {
                var e = await _db.Employees.AsNoTracking()
                           .FirstOrDefaultAsync(x => x.AspNetUserId == uid);
                if (e != null) return e;
            }
            return await EnsureLinkCurrentUserAsync();
        }

        /// <summary>
        /// Employee dashboard for today. NO role requirement here so that first visit can auto-link.
        /// </summary>
        [Authorize] // <-- allow first visit without role; CheckIn/Out are role-guarded
        [HttpGet]
        public async Task<IActionResult> My()
        {
            var emp = await GetOrLinkCurrentEmployeeAsync();
            if (emp == null)
            {
                // Auto-link failed: show guidance with current AspNetUserId
                return View("MyMissing", _userManager.GetUserId(User));
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var row = await _db.Attendances.AsNoTracking()
                .FirstOrDefaultAsync(a => a.EmployeeId == emp.EmployeeId && a.Date == today);

            var vm = new AttendanceMyVM
            {
                EmployeeId = emp.EmployeeId,
                EmployeeName = emp.FullName,
                Date = today,
                InTime = row?.InTime,
                OutTime = row?.OutTime,
                Message = row == null
                                ? "Not checked in yet."
                                : row.OutTime == null
                                    ? $"Checked in at {row.InTime}"
                                    : $"Checked out at {row.OutTime}"
            };
            return View(vm);
        }

        /// <summary>Record check-in for today (Employee-only).</summary>
        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn()
        {
            var emp = await GetOrLinkCurrentEmployeeAsync();
            if (emp == null) return Unauthorized();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var now = TimeOnly.FromDateTime(DateTime.Now);

            var existing = await _db.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == emp.EmployeeId && a.Date == today);

            if (existing != null && existing.InTime.HasValue)
            {
                TempData["Err"] = "Already checked in.";
                return RedirectToAction(nameof(My));
            }

            if (existing == null)
            {
                _db.Attendances.Add(new Attendance
                {
                    EmployeeId = emp.EmployeeId,
                    Date = today,
                    InTime = now
                });
            }
            else
            {
                existing.InTime = now;
                _db.Attendances.Update(existing);
            }

            await _db.SaveChangesAsync();
            TempData["Ok"] = "Checked in successfully.";
            return RedirectToAction(nameof(My));
        }

        /// <summary>Record check-out for today; validates OutTime > InTime (Employee-only).</summary>
        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut()
        {
            var emp = await GetOrLinkCurrentEmployeeAsync();
            if (emp == null) return Unauthorized();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var now = TimeOnly.FromDateTime(DateTime.Now);

            var row = await _db.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == emp.EmployeeId && a.Date == today);

            if (row == null || !row.InTime.HasValue)
            {
                TempData["Err"] = "Please check in first.";
                return RedirectToAction(nameof(My));
            }
            if (row.OutTime.HasValue)
            {
                TempData["Err"] = "Already checked out.";
                return RedirectToAction(nameof(My));
            }
            if (now <= row.InTime.Value)
            {
                TempData["Err"] = "Out time must be greater than in time.";
                return RedirectToAction(nameof(My));
            }

            row.OutTime = now;
            _db.Attendances.Update(row);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Checked out successfully.";
            return RedirectToAction(nameof(My));
        }

        /// <summary>Admin-only consolidated report with date or month filters.</summary>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminReport(DateOnly? date, int? year, int? month)
        {
            var q = _db.Attendances
                .Include(a => a.Employee)
                .AsNoTracking()
                .AsQueryable();

            if (date.HasValue)
                q = q.Where(a => a.Date == date.Value);
            else if (year.HasValue && month.HasValue)
                q = q.Where(a => a.Date.Year == year.Value && a.Date.Month == month.Value);

            var rows = await q
                .OrderBy(a => a.Date).ThenBy(a => a.Employee.FullName)
                .Select(a => new AttendanceRowVM
                {
                    Employee = a.Employee.FullName,
                    Date = a.Date,
                    InTime = a.InTime,
                    OutTime = a.OutTime
                })
                .ToListAsync();

            var vm = new AttendanceReportVM
            {
                Date = date,
                Year = year,
                Month = month,
                Rows = rows
            };
            return View(vm);
        }
    }
}
