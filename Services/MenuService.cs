using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperMarket.Models;
using SuperMarket.Models.ViewModels;

namespace SuperMarket.Services
{
    public class MenuService : IMenuService
    {
        private readonly DBContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _jsonFilePath;
        private readonly string _roleMenuFilePath;
        private static readonly SemaphoreSlim _fileLock = new(1, 1);

        public MenuService(DBContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
            string appDataDir = Path.Combine(_env.ContentRootPath, "App_Data");
            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }
            _jsonFilePath = Path.Combine(appDataDir, "menus.json");
            _roleMenuFilePath = Path.Combine(appDataDir, "role_menus.json");
        }

        private async Task<bool> CanUseDatabaseAsync()
        {
            try
            {
                if (_context.Database.CanConnect())
                {
                    // Check if Menus table exists
                    _context.Menus.Take(1).ToList();
                    return true;
                }
            }
            catch
            {
                // Database unreachable or table not scaffolded yet
            }
            return false;
        }

        public async Task<List<Menu>> GetAllAsync(string? category = null, string? search = null, bool? isMenuOnly = null)
        {
            List<Menu> list;

            if (await CanUseDatabaseAsync())
            {
                try
                {
                    list = await _context.Menus
                        .OrderBy(m => m.MenuCategory)
                        .ThenBy(m => m.SortOrder)
                        .ThenBy(m => m.Title)
                        .ToListAsync();

                    if (!list.Any())
                    {
                        var seed = GetDefaultSeedMenus();
                        _context.Menus.AddRange(seed);
                        await _context.SaveChangesAsync();
                        list = seed;
                    }
                }
                catch
                {
                    list = await LoadFromJsonAsync();
                }
            }
            else
            {
                list = await LoadFromJsonAsync();
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                list = list.Where(m => string.Equals(m.MenuCategory, category, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                list = list.Where(m =>
                    (m.Title != null && m.Title.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (m.Controller != null && m.Controller.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (m.Action != null && m.Action.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (m.MenuCategory != null && m.MenuCategory.Contains(s, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            if (isMenuOnly.HasValue && isMenuOnly.Value)
            {
                list = list.Where(m => m.IsMenu).ToList();
            }

            return list;
        }

        public async Task<Menu?> GetByIdAsync(int id)
        {
            if (await CanUseDatabaseAsync())
            {
                try
                {
                    return await _context.Menus.FindAsync(id);
                }
                catch
                {
                    // fallback to json
                }
            }

            var list = await LoadFromJsonAsync();
            return list.FirstOrDefault(m => m.MenuId == id);
        }

        public async Task<Menu> CreateAsync(Menu menu)
        {
            menu.CreatedAt = DateTime.UtcNow;

            if (await CanUseDatabaseAsync())
            {
                try
                {
                    _context.Menus.Add(menu);
                    await _context.SaveChangesAsync();
                    await SyncToJsonAsync();
                    return menu;
                }
                catch
                {
                    // fallback to json
                }
            }

            var list = await LoadFromJsonAsync();
            int nextId = list.Any() ? list.Max(m => m.MenuId) + 1 : 1;
            menu.MenuId = nextId;
            list.Add(menu);
            await SaveToJsonAsync(list);
            return menu;
        }

        public async Task<bool> UpdateAsync(Menu menu)
        {
            menu.UpdatedAt = DateTime.UtcNow;

            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var existing = await _context.Menus.FindAsync(menu.MenuId);
                    if (existing != null)
                    {
                        existing.Title = menu.Title;
                        existing.Controller = menu.Controller;
                        existing.Action = menu.Action;
                        existing.IsMenu = menu.IsMenu;
                        existing.IsTraceable = menu.IsTraceable;
                        existing.IsActive = menu.IsActive;
                        existing.MenuCategory = menu.MenuCategory;
                        existing.SortOrder = menu.SortOrder;
                        existing.Icon = menu.Icon;
                        existing.Description = menu.Description;
                        existing.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        await SyncToJsonAsync();
                        return true;
                    }
                }
                catch
                {
                    // fallback to json
                }
            }

            var list = await LoadFromJsonAsync();
            var index = list.FindIndex(m => m.MenuId == menu.MenuId);
            if (index >= 0)
            {
                list[index] = menu;
                await SaveToJsonAsync(list);
                return true;
            }

            return false;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var item = await _context.Menus.FindAsync(id);
                    if (item != null)
                    {
                        _context.Menus.Remove(item);
                        await _context.SaveChangesAsync();
                        await SyncToJsonAsync();
                        return true;
                    }
                }
                catch
                {
                    // fallback to json
                }
            }

            var list = await LoadFromJsonAsync();
            var itemToRemove = list.FirstOrDefault(m => m.MenuId == id);
            if (itemToRemove != null)
            {
                list.Remove(itemToRemove);
                await SaveToJsonAsync(list);
                return true;
            }

            return false;
        }

        public async Task<bool> TogglePropertyAsync(int id, string propertyName)
        {
            var menu = await GetByIdAsync(id);
            if (menu == null) return false;

            switch (propertyName.ToLowerInvariant())
            {
                case "ismenu":
                    menu.IsMenu = !menu.IsMenu;
                    break;
                case "istraceable":
                    menu.IsTraceable = !menu.IsTraceable;
                    break;
                case "isactive":
                    menu.IsActive = !menu.IsActive;
                    break;
                default:
                    return false;
            }

            return await UpdateAsync(menu);
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            var all = await GetAllAsync();
            return all.Select(m => m.MenuCategory)
                      .Where(c => !string.IsNullOrWhiteSpace(c))
                      .Distinct(StringComparer.OrdinalIgnoreCase)
                      .OrderBy(c => c)
                      .ToList();
        }

        public async Task<List<string>> GetAvailableControllersAsync()
        {
            var controllers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var controllerTypes = typeof(Program).Assembly.GetTypes()
                    .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract && t.Name.EndsWith("Controller"));

                foreach (var ct in controllerTypes)
                {
                    string name = ct.Name.Substring(0, ct.Name.Length - "Controller".Length);
                    controllers.Add(name);
                }
            }
            catch
            {
            }

            // Also add standard SuperMarket & image reference modules
            controllers.Add("ApprovalTypes");
            controllers.Add("Products");
            controllers.Add("Categories");
            controllers.Add("Suppliers");
            controllers.Add("Inventory");
            controllers.Add("Sales");
            controllers.Add("Roles");
            controllers.Add("Permissions");
            controllers.Add("Account");
            controllers.Add("PurchaseOrders");

            return controllers.OrderBy(c => c).ToList();
        }

        public async Task<List<string>> GetAvailableActionsAsync()
        {
            return new List<string>
            {
                "Index",
                "Create",
                "Edit",
                "Delete",
                "Details",
                "Export",
                "Trash",
                "Recover",
                "Print",
                "Status"
            };
        }

        public async Task<int> SyncControllersAsync()
        {
            var currentMenus = await GetAllAsync();
            int addedCount = 0;

            try
            {
                var controllerTypes = typeof(Program).Assembly.GetTypes()
                    .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract && t.Name.EndsWith("Controller"))
                    .ToList();

                foreach (var ct in controllerTypes)
                {
                    string ctrlName = ct.Name.Substring(0, ct.Name.Length - "Controller".Length);
                    string category = ResolveCategoryForController(ctrlName);

                    var actions = ct.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                        .Where(m => !m.IsSpecialName &&
                                    (typeof(IActionResult).IsAssignableFrom(m.ReturnType) ||
                                     typeof(Task<IActionResult>).IsAssignableFrom(m.ReturnType) ||
                                     m.ReturnType.Name.Contains("ActionResult")))
                        .Select(m => m.Name)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    foreach (var actionName in actions)
                    {
                        bool exists = currentMenus.Any(m =>
                            string.Equals(m.Controller, ctrlName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(m.Action, actionName, StringComparison.OrdinalIgnoreCase));

                        if (!exists)
                        {
                            bool isIndex = string.Equals(actionName, "Index", StringComparison.OrdinalIgnoreCase);
                            string title = isIndex ? ctrlName : $"{ctrlName} {actionName}";

                            var newMenu = new Menu
                            {
                                Title = title,
                                Controller = ctrlName,
                                Action = actionName,
                                IsMenu = isIndex,
                                IsTraceable = !isIndex,
                                IsActive = true,
                                MenuCategory = category,
                                SortOrder = isIndex ? 1 : 10
                            };

                            await CreateAsync(newMenu);
                            addedCount++;
                        }
                    }
                }
            }
            catch
            {
            }

            return addedCount;
        }

        public async Task ResetSeedDataAsync()
        {
            var seed = GetDefaultSeedMenus();

            if (await CanUseDatabaseAsync())
            {
                try
                {
                    _context.Menus.RemoveRange(_context.Menus);
                    await _context.SaveChangesAsync();
                    _context.Menus.AddRange(seed);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                }
            }

            await SaveToJsonAsync(seed);
        }

        public async Task<MenuIndexViewModel> GetIndexViewModelAsync(string? category, string? search, bool? isMenuOnly)
        {
            var all = await GetAllAsync();

            var categories = all.Select(m => m.MenuCategory)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();

            var filtered = all.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                filtered = filtered.Where(m => string.Equals(m.MenuCategory, category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                filtered = filtered.Where(m =>
                    (m.Title != null && m.Title.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (m.Controller != null && m.Controller.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (m.Action != null && m.Action.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (m.MenuCategory != null && m.MenuCategory.Contains(s, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (isMenuOnly.HasValue && isMenuOnly.Value)
            {
                filtered = filtered.Where(m => m.IsMenu);
            }

            var resultList = filtered
                .OrderBy(m => m.MenuCategory)
                .ThenBy(m => m.Controller)
                .ThenByDescending(m => m.IsMenu)
                .ThenBy(m => m.SortOrder)
                .ThenBy(m => m.Title)
                .ToList();

            return new MenuIndexViewModel
            {
                Menus = resultList,
                Categories = categories,
                SelectedCategory = category,
                SearchString = search,
                IsMenuOnly = isMenuOnly,
                TotalItems = all.Count,
                MenuCount = all.Count(m => m.IsMenu),
                ActiveCount = all.Count(m => m.IsActive),
                TraceableCount = all.Count(m => m.IsTraceable)
            };
        }

        private static string ResolveCategoryForController(string controller)
        {
            return controller.ToLowerInvariant() switch
            {
                "approvaltypes" or "approvals" => "Approval Settings",
                "roles" or "permissions" or "account" or "users" => "Security & Roles",
                "product" or "products" or "categories" or "category" or "batch" or "batches" => "Inventory",
                "sale" or "sales" or "pos" or "returns" or "customerreturns" => "Sales & Billing",
                "supplier" or "suppliers" or "purchase" or "purchases" or "purchaseorders" => "Purchases & Suppliers",
                "inventory" or "stock" or "adjustment" or "adjustments" or "transfers" or "location" or "locations" => "Inventory",
                "report" or "reports" or "analytics" => "Reports",
                _ => "General"
            };
        }

        private async Task<List<Menu>> LoadFromJsonAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                if (File.Exists(_jsonFilePath))
                {
                    string json = await File.ReadAllTextAsync(_jsonFilePath);
                    var data = JsonSerializer.Deserialize<List<Menu>>(json);
                    if (data != null && data.Any())
                    {
                        return data;
                    }
                }

                // If file doesn't exist or empty, write seed
                var seed = GetDefaultSeedMenus();
                string seedJson = JsonSerializer.Serialize(seed, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_jsonFilePath, seedJson);
                return seed;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task SaveToJsonAsync(List<Menu> menus)
        {
            await _fileLock.WaitAsync();
            try
            {
                string json = JsonSerializer.Serialize(menus, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_jsonFilePath, json);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task SyncToJsonAsync()
        {
            try
            {
                var list = await _context.Menus
                    .OrderBy(m => m.MenuCategory)
                    .ThenBy(m => m.SortOrder)
                    .ThenBy(m => m.Title)
                    .ToListAsync();
                await SaveToJsonAsync(list);
            }
            catch
            {
            }
        }

        public async Task<List<int>> GetMenuIdsForRoleAsync(int roleId)
        {
            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var role = await _context.Roles
                        .Include(r => r.Menus)
                        .FirstOrDefaultAsync(r => r.RoleId == roleId);

                    if (role != null && role.Menus.Any())
                    {
                        return role.Menus.Select(m => m.MenuId).ToList();
                    }
                }
                catch
                {
                }
            }

            var map = await LoadRoleMenusMapAsync();
            if (map.TryGetValue(roleId, out var ids))
            {
                return ids;
            }

            return new List<int>();
        }

        public async Task<List<Menu>> GetMenusForRoleAsync(int roleId)
        {
            var menuIds = await GetMenuIdsForRoleAsync(roleId);
            if (!menuIds.Any()) return new List<Menu>();

            var allMenus = await GetAllAsync();
            return allMenus.Where(m => menuIds.Contains(m.MenuId)).ToList();
        }

        public async Task SetRoleMenusAsync(int roleId, IEnumerable<int> menuIds)
        {
            var distinctIds = (menuIds ?? Enumerable.Empty<int>()).Distinct().ToList();

            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var role = await _context.Roles
                        .Include(r => r.Menus)
                        .FirstOrDefaultAsync(r => r.RoleId == roleId);

                    if (role != null)
                    {
                        var menusToAssign = await _context.Menus
                            .Where(m => distinctIds.Contains(m.MenuId))
                            .ToListAsync();

                        role.Menus.Clear();
                        foreach (var m in menusToAssign)
                        {
                            role.Menus.Add(m);
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                catch
                {
                }
            }

            var map = await LoadRoleMenusMapAsync();
            map[roleId] = distinctIds;
            await SaveRoleMenusMapAsync(map);
        }

        public async Task<Dictionary<int, int>> GetRoleMenuCountsAsync()
        {
            var result = new Dictionary<int, int>();

            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var roles = await _context.Roles
                        .Include(r => r.Menus)
                        .ToListAsync();

                    foreach (var r in roles)
                    {
                        result[r.RoleId] = r.Menus.Count;
                    }
                    if (result.Any()) return result;
                }
                catch
                {
                }
            }

            var map = await LoadRoleMenusMapAsync();
            foreach (var kvp in map)
            {
                result[kvp.Key] = kvp.Value.Count;
            }

            return result;
        }

        public async Task<Dictionary<int, List<string>>> GetRoleMenuTitlesAsync()
        {
            var result = new Dictionary<int, List<string>>();
            var allMenus = await GetAllAsync();
            var menuDict = allMenus.ToDictionary(m => m.MenuId, m => m.Title);

            var map = await LoadRoleMenusMapAsync();
            foreach (var kvp in map)
            {
                var titles = kvp.Value
                    .Where(id => menuDict.ContainsKey(id))
                    .Select(id => menuDict[id])
                    .ToList();
                result[kvp.Key] = titles;
            }

            return result;
        }

        private async Task<Dictionary<int, List<int>>> LoadRoleMenusMapAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                if (File.Exists(_roleMenuFilePath))
                {
                    string json = await File.ReadAllTextAsync(_roleMenuFilePath);
                    var data = JsonSerializer.Deserialize<Dictionary<int, List<int>>>(json);
                    if (data != null) return data;
                }
                return new Dictionary<int, List<int>>();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task SaveRoleMenusMapAsync(Dictionary<int, List<int>> map)
        {
            await _fileLock.WaitAsync();
            try
            {
                string json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_roleMenuFilePath, json);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public static List<Menu> GetDefaultSeedMenus()
        {
            int id = 1;
            var list = new List<Menu>
            {
                // EXACT ITEMS FROM USER REFERENCE IMAGE:
                new Menu { MenuId = id++, Title = "Approval Types", Controller = "ApprovalTypes", Action = "Index", IsMenu = true, IsTraceable = false, IsActive = true, MenuCategory = "Approval Settings", SortOrder = 1 },
                new Menu { MenuId = id++, Title = "ApprovalTypes Add New", Controller = "ApprovalTypes", Action = "Create", IsMenu = false, IsTraceable = false, IsActive = true, MenuCategory = "Approval Settings", SortOrder = 2 },
                new Menu { MenuId = id++, Title = "ApprovalTypes Edit", Controller = "ApprovalTypes", Action = "Edit", IsMenu = false, IsTraceable = false, IsActive = true, MenuCategory = "Approval Settings", SortOrder = 3 },
                new Menu { MenuId = id++, Title = "ApprovalTypes Delete", Controller = "ApprovalTypes", Action = "Delete", IsMenu = false, IsTraceable = false, IsActive = true, MenuCategory = "Approval Settings", SortOrder = 4 },
                new Menu { MenuId = id++, Title = "ApprovalTypes Export", Controller = "ApprovalTypes", Action = "Export", IsMenu = false, IsTraceable = false, IsActive = true, MenuCategory = "Approval Settings", SortOrder = 5 },
                new Menu { MenuId = id++, Title = "ApprovalTypes Trash", Controller = "ApprovalTypes", Action = "Trash", IsMenu = false, IsTraceable = false, IsActive = true, MenuCategory = "Approval Settings", SortOrder = 6 },
                new Menu { MenuId = id++, Title = "ApprovalTypes Recover", Controller = "ApprovalTypes", Action = "Recover", IsMenu = false, IsTraceable = false, IsActive = true, MenuCategory = "Approval Settings", SortOrder = 7 },

                // SUPERMARKET SECURITY & ROLES:
                new Menu { MenuId = id++, Title = "Roles", Controller = "Roles", Action = "Index", IsMenu = true, IsTraceable = false, IsActive = true, MenuCategory = "Security & Roles", SortOrder = 10 },
                new Menu { MenuId = id++, Title = "Roles Add New", Controller = "Roles", Action = "Create", IsMenu = false, IsTraceable = true, IsActive = true, MenuCategory = "Security & Roles", SortOrder = 11 },
                new Menu { MenuId = id++, Title = "Roles Edit", Controller = "Roles", Action = "Edit", IsMenu = false, IsTraceable = true, IsActive = true, MenuCategory = "Security & Roles", SortOrder = 12 },
                new Menu { MenuId = id++, Title = "Roles Delete", Controller = "Roles", Action = "Delete", IsMenu = false, IsTraceable = true, IsActive = true, MenuCategory = "Security & Roles", SortOrder = 13 },
                new Menu { MenuId = id++, Title = "Permissions", Controller = "Permissions", Action = "Index", IsMenu = true, IsTraceable = false, IsActive = true, MenuCategory = "Security & Roles", SortOrder = 14 },
                new Menu { MenuId = id++, Title = "Permissions Add New", Controller = "Permissions", Action = "Create", IsMenu = false, IsTraceable = true, IsActive = true, MenuCategory = "Security & Roles", SortOrder = 15 },
                new Menu { MenuId = id++, Title = "Permissions Assign", Controller = "Permissions", Action = "EditPermissions", IsMenu = false, IsTraceable = true, IsActive = true, MenuCategory = "Security & Roles", SortOrder = 16 },

                // USER MANAGEMENT:
                new Menu { MenuId = id++, Title = "Users List", Controller = "Account", Action = "Index", IsMenu = true, IsTraceable = false, IsActive = true, MenuCategory = "User Management", SortOrder = 20 },
                new Menu { MenuId = id++, Title = "User Login", Controller = "Account", Action = "Login", IsMenu = false, IsTraceable = true, IsActive = true, MenuCategory = "User Management", SortOrder = 21 },
                new Menu { MenuId = id++, Title = "User Logout", Controller = "Account", Action = "Logout", IsMenu = false, IsTraceable = true, IsActive = true, MenuCategory = "User Management", SortOrder = 22 },

                // INVENTORY:
                new Menu { MenuId = id++, Title = "Products List", Controller = "Products", Action = "Index", IsMenu = true, IsTraceable = false, IsActive = true, MenuCategory = "Inventory", SortOrder = 30 },
                new Menu { MenuId = id++, Title = "Products Add New", Controller = "Products", Action = "Create", IsMenu = false, IsTraceable = true, IsActive = true, MenuCategory = "Inventory", SortOrder = 31 },
                new Menu { MenuId = id++, Title = "Products Edit", Controller = "Products", Action = "Edit", IsMenu = false, IsTraceable = true, IsActive = true, MenuCategory = "Inventory", SortOrder = 32 },
                new Menu { MenuId = id++, Title = "Products Delete", Controller = "Products", Action = "Delete", IsMenu = false, IsTraceable = true, IsActive = true, MenuCategory = "Inventory", SortOrder = 33 },
                new Menu { MenuId = id++, Title = "Categories List", Controller = "Categories", Action = "Index", IsMenu = true, IsTraceable = false, IsActive = true, MenuCategory = "Inventory", SortOrder = 34 },
                new Menu { MenuId = id++, Title = "Categories Add New", Controller = "Categories", Action = "Create", IsMenu = false, IsTraceable = true, IsActive = true, MenuCategory = "Inventory", SortOrder = 35 },
                new Menu { MenuId = id++, Title = "Stock Transfers", Controller = "Inventory", Action = "Transfers", IsMenu = true, IsTraceable = false, IsActive = true, MenuCategory = "Inventory", SortOrder = 36 },
                new Menu { MenuId = id++, Title = "Stock Adjustments", Controller = "Inventory", Action = "Adjustments", IsMenu = true, IsTraceable = false, IsActive = true, MenuCategory = "Inventory", SortOrder = 37 },

                // SALES & BILLING:
                new Menu { MenuId = id++, Title = "Sales Dashboard", Controller = "Sales", Action = "Index", IsMenu = true, IsTraceable = false, IsActive = true, MenuCategory = "Sales & Billing", SortOrder = 40 },
                new Menu { MenuId = id++, Title = "New Sale (POS)", Controller = "Sales", Action = "Create", IsMenu = false, IsTraceable = true, IsActive = true, MenuCategory = "Sales & Billing", SortOrder = 41 },
                new Menu { MenuId = id++, Title = "Customer Returns", Controller = "Sales", Action = "Returns", IsMenu = true, IsTraceable = false, IsActive = true, MenuCategory = "Sales & Billing", SortOrder = 42 },

                // PURCHASES & SUPPLIERS:
                new Menu { MenuId = id++, Title = "Suppliers List", Controller = "Suppliers", Action = "Index", IsMenu = true, IsTraceable = false, IsActive = true, MenuCategory = "Purchases & Suppliers", SortOrder = 50 },
                new Menu { MenuId = id++, Title = "Suppliers Add New", Controller = "Suppliers", Action = "Create", IsMenu = false, IsTraceable = true, IsActive = true, MenuCategory = "Purchases & Suppliers", SortOrder = 51 },
                new Menu { MenuId = id++, Title = "Purchase Orders", Controller = "PurchaseOrders", Action = "Index", IsMenu = true, IsTraceable = false, IsActive = true, MenuCategory = "Purchases & Suppliers", SortOrder = 52 },
                new Menu { MenuId = id++, Title = "Purchase Order Create", Controller = "PurchaseOrders", Action = "Create", IsMenu = false, IsTraceable = true, IsActive = true, MenuCategory = "Purchases & Suppliers", SortOrder = 53 }
            };

            return list;
        }
    }
}

