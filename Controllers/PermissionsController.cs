//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;
//using SuperMarket.Models;

//namespace SuperMarket.Controllers
//{
//    public class PermissionsController : Controller
//    {
//        private readonly DBContext _context;

//        public PermissionsController(DBContext context)
//        {
//            _context = context;
//        }

//        // GET: Permissions
//        public async Task<IActionResult> Index()
//        {
//            return View(await _context.Permissions.ToListAsync());
//        }

//        // GET: Permissions/Details/5
//        public async Task<IActionResult> Details(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var permission = await _context.Permissions
//                .FirstOrDefaultAsync(m => m.PermissionId == id);
//            if (permission == null)
//            {
//                return NotFound();
//            }

//            return View(permission);
//        }

//        // GET: Permissions/Create
//        public IActionResult Create()
//        {
//            return View();
//        }

//        // POST: Permissions/Create
//        // To protect from overposting attacks, enable the specific properties you want to bind to.
//        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create([Bind("PermissionId,PermissionCode,ModuleName,Description")] Permission permission)
//        {
//            if (ModelState.IsValid)
//            {
//                _context.Add(permission);
//                await _context.SaveChangesAsync();
//                return RedirectToAction(nameof(Index));
//            }
//            return View(permission);
//        }

//        // GET: Permissions/Edit/5
//        public async Task<IActionResult> Edit(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var permission = await _context.Permissions.FindAsync(id);
//            if (permission == null)
//            {
//                return NotFound();
//            }
//            return View(permission);
//        }

//        // POST: Permissions/Edit/5
//        // To protect from overposting attacks, enable the specific properties you want to bind to.
//        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int id, [Bind("PermissionId,PermissionCode,ModuleName,Description")] Permission permission)
//        {
//            if (id != permission.PermissionId)
//            {
//                return NotFound();
//            }

//            if (ModelState.IsValid)
//            {
//                try
//                {
//                    _context.Update(permission);
//                    await _context.SaveChangesAsync();
//                }
//                catch (DbUpdateConcurrencyException)
//                {
//                    if (!PermissionExists(permission.PermissionId))
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
//            return View(permission);
//        }

//        // GET: Permissions/Delete/5
//        public async Task<IActionResult> Delete(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var permission = await _context.Permissions
//                .FirstOrDefaultAsync(m => m.PermissionId == id);
//            if (permission == null)
//            {
//                return NotFound();
//            }

//            return View(permission);
//        }

//        // POST: Permissions/Delete/5
//        [HttpPost, ActionName("Delete")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(int id)
//        {
//            var permission = await _context.Permissions.FindAsync(id);
//            if (permission != null)
//            {
//                _context.Permissions.Remove(permission);
//            }

//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        private bool PermissionExists(int id)
//        {
//            return _context.Permissions.Any(e => e.PermissionId == id);
//        }
//    }
//}


using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SuperMarket.Models;
using SuperMarket.Models.ViewModels;

namespace SuperMarket.Controllers
{
    public class PermissionsController : Controller
    {
        private readonly DBContext _context;

        public PermissionsController(DBContext context)
        {
            _context = context;
        }

        // GET: Permissions/CreatePermission
        [HttpGet]
        public IActionResult Create()
        {
            var systemModules = new List<string>
            {
                "Users",
                "Inventory",
                "Sales",
                "Reports",
                "Purchases",
                "Customers"
            };

            ViewBag.ModuleList = new SelectList(systemModules);

            return View(new CreatePermissionViewModel());
        }

        // POST: Permissions/CreatePermission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePermission(
            CreatePermissionViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool codeExists = await _context.Permissions
                    .AnyAsync(p =>
                        p.PermissionCode == model.PermissionCode);

                if (codeExists)
                {
                    ModelState.AddModelError(
                        "PermissionCode",
                        "This permission code already exists.");

                    ViewBag.ModuleList = new SelectList(
                        GetSystemModules(), model.ModuleName);

                    return View(model);
                }

                var permission = new Permission
                {
                    PermissionCode = model.PermissionCode,
                    ModuleName = model.ModuleName,
                    Description = model.Description
                };

                _context.Permissions.Add(permission);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Permission created successfully!";

                return RedirectToAction(nameof(CreatePermission));
            }

            ViewBag.ModuleList = new SelectList(
                GetSystemModules(), model.ModuleName);

            return View(model);
        }

        // GET: Permissions/EditPermissions/5
        public async Task<IActionResult> EditPermissions(int id)
        {
            var role = await _context.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r =>
                    r.RoleId == id && !r.IsDeleted);

            if (role == null)
                return NotFound();

            var allPermissions = await _context.Permissions
                .ToListAsync();

            var model = new RolePermissionsViewModel
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,

                Permissions = allPermissions.Select(p =>
                    new PermissionSelectionViewModel
                    {
                        PermissionId = p.PermissionId,
                        PermissionCode = p.PermissionCode,
                        ModuleName = p.ModuleName,
                        Description = p.Description,

                        IsSelected = role.Permissions.Any(rp =>
                            rp.PermissionId == p.PermissionId)
                    }).ToList()
            };

            return View(model);
        }

        // POST: Permissions/EditPermissions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPermissions(
            RolePermissionsViewModel model)
        {
            var role = await _context.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r =>
                    r.RoleId == model.RoleId && !r.IsDeleted);

            if (role == null)
                return NotFound();

            var selectedPermissionIds = (model.Permissions ?? new())
                .Where(p => p.IsSelected)
                .Select(p => p.PermissionId)
                .Distinct()
                .ToList();

            var permissionsToAssign = await _context.Permissions
                .Where(p =>
                    selectedPermissionIds.Contains(p.PermissionId))
                .ToListAsync();

            // Replace the role's existing permissions.
            role.Permissions.Clear();

            foreach (var permission in permissionsToAssign)
            {
                role.Permissions.Add(permission);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Role permissions updated successfully!";

            return RedirectToAction(
                "EditPermissions",
                new { id = role.RoleId });
        }

        private static List<string> GetSystemModules()
        {
            return new List<string>
            {
                "Users",
                "Inventory",
                "Sales",
                "Reports",
                "Purchases",
                "Customers"
            };
        }
    }
}
