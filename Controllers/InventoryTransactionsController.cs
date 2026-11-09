using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SuperMarket.Models.ViewModels;
using SuperMarket.Services;

namespace SuperMarket.Controllers
{
    // READ-ONLY — No create/edit/delete — Immutable ledger
    public class InventoryTransactionsController : Controller
    {
        private readonly IInventoryService _inventoryService;

        public InventoryTransactionsController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        // GET /InventoryTransactions
        public async Task<IActionResult> Index(string? type, int? productId, int? locationId,
            DateTime? dateFrom, DateTime? dateTo, string? search)
        {
            var vm = await _inventoryService.GetTransactionsAsync(type, productId, locationId, dateFrom, dateTo, search);
            return View(vm);
        }

        // GET /InventoryTransactions/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var txn = await _inventoryService.GetTransactionByIdAsync(id);
            if (txn == null) return NotFound();
            return View(txn);
        }
    }
}

