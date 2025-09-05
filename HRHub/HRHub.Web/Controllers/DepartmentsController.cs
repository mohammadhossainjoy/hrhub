using HRHub.Web.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace HRHub.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartmentsController : Controller
    {
        private readonly HrHubContext _context;
        public DepartmentsController(HrHubContext context) => _context = context;

        // GET: Departments
        public async Task<IActionResult> Index()
        {
            var data = await _context.Departments
                                     .Include(d => d.Company)
                                     .AsNoTracking()
                                     .ToListAsync();
            return View(data);
        }

        // GET: Departments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments
                                           .Include(d => d.Company)
                                           .AsNoTracking()
                                           .FirstOrDefaultAsync(m => m.DepartmentId == id);
            if (department == null) return NotFound();

            return View(department);
        }

        // GET: Departments/Create
        public IActionResult Create()
        {
            ViewData["CompanyId"] = new SelectList(
                _context.Companies.AsNoTracking(), "CompanyId", "Name"
            );
            return View();
        }

        // POST: Departments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DepartmentId,CompanyId,Name")] Department department)
        {
            // nav-prop validation noise
            ModelState.Remove(nameof(Department.Company));
            ModelState.Remove("Company");

            // FK exists check
            if (!await _context.Companies.AnyAsync(c => c.CompanyId == department.CompanyId))
                ModelState.AddModelError("CompanyId", "Please select a valid company.");

            System.Diagnostics.Debug.WriteLine($"[POST-Create] CompanyId={department.CompanyId}, Name='{department.Name}'");

            if (!ModelState.IsValid)
            {
                foreach (var e in ModelState.SelectMany(kv => kv.Value.Errors))
                    System.Diagnostics.Debug.WriteLine("[ModelState] " + e.ErrorMessage);

                ViewData["CompanyId"] = new SelectList(
                    _context.Companies.AsNoTracking(), "CompanyId", "Name", department.CompanyId
                );
                return View(department);
            }

            _context.Add(department);
            await _context.SaveChangesAsync();   // INSERT
            return RedirectToAction(nameof(Index));
        }

        // GET: Departments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments.FindAsync(id);
            if (department == null) return NotFound();

            ViewData["CompanyId"] = new SelectList(
                _context.Companies.AsNoTracking(), "CompanyId", "Name", department.CompanyId
            );
            return View(department);
        }

        // POST: Departments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DepartmentId,CompanyId,Name")] Department department)
        {
            if (id != department.DepartmentId) return NotFound();

            // nav-prop validation noise 
            ModelState.Remove(nameof(Department.Company));
            ModelState.Remove("Company");

            // FK exists
            if (!await _context.Companies.AnyAsync(c => c.CompanyId == department.CompanyId))
                ModelState.AddModelError("CompanyId", "Please select a valid company.");

            System.Diagnostics.Debug.WriteLine($"[POST-Edit] Id={id}, CompanyId={department.CompanyId}, Name='{department.Name}'");

            if (!ModelState.IsValid)
            {
                foreach (var e in ModelState.SelectMany(kv => kv.Value.Errors))
                    System.Diagnostics.Debug.WriteLine("[ModelState] " + e.ErrorMessage);

                ViewData["CompanyId"] = new SelectList(
                    _context.Companies.AsNoTracking(), "CompanyId", "Name", department.CompanyId
                );
                return View(department);
            }

            try
            {
                _context.Update(department);      // UPDATE
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context.Departments.AnyAsync(e => e.DepartmentId == id);
                if (!exists) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Departments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments
                                           .Include(d => d.Company)
                                           .AsNoTracking()
                                           .FirstOrDefaultAsync(m => m.DepartmentId == id);
            if (department == null) return NotFound();

            return View(department);
        }

        // POST: Departments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department != null)
            {
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
