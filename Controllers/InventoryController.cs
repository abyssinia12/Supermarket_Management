using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SuperMarket.Models.ViewModels;
using SuperMarket.Services;

namespace SuperMarket.Controllers
{
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        private int GetCurrentUserId()
        {
            var claim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out int uid) ? uid : 1;
        }

        // GET /Inventory
        //public async Task<IActionResult> Index()
        //{
        //    await _inventoryService.EnsureDefaultLocationsAsync();
        //    var dashboard = await _inventoryService.GetStockDashboardAsync();
        //    return View(dashboard);
        //}
        // GET /Inventory
        public async Task<IActionResult> Index()
        {
            var dashboard = await _inventoryService.GetStockDashboardAsync();
            return View(dashboard);
        }

        // GET /Inventory/StockByLocation
        public async Task<IActionResult> StockByLocation(int? locationId, int? productId, string? stockStatus)
        {
            var stock = await _inventoryService.GetStockByLocationAsync(locationId, productId, stockStatus);
            var locations = await _inventoryService.GetStockLocationsAsync();
            var products = await _inventoryService.GetActiveProductsForDropdownAsync();
            ViewBag.Locations = new SelectList(locations, "LocationId", "LocationName", locationId);
            ViewBag.Products = new SelectList(products, "ProductId", "ProductName", productId);
            ViewBag.SelectedLocationId = locationId;
            ViewBag.SelectedProductId = productId;
            ViewBag.SelectedStockStatus = stockStatus;
            return View(stock);
        }

        // GET /Inventory/Locations
        public async Task<IActionResult> Locations()
        {
            var locations = await _inventoryService.GetLocationsAsync();
            return View(locations);
        }

        // GET /Inventory/CreateLocation
        public IActionResult CreateLocation()
        {
            return View(new LocationFormViewModel());
        }

        // POST /Inventory/CreateLocation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLocation(LocationFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var (success, error, _) = await _inventoryService.CreateLocationAsync(model);
            if (success)
            {
                TempData["SuccessMessage"] = "Location created successfully.";
                return RedirectToAction(nameof(Locations));
            }
            ModelState.AddModelError("", error ?? "Failed to create location.");
            return View(model);
        }

        // GET /Inventory/EditLocation/5
        public async Task<IActionResult> EditLocation(int id)
        {
            var loc = await _inventoryService.GetLocationByIdAsync(id);
            if (loc == null) return NotFound();
            return View(new LocationFormViewModel
            {
                LocationId = loc.LocationId,
                LocationName = loc.LocationName,
                LocationType = loc.LocationType,
                IsActive = loc.IsActive ?? true
            });
        }

        // POST /Inventory/EditLocation/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLocation(int id, LocationFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            model.LocationId = id;
            var (success, error) = await _inventoryService.UpdateLocationAsync(model);
            if (success)
            {
                TempData["SuccessMessage"] = "Location updated successfully.";
                return RedirectToAction(nameof(Locations));
            }
            ModelState.AddModelError("", error ?? "Failed to update location.");
            return View(model);
        }

        // GET /Inventory/DeleteLocation/5
        public async Task<IActionResult> DeleteLocation(int id)
        {
            var loc = await _inventoryService.GetLocationByIdAsync(id);
            if (loc == null) return NotFound();
            return View(loc);
        }

        // POST /Inventory/DeleteLocation/5
        [HttpPost, ActionName("DeleteLocation")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLocationConfirmed(int id)
        {
            var success = await _inventoryService.DeleteLocationAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Location deleted." : "Failed to delete location.";
            return RedirectToAction(nameof(Locations));
        }

        // GET /Inventory/Valuation
        public async Task<IActionResult> Valuation()
        {
            var vm = await _inventoryService.GetStockValuationAsync();
            return View(vm);
        }
    }
}

