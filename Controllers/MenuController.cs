using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SuperMarket.Models;
using SuperMarket.Models.ViewModels;
using SuperMarket.Services;

namespace SuperMarket.Controllers
{
    public class MenuController : Controller
    {
        private readonly IMenuService _menuService;

        public MenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        // GET: /Menu or /Menu/Index
        [HttpGet]
        public async Task<IActionResult> Index(string? category = null, string? search = null, bool? isMenuOnly = null)
        {
            var viewModel = await _menuService.GetIndexViewModelAsync(category, search, isMenuOnly);
            return View(viewModel);
        }

        // GET: /Menu/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new MenuFormViewModel
            {
                IsActive = true,
                IsMenu = false,
                IsTraceable = false,
                SortOrder = 1,
                MenuCategory = "General",
                AvailableControllers = await _menuService.GetAvailableControllersAsync(),
                AvailableActions = await _menuService.GetAvailableActionsAsync(),
                AvailableCategories = await _menuService.GetCategoriesAsync()
            };

            return View(model);
        }

        // POST: /Menu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var menu = new Menu
                {
                    Title = model.Title.Trim(),
                    Controller = model.Controller.Trim(),
                    Action = model.Action.Trim(),
                    IsMenu = model.IsMenu,
                    IsTraceable = model.IsTraceable,
                    IsActive = model.IsActive,
                    MenuCategory = model.MenuCategory.Trim(),
                    SortOrder = model.SortOrder,
                    Icon = model.Icon?.Trim(),
                    Description = model.Description?.Trim()
                };

                await _menuService.CreateAsync(menu);
                TempData["SuccessMessage"] = $"Menu item '{menu.Title}' was successfully created.";
                return RedirectToAction(nameof(Index));
            }

            model.AvailableControllers = await _menuService.GetAvailableControllersAsync();
            model.AvailableActions = await _menuService.GetAvailableActionsAsync();
            model.AvailableCategories = await _menuService.GetCategoriesAsync();
            return View(model);
        }

        // GET: /Menu/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var menu = await _menuService.GetByIdAsync(id.Value);
            if (menu == null) return NotFound();

            var model = new MenuFormViewModel
            {
                MenuId = menu.MenuId,
                Title = menu.Title,
                Controller = menu.Controller,
                Action = menu.Action,
                IsMenu = menu.IsMenu,
                IsTraceable = menu.IsTraceable,
                IsActive = menu.IsActive,
                MenuCategory = menu.MenuCategory,
                SortOrder = menu.SortOrder,
                Icon = menu.Icon,
                Description = menu.Description,
                AvailableControllers = await _menuService.GetAvailableControllersAsync(),
                AvailableActions = await _menuService.GetAvailableActionsAsync(),
                AvailableCategories = await _menuService.GetCategoriesAsync()
            };

            return View(model);
        }

        // POST: /Menu/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MenuFormViewModel model)
        {
            if (id != model.MenuId) return NotFound();

            if (ModelState.IsValid)
            {
                var menu = new Menu
                {
                    MenuId = model.MenuId,
                    Title = model.Title.Trim(),
                    Controller = model.Controller.Trim(),
                    Action = model.Action.Trim(),
                    IsMenu = model.IsMenu,
                    IsTraceable = model.IsTraceable,
                    IsActive = model.IsActive,
                    MenuCategory = model.MenuCategory.Trim(),
                    SortOrder = model.SortOrder,
                    Icon = model.Icon?.Trim(),
                    Description = model.Description?.Trim()
                };

                bool updated = await _menuService.UpdateAsync(menu);
                if (updated)
                {
                    TempData["SuccessMessage"] = $"Menu item '{menu.Title}' was updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Unable to find the menu item to update.");
                }
            }

            model.AvailableControllers = await _menuService.GetAvailableControllersAsync();
            model.AvailableActions = await _menuService.GetAvailableActionsAsync();
            model.AvailableCategories = await _menuService.GetCategoriesAsync();
            return View(model);
        }

        // GET: /Menu/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var menu = await _menuService.GetByIdAsync(id.Value);
            if (menu == null) return NotFound();

            return View(menu);
        }

        // GET: /Menu/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var menu = await _menuService.GetByIdAsync(id.Value);
            if (menu == null) return NotFound();

            return View(menu);
        }

        // POST: /Menu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var menu = await _menuService.GetByIdAsync(id);
            string title = menu?.Title ?? "Item";

            await _menuService.DeleteAsync(id);
            TempData["SuccessMessage"] = $"Menu item '{title}' has been deleted.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Menu/ToggleProperty
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProperty(int id, string property)
        {
            bool success = await _menuService.TogglePropertyAsync(id, property);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                var updatedItem = await _menuService.GetByIdAsync(id);
                bool? newValue = property.ToLowerInvariant() switch
                {
                    "ismenu" => updatedItem?.IsMenu,
                    "istraceable" => updatedItem?.IsTraceable,
                    "isactive" => updatedItem?.IsActive,
                    _ => null
                };

                return Json(new { success, newValue, id, property });
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Menu/SyncControllers
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncControllers()
        {
            int added = await _menuService.SyncControllersAsync();
            if (added > 0)
            {
                TempData["SuccessMessage"] = $"Scanned controllers successfully. Added {added} new action(s) to the menu.";
            }
            else
            {
                TempData["InfoMessage"] = "All controller actions are already synced in the menu.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Menu/ResetSeed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetSeed()
        {
            await _menuService.ResetSeedDataAsync();
            TempData["SuccessMessage"] = "Menu configuration has been reset to default reference seed data.";
            return RedirectToAction(nameof(Index));
        }
    }
}

