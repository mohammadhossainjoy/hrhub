using HRHub.Web.Data.Models;
using HRHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRHub.Web.Controllers
{
    /// <summary>Admin-only: Create promotion + History</summary>
    [Authorize(Roles = "Admin")]
    public class PromotionsController : Controller
    {
        private readonly HrHubContext _db;
        private readonly PromotionService _service;

        public PromotionsController(HrHubContext db, PromotionService service)
        {
            _db = db;
            _service = service;
        }

        // GET: /Promotions/Create?employeeId=1
        [HttpGet]
        public async Task<IActionResult> Create(int employeeId)
        {
            var emp = await _db.Employees
                .Include(e => e.Designation)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
            if (emp == null) return NotFound();

            ViewData["NewDesignationId"] = new SelectList(
                await _db.Designations.AsNoTracking().ToListAsync(),
                "DesignationId", "Title", emp.DesignationId);

            var vm = new Promotion
            {
                EmployeeId = emp.EmployeeId,
                OldDesignationId = emp.DesignationId,
                EffectiveDate = DateOnly.FromDateTime(DateTime.Today)
            };
            ViewData["EmployeeName"] = emp.FullName;
            ViewData["OldDesignationTitle"] = (await _db.Designations.FindAsync(emp.DesignationId))?.Title ?? "";
            return View(vm);
        }

        // POST: /Promotions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeId,OldDesignationId,NewDesignationId,EffectiveDate,Notes")] Promotion vm)
        {
            var (ok, error, _) = await _service.ValidateAsync(vm.EmployeeId, vm.OldDesignationId, vm.NewDesignationId, vm.EffectiveDate);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, error ?? "Validation failed.");
                ViewData["NewDesignationId"] = new SelectList(
                    await _db.Designations.AsNoTracking().ToListAsync(),
                    "DesignationId", "Title", vm.NewDesignationId);

                var empHdr = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeId == vm.EmployeeId);
                ViewData["EmployeeName"] = empHdr?.FullName ?? "";
                ViewData["OldDesignationTitle"] = (await _db.Designations.FindAsync(vm.OldDesignationId))?.Title ?? "";
                return View(vm);
            }

            // Save promotion + update employee designation in a transaction
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Promotions.Add(vm);
                await _db.SaveChangesAsync();

                var emp = await _db.Employees.FirstAsync(e => e.EmployeeId == vm.EmployeeId);
                emp.DesignationId = vm.NewDesignationId;
                _db.Employees.Update(emp);
                await _db.SaveChangesAsync();

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            TempData["Ok"] = "Promotion saved and employee designation updated.";
            return RedirectToAction("Details", "Employees", new { id = vm.EmployeeId });
        }

        // GET: /Promotions/History/1  (id = EmployeeId)
        [HttpGet]
        public async Task<IActionResult> History(int id)
        {
            var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeId == id);
            if (emp == null) return NotFound();

            var rows = await _db.Promotions
                .Include(p => p.OldDesignation)
                .Include(p => p.NewDesignation)
                .Where(p => p.EmployeeId == id)
                .OrderByDescending(p => p.EffectiveDate)
                .AsNoTracking()
                .ToListAsync();

            ViewData["EmployeeName"] = emp.FullName;
            return View(rows);
        }
    }
}
