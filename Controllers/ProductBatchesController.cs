using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SuperMarket.Models.ViewModels;
using SuperMarket.Services;

namespace SuperMarket.Controllers
{
    public class ProductBatchesController : Controller
    {
        private readonly IProductService _productService;

        public ProductBatchesController(IProductService productService)
        {
            _productService = productService;
        }

        // GET: /ProductBatches
        [HttpGet]
        public async Task<IActionResult> Index(int? productId)
        {
            var batches = await _productService.GetBatchesAsync(productId);
            var products = await _productService.GetProductsAsync();

            ViewBag.ProductId = productId;
            ViewBag.Products = products.Products;

            if (productId.HasValue && productId.Value > 0)
            {
                var prod = await _productService.GetProductByIdAsync(productId.Value);
                ViewBag.SelectedProductName = prod?.ProductName;
            }

            return View(batches);
        }

        // GET: /ProductBatches/Create
        [HttpGet]
        public async Task<IActionResult> Create(int? productId)
        {
            var products = await _productService.GetProductsAsync();
            var locations = await _productService.GetLocationsAsync();

            var model = new BatchFormViewModel
            {
                ProductId = productId ?? 0,
                BatchNumber = $"BATCH-{DateTime.UtcNow:yyyyMM}-{new Random().Next(100, 999)}",
                ManufactureDate = DateOnly.FromDateTime(DateTime.Today),
                ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(6)),
                IsActive = true,
                Quantity = 10,
                AvailableProducts = products.Products.Select(p => new SelectListItem($"{p.ProductName} ({p.Sku})", p.ProductId.ToString())).ToList(),
                AvailableLocations = locations.Select(l => new SelectListItem(l.LocationName, l.LocationId.ToString())).ToList()
            };

            if (productId.HasValue && productId.Value > 0)
            {
                var prod = await _productService.GetProductByIdAsync(productId.Value);
                if (prod != null)
                {
                    model.ProductName = prod.ProductName;
                    model.ProductSku = prod.Sku;
                    model.UnitCost = prod.PurchaseCost;
                }
            }

            return View(model);
        }

        // POST: /ProductBatches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BatchFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _productService.CreateBatchAsync(model);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Batch '{model.BatchNumber}' was created successfully with {model.Quantity} units recorded in stock!";
                    return RedirectToAction(nameof(Index), new { productId = model.ProductId });
                }

                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to create batch.");
            }

            var products = await _productService.GetProductsAsync();
            var locations = await _productService.GetLocationsAsync();
            model.AvailableProducts = products.Products.Select(p => new SelectListItem($"{p.ProductName} ({p.Sku})", p.ProductId.ToString())).ToList();
            model.AvailableLocations = locations.Select(l => new SelectListItem(l.LocationName, l.LocationId.ToString())).ToList();

            return View(model);
        }

        // GET: /ProductBatches/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var batch = await _productService.GetBatchByIdAsync(id.Value);
            if (batch == null) return NotFound();

            var products = await _productService.GetProductsAsync();
            var locations = await _productService.GetLocationsAsync();

            var model = new BatchFormViewModel
            {
                BatchId = batch.BatchId,
                ProductId = batch.ProductId,
                ProductName = batch.ProductName,
                ProductSku = batch.ProductSku,
                BatchNumber = batch.BatchNumber,
                ManufactureDate = batch.ManufactureDate,
                ExpirationDate = batch.ExpirationDate,
                UnitCost = batch.UnitCost,
                Quantity = batch.TotalQuantity,
                IsActive = batch.IsActive,
                AvailableProducts = products.Products.Select(p => new SelectListItem($"{p.ProductName} ({p.Sku})", p.ProductId.ToString(), p.ProductId == batch.ProductId)).ToList(),
                AvailableLocations = locations.Select(l => new SelectListItem(l.LocationName, l.LocationId.ToString())).ToList()
            };

            return View(model);
        }

        // POST: /ProductBatches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BatchFormViewModel model)
        {
            if (id != model.BatchId) return NotFound();

            if (ModelState.IsValid)
            {
                var result = await _productService.UpdateBatchAsync(model);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Batch '{model.BatchNumber}' was updated successfully!";
                    return RedirectToAction(nameof(Index), new { productId = model.ProductId });
                }

                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to update batch.");
            }

            var products = await _productService.GetProductsAsync();
            var locations = await _productService.GetLocationsAsync();
            model.AvailableProducts = products.Products.Select(p => new SelectListItem($"{p.ProductName} ({p.Sku})", p.ProductId.ToString(), p.ProductId == model.ProductId)).ToList();
            model.AvailableLocations = locations.Select(l => new SelectListItem(l.LocationName, l.LocationId.ToString())).ToList();

            return View(model);
        }

        // GET: /ProductBatches/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var batch = await _productService.GetBatchByIdAsync(id.Value);
            if (batch == null) return NotFound();

            return View(batch);
        }

        // GET: /ProductBatches/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var batch = await _productService.GetBatchByIdAsync(id.Value);
            if (batch == null) return NotFound();

            return View(batch);
        }

        // POST: /ProductBatches/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var batch = await _productService.GetBatchByIdAsync(id);
            int? prodId = batch?.ProductId;

            bool deleted = await _productService.DeleteBatchAsync(id);
            if (deleted)
            {
                TempData["SuccessMessage"] = $"Batch was deleted successfully.";
            }

            return RedirectToAction(nameof(Index), new { productId = prodId });
        }
    }
}

