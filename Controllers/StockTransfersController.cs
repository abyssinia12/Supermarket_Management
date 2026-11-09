using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SuperMarket.Models.ViewModels;
using SuperMarket.Services;

namespace SuperMarket.Controllers
{
    public class StockTransfersController : Controller
    {
        private readonly IInventoryService _inventoryService;

        public StockTransfersController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        private int GetCurrentUserId()
        {
            var claim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out int uid) ? uid : 1;
        }

        // GET /StockTransfers
        public async Task<IActionResult> Index(string? status)
        {
            var transfers = await _inventoryService.GetTransfersAsync(status);
            ViewBag.StatusFilter = status;
            return View(transfers);
        }

        // GET /StockTransfers/Create
        public async Task<IActionResult> Create()
        {
            var model = new StockTransferFormViewModel();
            await PopulateDropdownsAsync(model);
            model.Items.Add(new TransferItemLineViewModel());
            return View(model);
        }

        // POST /StockTransfers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StockTransferFormViewModel model)
        {
            model.Items = model.Items.Where(i => i.ProductId > 0 && i.BatchId > 0 && i.Quantity > 0).ToList();
            if (!model.Items.Any())
                ModelState.AddModelError("", "Please add at least one valid transfer item.");

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model);
                if (!model.Items.Any()) model.Items.Add(new TransferItemLineViewModel());
                return View(model);
            }
            var (success, error, _) = await _inventoryService.CreateTransferAsync(model, GetCurrentUserId());
            if (success)
            {
                TempData["SuccessMessage"] = "Transfer request created successfully.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", error ?? "Failed to create transfer.");
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        // GET /StockTransfers/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var transfer = await _inventoryService.GetTransferDetailsAsync(id);
            if (transfer == null) return NotFound();
            return View(transfer);
        }

        // POST /StockTransfers/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var (success, error) = await _inventoryService.ApproveTransferAsync(id, GetCurrentUserId());
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Transfer approved." : (error ?? "Failed.");
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST /StockTransfers/Dispatch/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dispatch(int id)
        {
            var (success, error) = await _inventoryService.DispatchTransferAsync(id, GetCurrentUserId());
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Transfer dispatched." : (error ?? "Failed.");
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST /StockTransfers/Receive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Receive(int id)
        {
            var (success, error) = await _inventoryService.ReceiveTransferAsync(id, GetCurrentUserId());
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Transfer received. Stock updated." : (error ?? "Failed.");
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST /StockTransfers/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var (success, error) = await _inventoryService.CancelTransferAsync(id, GetCurrentUserId());
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Transfer cancelled." : (error ?? "Failed.");
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET /StockTransfers/GetBatchesForProduct?productId=1
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

        private async Task PopulateDropdownsAsync(StockTransferFormViewModel model)
        {
            var locations = await _inventoryService.GetLocationsAsync();
            var products = await _inventoryService.GetActiveProductsForDropdownAsync();
            model.AvailableLocations = locations.Select(l => new SelectListItem(l.LocationName, l.LocationId.ToString())).ToList();
            model.AvailableProducts = products.Select(p => new SelectListItem($"{p.ProductName} ({p.Sku})", p.ProductId.ToString())).ToList();
        }
    }
}

