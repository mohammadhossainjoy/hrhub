using HRHub.Web.Data.Models;
using HRHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRHub.Web.Controllers
{
    /// <summary>
    /// Employee: My (list) + Apply
    /// Admin: Pending (approve/reject)
    /// </summary>
    [Authorize]
    public class LeavesController : Controller
    {
        private readonly HrHubContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly LeaveService _leaveService;

        public LeavesController(HrHubContext db, UserManager<IdentityUser> userManager, LeaveService leaveService)
        {
            _db = db;
            _userManager = userManager;
            _leaveService = leaveService;
        }

        // ---- Helpers ----
        private async Task<Employee?> GetCurrentEmployeeAsync()
        {
            var uid = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(uid)) return null;
            return await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.AspNetUserId == uid);
        }

        private void BindLeaveTypes(int? sel = null)
        {
            ViewData["LeaveTypeId"] = new SelectList(_db.LeaveTypes.AsNoTracking(), "LeaveTypeId", "Name", sel);
        }

        // ---- Employee: My + Apply ----

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> My()
        {
            var emp = await GetCurrentEmployeeAsync();
            if (emp == null) return Unauthorized();

            var rows = await _db.Leaves
                .Include(l => l.LeaveType)
                .Where(l => l.EmployeeId == emp.EmployeeId)
                .OrderByDescending(l => l.StartDate)
                .AsNoTracking()
                .ToListAsync();

            return View(rows);
        }

        [Authorize(Roles = "Employee")]
        public IActionResult Apply()
        {
            BindLeaveTypes();
            return View(new Leaf
            {
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today)
            });
        }

        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply([Bind("LeaveTypeId,StartDate,EndDate,Reason")] Leaf vm)
        {
            var emp = await GetCurrentEmployeeAsync();
            if (emp == null) return Unauthorized();

            // Validate and compute working-day count
            var (ok, error, days) = await _leaveService.ValidateAsync(emp.EmployeeId, vm.LeaveTypeId, vm.StartDate, vm.EndDate);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, error ?? "Validation failed.");
                BindLeaveTypes(vm.LeaveTypeId);
                return View(vm);
            }

            var entity = new Leaf
            {
                EmployeeId = emp.EmployeeId,
                LeaveTypeId = vm.LeaveTypeId,
                StartDate = vm.StartDate,
                EndDate = vm.EndDate,
                Days = days,
                Status = "Pending",
                Reason = vm.Reason
            };
            _db.Leaves.Add(entity);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Leave request submitted.";
            return RedirectToAction(nameof(My));
        }

        // ---- Admin: Pending + Actions ----

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Pending()
        {
            var pending = await _db.Leaves
                .Include(l => l.Employee)
                .Include(l => l.LeaveType)
                .Where(l => l.Status == "Pending")
                .OrderBy(l => l.StartDate)
                .AsNoTracking().ToListAsync();

            return View(pending);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var row = await _db.Leaves.FindAsync(id);
            if (row == null) return NotFound();

            // Re-validate at approval time
            var (ok, error, _) = await _leaveService.ValidateAsync(row.EmployeeId, row.LeaveTypeId, row.StartDate, row.EndDate);
            if (!ok)
            {
                TempData["Err"] = error ?? "Validation failed.";
                return RedirectToAction(nameof(Pending));
            }

            row.Status = "Approved";
            var approver = await GetCurrentEmployeeAsync();
            if (approver != null) row.ApprovedByEmployeeId = approver.EmployeeId;

            _db.Leaves.Update(row);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Leave approved.";
            return RedirectToAction(nameof(Pending));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var row = await _db.Leaves.FindAsync(id);
            if (row == null) return NotFound();

            row.Status = "Rejected";
            _db.Leaves.Update(row);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Leave rejected.";
            return RedirectToAction(nameof(Pending));
        }
    }
}
