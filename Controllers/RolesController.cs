//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;
//using SuperMarket.Models;
//using SuperMarket.Models.ViewModels;

//namespace SuperMarket.Controllers
//{
//    public class RolesController : Controller
//    {
//        private readonly DBContext _context;

//        public RolesController(DBContext context)
//        {
//            _context = context;
//        }
//        // GET: Roles/CreatePermission
//        [HttpGet]
//        public IActionResult CreatePermission()
//        {
//            // Predefined modules to prevent spelling conflicts and typos
//            var systemModules = new List<string> { "Users", "Inventory", "Sales", "Reports", "Purchases", "Customers" };
//            ViewBag.ModuleList = new SelectList(systemModules);

//            return View(new CreatePermissionViewModel());
//        }

//        // POST: Roles/CreatePermission
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> CreatePermission(CreatePermissionViewModel model)
//        {
//            if (ModelState.IsValid)
//            {
//                // Check if the PermissionCode already exists in the database
//                bool codeExists = await _context.Permissions
//                    .AnyAsync(p => p.PermissionCode == model.PermissionCode);

//                if (codeExists)
//                {
//                    ModelState.AddModelError("PermissionCode", "This permission code already exists.");

//                    // Repopulate dropdown if validation fails
//                    var systemModules = new List<string> { "Users", "Inventory", "Sales", "Reports", "Purchases", "Customers" };
//                    ViewBag.ModuleList = new SelectList(systemModules, model.ModuleName);
//                    return View(model);
//                }

//                // Map ViewModel to domain Permission model
//                var permission = new Permission
//                {
//                    PermissionCode = model.PermissionCode,
//                    ModuleName = model.ModuleName,
//                    Description = model.Description
//                };

//                _context.Permissions.Add(permission);
//                await _context.SaveChangesAsync();

//                TempData["SuccessMessage"] = "Permission created successfully!";
//                return RedirectToAction(nameof(Index));
//            }

//            // Repopulate dropdown if model state is invalid
//            var modules = new List<string> { "Users", "Inventory", "Sales", "Reports", "Purchases", "Customers" };
//            ViewBag.ModuleList = new SelectList(modules, model.ModuleName);

//            return View(model);
//        }
//        //old code

//        // GET: Roles/EditPermissions/5
//        public async Task<IActionResult> EditPermissions(int id)
//        {
//            var role = await _context.Roles
//                .Include(r => r.Permissions)
//                .FirstOrDefaultAsync(r => r.RoleId == id && !r.IsDeleted);

//            if (role == null)
//            {
//                return NotFound();
//            }

//            // Get all active permissions in the system
//            var allPermissions = await _context.Permissions.ToListAsync();

//            var model = new RolePermissionsViewModel
//            {
//                RoleId = role.RoleId,
//                RoleName = role.RoleName,
//                Permissions = allPermissions.Select(p => new PermissionSelectionViewModel
//                {
//                    PermissionId = p.PermissionId,
//                    // Adjust property names based on your actual Permission.cs model fields (e.g., PermissionName or Name)
//                    PermissionCode = p.PermissionCode,
//                    ModuleName = p.ModuleName,
//                    Description = p.Description,
//                    // Check if the role already has this permission assigned
//                    IsSelected = role.Permissions.Any(rp => rp.PermissionId == p.PermissionId)
//                }).ToList()
//            };

//            return View(model);
//        }

//        // POST: Roles/EditPermissions
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> EditPermissions(RolePermissionsViewModel model)
//        {
//            var role = await _context.Roles
//                .Include(r => r.Permissions)
//                .FirstOrDefaultAsync(r => r.RoleId == model.RoleId);

//            if (role == null)
//            {
//                return NotFound();
//            }

//            // Clear existing permissions
//            role.Permissions.Clear();

//            // Find the selected permission IDs from the form post
//            var selectedPermissionIds = model.Permissions
//                .Where(p => p.IsSelected)
//                .Select(p => p.PermissionId)
//                .ToList();

//            // Re-add selected permissions
//            var permissionsToAssign = await _context.Permissions
//                .Where(p => selectedPermissionIds.Contains(p.PermissionId))
//                .ToListAsync();

//            foreach (var permission in permissionsToAssign)
//            {
//                role.Permissions.Add(permission);
//            }

//            _context.Update(role);
//            await _context.SaveChangesAsync();

//            TempData["SuccessMessage"] = "Role permissions updated successfully!";
//            return RedirectToAction(nameof(Index));
//        }


//        // GET: Roles
//        public async Task<IActionResult> Index()
//        {
//            return View(await _context.Roles.ToListAsync());
//        }

//        // GET: Roles/Details/5
//        public async Task<IActionResult> Details(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var role = await _context.Roles
//                .FirstOrDefaultAsync(m => m.RoleId == id);
//            if (role == null)
//            {
//                return NotFound();
//            }

//            return View(role);
//        }

//        // GET: Roles/Create
//        public IActionResult Create()
//        {
//            return View();
//        }

//        // POST: Roles/Create
//        // To protect from overposting attacks, enable the specific properties you want to bind to.
//        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create([Bind("RoleId,RoleName,Description,CreatedAt,IsActive,IsDeleted,UpdatedAt")] Role role)
//        {
//            if (ModelState.IsValid)
//            {
//                role.CreatedAt = DateTime.Now;
//                role.IsActive = true;
//                role.IsDeleted = false;
//                _context.Add(role);
//                await _context.SaveChangesAsync();
//                return RedirectToAction(nameof(Index));
//            }
//            return View(role);
//        }

//        // GET: Roles/Edit/5
//        public async Task<IActionResult> Edit(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var role = await _context.Roles.FindAsync(id);
//            if (role == null)
//            {
//                return NotFound();
//            }
//            return View(role);
//        }

//        // POST: Roles/Edit/5
//        // To protect from overposting attacks, enable the specific properties you want to bind to.
//        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int id, [Bind("RoleId,RoleName,Description,CreatedAt,IsActive,IsDeleted,UpdatedAt")] Role role)
//        {
//            if (id != role.RoleId)
//            {
//                return NotFound();
//            }

//            if (ModelState.IsValid)
//            {
//                try
//                {
//                    _context.Update(role);
//                    await _context.SaveChangesAsync();
//                }
//                catch (DbUpdateConcurrencyException)
//                {
//                    if (!RoleExists(role.RoleId))
//                    {
//                        return NotFound();
//                    }
//                    else
//                    {
//                        throw;
//                    }
//                }
//                return RedirectToAction(nameof(Index));
//            }
//            return View(role);
//        }

//        // GET: Roles/Delete/5
//        public async Task<IActionResult> Delete(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var role = await _context.Roles
//                .FirstOrDefaultAsync(m => m.RoleId == id);
//            if (role == null)
//            {
//                return NotFound();
//            }

//            return View(role);
//        }

//        // POST: Roles/Delete/5
//        [HttpPost, ActionName("Delete")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(int id)
//        {
//            var role = await _context.Roles.FindAsync(id);
//            if (role != null)
//            {
//                _context.Roles.Remove(role);
//            }

//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        private bool RoleExists(int id)
//        {
//            return _context.Roles.Any(e => e.RoleId == id);
//        }
//    }
//}



using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperMarket.Models;

namespace SuperMarket.Controllers
{
    public class RolesController : Controller
    {
        private readonly DBContext _context;

        public RolesController(DBContext context)
        {
            _context = context;
        }

        // GET: Roles
        public async Task<IActionResult> Index()
        {
            return View(await _context.Roles.ToListAsync());
        }

        // GET: Roles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null)
                return NotFound();

            return View(role);
        }

        // GET: Roles/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Roles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("RoleName,Description")] Role role)
        {
            if (ModelState.IsValid)
            {
                role.CreatedAt = DateTime.Now;
                role.IsActive = true;
                role.IsDeleted = false;

                _context.Roles.Add(role);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(role);
        }

        // GET: Roles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var role = await _context.Roles.FindAsync(id);

            if (role == null)
                return NotFound();

            return View(role);
        }

        // POST: Roles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("RoleId,RoleName,Description,CreatedAt,IsActive,IsDeleted,UpdatedAt")]
            Role role)
        {
            if (id != role.RoleId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    role.UpdatedAt = DateTime.Now;

                    _context.Update(role);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoleExists(role.RoleId))
                        return NotFound();

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(role);
        }

        // GET: Roles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null)
                return NotFound();

            return View(role);
        }

        // POST: Roles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var role = await _context.Roles.FindAsync(id);

            if (role != null)
            {
                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RoleExists(int id)
        {
            return _context.Roles.Any(r => r.RoleId == id);
        }
    }
}
