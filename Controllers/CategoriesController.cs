using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SuperMarket.Models.ViewModels;
using SuperMarket.Services;

namespace SuperMarket.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly IProductService _productService;

        public CategoriesController(IProductService productService)
        {
            _productService = productService;
        }

        // GET: /Categories
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var categories = await _productService.GetCategoriesAsync();
            return View(categories);
        }

        // GET: /Categories/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CategoryFormViewModel { IsActive = true });
        }

        // POST: /Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _productService.CreateCategoryAsync(model);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Category '{model.CategoryName}' was created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to create category.");
            }

            return View(model);
        }

        // GET: /Categories/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _productService.GetCategoryByIdAsync(id.Value);
            if (category == null) return NotFound();

            var model = new CategoryFormViewModel
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                IsActive = category.IsActive,
                ProductCount = category.Products?.Count(p => !p.IsDeleted) ?? 0
            };

            return View(model);
        }

        // POST: /Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
        {
            if (id != model.CategoryId) return NotFound();

            if (ModelState.IsValid)
            {
                var result = await _productService.UpdateCategoryAsync(model);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Category '{model.CategoryName}' was updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to update category.");
            }

            return View(model);
        }

        // GET: /Categories/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var category = await _productService.GetCategoryByIdAsync(id.Value);
            if (category == null) return NotFound();

            var productsViewModel = await _productService.GetProductsAsync(categoryId: id.Value);
            ViewBag.Products = productsViewModel.Products;

            return View(category);
        }

        // GET: /Categories/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = await _productService.GetCategoryByIdAsync(id.Value);
            if (category == null) return NotFound();

            int prodCount = category.Products?.Count(p => !p.IsDeleted) ?? 0;
            ViewBag.ProductCount = prodCount;

            return View(category);
        }

        // POST: /Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _productService.GetCategoryByIdAsync(id);
            string name = category?.CategoryName ?? "Category";

            bool deleted = await _productService.DeleteCategoryAsync(id);
            if (deleted)
            {
                TempData["SuccessMessage"] = $"Category '{name}' was deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not delete category.";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX Quick Create Category (used inside Product Create modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickCreate(string categoryName, string? description)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return Json(new { success = false, message = "Category name is required." });
            }

            var model = new CategoryFormViewModel
            {
                CategoryName = categoryName.Trim(),
                Description = description?.Trim(),
                IsActive = true
            };

            var result = await _productService.CreateCategoryAsync(model);
            if (result.Success && result.Category != null)
            {
                return Json(new
                {
                    success = true,
                    categoryId = result.Category.CategoryId,
                    categoryName = result.Category.CategoryName
                });
            }

            return Json(new { success = false, message = result.ErrorMessage ?? "Failed to create category." });
        }
    }
}

