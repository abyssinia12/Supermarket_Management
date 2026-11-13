using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SuperMarket.Models.ViewModels;
using SuperMarket.Services;

namespace SuperMarket.Controllers
{
    public class StockAdjustmentsController : Controller
    {
        private readonly IInventoryService _inventoryService;

        public StockAdjustmentsController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        private int GetCurrentUserId()
        {
            var claim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out int uid) ? uid : 1;
        }

        // GET /StockAdjustments
        public async Task<IActionResult> Index(string? status)
        {
            var adjustments = await _inventoryService.GetAdjustmentsAsync(status);
            ViewBag.StatusFilter = status;
            return View(adjustments);
        }

        // GET /StockAdjustments/Create
        public async Task<IActionResult> Create()
        {
            var model = new StockAdjustmentFormViewModel();
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        // POST /StockAdjustments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StockAdjustmentFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model);
                return View(model);
            }
            var (success, error, _) = await _inventoryService.CreateAdjustmentAsync(model, GetCurrentUserId());
            if (success)
            {
                TempData["SuccessMessage"] = "Stock adjustment submitted and pending approval.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", error ?? "Failed to submit adjustment.");
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        // GET /StockAdjustments/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var allAdj = await _inventoryService.GetAdjustmentsAsync();
            var vm = allAdj.FirstOrDefault(a => a.AdjustmentId == id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        // POST /StockAdjustments/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var (success, error) = await _inventoryService.ApproveAdjustmentAsync(id, GetCurrentUserId());
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Adjustment approved and stock updated."
                : (error ?? "Failed to approve.");
            return RedirectToAction(nameof(Index));
        }

        // POST /StockAdjustments/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var (success, error) = await _inventoryService.RejectAdjustmentAsync(id, GetCurrentUserId());
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Adjustment rejected."
                : (error ?? "Failed to reject.");
            return RedirectToAction(nameof(Index));
        }

        // GET /StockAdjustments/GetBatchesForProduct?productId=1
        public async Task<IActionResult> GetBatchesForProduct(int productId)
        {
            var batches = await _inventoryService.GetBatchesForProductAsync(productId);
            var result = batches.Select(b => new
            {
                value = b.BatchId,
                text = $"{b.BatchNumber} (Exp: {b.ExpirationDate?.ToString("yyyy-MM-dd") ?? "N/A"})"
            });
            return Json(result);
        }

        private async Task PopulateDropdownsAsync(StockAdjustmentFormViewModel model)
        {
            var products = await _inventoryService.GetActiveProductsForDropdownAsync();
            var locations = await _inventoryService.GetStockLocationsAsync();
            model.AvailableProducts = products.Select(p => new SelectListItem(p.ProductName, p.ProductId.ToString())).ToList();
            model.AvailableLocations = locations.Select(l => new SelectListItem(l.LocationName, l.LocationId.ToString())).ToList();
            if (model.ProductId > 0)
            {
                var batches = await _inventoryService.GetBatchesForProductAsync(model.ProductId);
                model.AvailableBatches = batches.Select(b =>
                    new SelectListItem($"{b.BatchNumber} (Exp: {b.ExpirationDate?.ToString("yyyy-MM-dd") ?? "N/A"})", b.BatchId.ToString())).ToList();
            }
        }
    }
}
