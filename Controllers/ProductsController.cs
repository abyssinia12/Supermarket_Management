using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SuperMarket.Models.ViewModels;
using SuperMarket.Services;

namespace SuperMarket.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // GET: /Products
        [HttpGet]
        public async Task<IActionResult> Index(int? categoryId, string? search, string? stockFilter)
        {
            var viewModel = await _productService.GetProductsAsync(categoryId, search, stockFilter);
            return View(viewModel);
        }

        // GET: /Products/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var viewModel = await _productService.GetProductDetailsAsync(id.Value);
            if (viewModel == null) return NotFound();

            return View(viewModel);
        }

        // GET: /Products/Create
        [HttpGet]
        public async Task<IActionResult> Create(int? categoryId)
        {
            var categories = await _productService.GetCategoriesAsync();
            var locations = await _productService.GetLocationsAsync();

            var model = new ProductFormViewModel
            {
                Sku = _productService.GenerateUniqueSku(),
                Barcode = _productService.GenerateUniqueBarcode(),
                UnitOfMeasure = "PCS",
                PurchaseCost = 0.00m,
                SellingPrice = 0.00m,
                MinStockLevel = 10,
                IsActive = true,
                CategoryId = categoryId ?? (categories.FirstOrDefault()?.CategoryId ?? 0),
                AvailableCategories = categories.Select(c => new SelectListItem(c.CategoryName, c.CategoryId.ToString())).ToList(),
                AvailableLocations = locations.Select(l => new SelectListItem(l.LocationName, l.LocationId.ToString())).ToList()
            };

            return View(model);
        }

        // POST: /Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _productService.CreateProductAsync(model);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Product '{model.ProductName}' (SKU: {model.Sku}) was registered successfully!";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to register product.");
            }

            var categories = await _productService.GetCategoriesAsync();
            var locations = await _productService.GetLocationsAsync();
            model.AvailableCategories = categories.Select(c => new SelectListItem(c.CategoryName, c.CategoryId.ToString())).ToList();
            model.AvailableLocations = locations.Select(l => new SelectListItem(l.LocationName, l.LocationId.ToString())).ToList();

            return View(model);
        }

        // GET: /Products/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _productService.GetProductByIdAsync(id.Value);
            if (product == null) return NotFound();

            var categories = await _productService.GetCategoriesAsync();

            var model = new ProductFormViewModel
            {
                ProductId = product.ProductId,
                Sku = product.Sku,
                Barcode = product.Barcode,
                ProductName = product.ProductName,
                Description = product.Description,
                CategoryId = product.CategoryId,
                UnitOfMeasure = product.UnitOfMeasure,
                PurchaseCost = product.PurchaseCost,
                SellingPrice = product.SellingPrice,
                MinStockLevel = product.MinStockLevel,
                IsActive = product.IsActive ?? true,
                AvailableCategories = categories.Select(c => new SelectListItem(c.CategoryName, c.CategoryId.ToString(), c.CategoryId == product.CategoryId)).ToList()
            };

            return View(model);
        }

        // POST: /Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
        {
            if (id != model.ProductId) return NotFound();

            if (ModelState.IsValid)
            {
                var result = await _productService.UpdateProductAsync(model);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Product '{model.ProductName}' was updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to update product.");
            }

            var categories = await _productService.GetCategoriesAsync();
            model.AvailableCategories = categories.Select(c => new SelectListItem(c.CategoryName, c.CategoryId.ToString(), c.CategoryId == model.CategoryId)).ToList();

            return View(model);
        }

        // GET: /Products/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var details = await _productService.GetProductDetailsAsync(id.Value);
            if (details == null) return NotFound();

            return View(details);
        }

        // POST: /Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            string name = product?.ProductName ?? "Product";

            bool deleted = await _productService.DeleteProductAsync(id);
            if (deleted)
            {
                TempData["SuccessMessage"] = $"Product '{name}' was deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not delete product.";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX endpoints for auto-generating SKU / Barcode
        [HttpGet]
        public IActionResult GenerateSku()
        {
            return Json(new { sku = _productService.GenerateUniqueSku() });
        }

        [HttpGet]
        public IActionResult GenerateBarcode()
        {
            return Json(new { barcode = _productService.GenerateUniqueBarcode() });
        }
    }
}

