using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SuperMarket.Models.ViewModels;
using SuperMarket.Services;

namespace SuperMarket.Controllers
{
    public class CustomerReturnsController : Controller
    {
        private readonly IInventoryService _inventoryService;

        public CustomerReturnsController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        private int GetCurrentUserId()
        {
            var claim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out int uid) ? uid : 1;
        }

        // GET /CustomerReturns
        public async Task<IActionResult> Index()
        {
            var returns = await _inventoryService.GetReturnsAsync();
            return View(returns);
        }

        // GET /CustomerReturns/Create
        public async Task<IActionResult> Create()
        {
            var model = new CustomerReturnFormViewModel();
            await PopulateDropdownsAsync(model);
            model.Items.Add(new ReturnItemLineViewModel());
            return View(model);
        }

        // POST /CustomerReturns/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerReturnFormViewModel model)
        {
            model.Items = model.Items.Where(i => i.ProductId > 0 && i.BatchId > 0 && i.Quantity > 0).ToList();
            if (!model.Items.Any())
                ModelState.AddModelError("", "Please add at least one valid return item.");

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model);
                if (!model.Items.Any()) model.Items.Add(new ReturnItemLineViewModel());
                return View(model);
            }
            var (success, error, _) = await _inventoryService.CreateReturnAsync(model, GetCurrentUserId());
            if (success)
            {
                TempData["SuccessMessage"] = "Customer return recorded. Stock updated for resalable items.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", error ?? "Failed to record return.");
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        // GET /CustomerReturns/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var ret = await _inventoryService.GetReturnByIdAsync(id);
            if (ret == null) return NotFound();
            return View(ret);
        }

        // GET /CustomerReturns/GetBatchesForProduct?productId=1
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

        private async Task PopulateDropdownsAsync(CustomerReturnFormViewModel model)
        {
            var products = await _inventoryService.GetActiveProductsForDropdownAsync();
            var locations = await _inventoryService.GetLocationsAsync();
            model.AvailableProducts = products.Select(p => new SelectListItem(p.ProductName, p.ProductId.ToString())).ToList();
            model.AvailableLocations = locations.Select(l => new SelectListItem(l.LocationName, l.LocationId.ToString())).ToList();
        }
    }
}

