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
    public class EmployeesController : Controller
    {
        private readonly HrHubContext _context;
        public EmployeesController(HrHubContext context) => _context = context;

        // GET: Employees
        public async Task<IActionResult> Index()
        {
            var list = await _context.Employees
                .Include(e => e.Company)
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .AsNoTracking()
                .ToListAsync();

            return View(list);
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Company)
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.EmployeeId == id);

            if (employee == null) return NotFound();

            return View(employee);
        }

        // GET: Employees/Create
        public IActionResult Create()
        {
            BindDropdowns();
            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeId,EmpNo,FullName,Email,JoinDate,CompanyId,DepartmentId,DesignationId,IsActive,AspNetUserId")] Employee employee)
        {
            // nav props remove (noise এড়াতে)
            ModelState.Remove(nameof(Employee.Company));
            ModelState.Remove(nameof(Employee.Department));
            ModelState.Remove(nameof(Employee.Designation));

            // FK exists
            if (!await _context.Companies.AnyAsync(c => c.CompanyId == employee.CompanyId))
                ModelState.AddModelError("CompanyId", "Select a valid company.");
            if (!await _context.Departments.AnyAsync(d => d.DepartmentId == employee.DepartmentId))
                ModelState.AddModelError("DepartmentId", "Select a valid department.");
            if (!await _context.Designations.AnyAsync(x => x.DesignationId == employee.DesignationId))
                ModelState.AddModelError("DesignationId", "Select a valid designation.");

            // Unique checks
            if (await _context.Employees.AnyAsync(e => e.EmpNo == employee.EmpNo))
                ModelState.AddModelError("EmpNo", "Employee No already exists.");
            if (await _context.Employees.AnyAsync(e => e.Email == employee.Email))
                ModelState.AddModelError("Email", "Email already exists.");

            if (!ModelState.IsValid)
            {
                BindDropdowns(employee);
                return View(employee);
            }

            _context.Add(employee);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            BindDropdowns(employee);
            return View(employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EmployeeId,EmpNo,FullName,Email,JoinDate,CompanyId,DepartmentId,DesignationId,IsActive,AspNetUserId")] Employee employee)
        {
            if (id != employee.EmployeeId) return NotFound();

            // nav props remove
            ModelState.Remove(nameof(Employee.Company));
            ModelState.Remove(nameof(Employee.Department));
            ModelState.Remove(nameof(Employee.Designation));

            // FK exists
            if (!await _context.Companies.AnyAsync(c => c.CompanyId == employee.CompanyId))
                ModelState.AddModelError("CompanyId", "Select a valid company.");
            if (!await _context.Departments.AnyAsync(d => d.DepartmentId == employee.DepartmentId))
                ModelState.AddModelError("DepartmentId", "Select a valid department.");
            if (!await _context.Designations.AnyAsync(x => x.DesignationId == employee.DesignationId))
                ModelState.AddModelError("DesignationId", "Select a valid designation.");

            // Unique checks (excluding current record)
            if (await _context.Employees.AnyAsync(e => e.EmpNo == employee.EmpNo && e.EmployeeId != id))
                ModelState.AddModelError("EmpNo", "Employee No already exists.");
            if (await _context.Employees.AnyAsync(e => e.Email == employee.Email && e.EmployeeId != id))
                ModelState.AddModelError("Email", "Email already exists.");

            if (!ModelState.IsValid)
            {
                BindDropdowns(employee);
                return View(employee);
            }

            try
            {
                _context.Update(employee);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context.Employees.AnyAsync(e => e.EmployeeId == id);
                if (!exists) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Company)
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.EmployeeId == id);

            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private void BindDropdowns(Employee e = null)
        {
            ViewData["CompanyId"] = new SelectList(_context.Companies.AsNoTracking(), "CompanyId", "Name", e?.CompanyId);
            ViewData["DepartmentId"] = new SelectList(_context.Departments.AsNoTracking(), "DepartmentId", "Name", e?.DepartmentId);
            ViewData["DesignationId"] = new SelectList(_context.Designations.AsNoTracking(), "DesignationId", "Title", e?.DesignationId);
        }

        private bool EmployeeExists(int id) => _context.Employees.Any(e => e.EmployeeId == id);
    }
}
