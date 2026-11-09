using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SuperMarket.Models;
using SuperMarket.Models.ViewModels;

namespace SuperMarket.Services
{
    // ─── JSON store DTOs ───────────────────────────────────────────────────────

    public class InvStoreData
    {
        public List<InvLocation> Locations { get; set; } = new();
        public List<InvAdjustment> Adjustments { get; set; } = new();
        public List<InvTransfer> Transfers { get; set; } = new();
        public List<InvTransferItem> TransferItems { get; set; } = new();
        public List<InvTransaction> Transactions { get; set; } = new();
        public List<InvReturn> Returns { get; set; } = new();
        public List<InvReturnItem> ReturnItems { get; set; } = new();
        public List<InvStock> Stocks { get; set; } = new();
        public bool Seeded { get; set; } = false;
    }

    public class InvLocation
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = "";
        public string LocationType { get; set; } = "Store";
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class InvAdjustment
    {
        public int AdjustmentId { get; set; }
        public int ProductId { get; set; }
        public int BatchId { get; set; }
        public int LocationId { get; set; }
        public int ReportedByUserId { get; set; }
        public int? ApprovedByUserId { get; set; }
        public decimal Quantity { get; set; }
        public string AdjustmentType { get; set; } = "";
        public string Reason { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
    }

    public class InvTransfer
    {
        public int TransferId { get; set; }
        public int FromLocationId { get; set; }
        public int ToLocationId { get; set; }
        public int RequestedByUserId { get; set; }
        public int? ApprovedByUserId { get; set; }
        public string Status { get; set; } = "Requested";
        public DateTime RequestedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? Notes { get; set; }
    }

    public class InvTransferItem
    {
        public int TransferItemId { get; set; }
        public int TransferId { get; set; }
        public int ProductId { get; set; }
        public int BatchId { get; set; }
        public decimal Quantity { get; set; }
    }

    public class InvTransaction
    {
        public int TransactionId { get; set; }
        public int ProductId { get; set; }
        public int BatchId { get; set; }
        public int LocationId { get; set; }
        public int UserId { get; set; }
        public string TransactionType { get; set; } = "";
        public decimal QuantityDelta { get; set; }
        public int? ReferenceId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
    }

    public class InvReturn
    {
        public int ReturnId { get; set; }
        public int SaleId { get; set; }
        public int ProcessedByUserId { get; set; }
        public DateTime ReturnDate { get; set; } = DateTime.Now;
        public decimal RefundAmount { get; set; }
        public string Reason { get; set; } = "";
        public int LocationId { get; set; }
        public string? Notes { get; set; }
    }

    public class InvReturnItem
    {
        public int ReturnItemId { get; set; }
        public int ReturnId { get; set; }
        public int ProductId { get; set; }
        public int BatchId { get; set; }
        public decimal Quantity { get; set; }
        public string RestockCondition { get; set; } = "Resalable";
    }

    public class InvStock
    {
        public int InventoryId { get; set; }
        public int ProductId { get; set; }
        public int BatchId { get; set; }
        public int LocationId { get; set; }
        public decimal Quantity { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    // ─── Internal product/batch DTOs (for reading ProductService JSON) ──────────

    internal class ProdStoreItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string Sku { get; set; } = "";
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public decimal PurchaseCost { get; set; }
        public decimal SellingPrice { get; set; }
        public int MinStockLevel { get; set; }
        public int ReorderPoint { get; set; }
        public int MaxStockLevel { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
    }

    internal class BatchStoreItem
    {
        public int BatchId { get; set; }
        public int ProductId { get; set; }
        public string BatchNumber { get; set; } = "";
        public DateOnly? ManufactureDate { get; set; }
        public DateOnly? ExpirationDate { get; set; }
        public decimal UnitCost { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; }
    }

    // ─── Service ───────────────────────────────────────────────────────────────

    public class InventoryService : IInventoryService
    {
        private readonly DBContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _storeFilePath;
        private static readonly SemaphoreSlim _fileLock = new(1, 1);

        public InventoryService(DBContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
            string appDataDir = Path.Combine(_env.ContentRootPath, "App_Data");
            if (!Directory.Exists(appDataDir))
                Directory.CreateDirectory(appDataDir);
            _storeFilePath = Path.Combine(appDataDir, "inventory_data.json");
        }

        // ── DB check ──────────────────────────────────────────────────────────

        private async Task<bool> CanUseDatabaseAsync()
        {
            try
            {
                if (_context.Database.CanConnect())
                {
                    _context.Locations.Take(1).ToList();
                    return true;
                }
            }
            catch { }
            return false;
        }

        // ── JSON helpers ──────────────────────────────────────────────────────

        private async Task<InvStoreData> LoadStoreAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                if (!File.Exists(_storeFilePath)) return new InvStoreData();
                var json = await File.ReadAllTextAsync(_storeFilePath);
                return JsonSerializer.Deserialize<InvStoreData>(json) ?? new InvStoreData();
            }
            finally { _fileLock.Release(); }
        }

        private async Task SaveStoreAsync(InvStoreData data)
        {
            await _fileLock.WaitAsync();
            try
            {
                var opts = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(data, opts);
                await File.WriteAllTextAsync(_storeFilePath, json);
            }
            finally { _fileLock.Release(); }
        }

        // ── Seed ──────────────────────────────────────────────────────────────

        public async Task EnsureDefaultLocationsAsync()
        {
            var store = await LoadStoreAsync();
            if (store.Seeded) return;

            if (!store.Locations.Any())
            {
                store.Locations.AddRange(new[]
                {
                    new InvLocation { LocationId=1, LocationName="Main Store Floor", LocationType="Store", IsActive=true },
                    new InvLocation { LocationId=2, LocationName="Central Warehouse", LocationType="Warehouse", IsActive=true },
                    new InvLocation { LocationId=3, LocationName="Branch A", LocationType="Branch", IsActive=true },
                });
            }

            if (!store.Stocks.Any())
            {
                store.Stocks.AddRange(new[]
                {
                    new InvStock { InventoryId=1, ProductId=1, BatchId=1, LocationId=1, Quantity=120 },
                    new InvStock { InventoryId=2, ProductId=1, BatchId=1, LocationId=2, Quantity=300 },
                    new InvStock { InventoryId=3, ProductId=2, BatchId=2, LocationId=1, Quantity=5 },
                    new InvStock { InventoryId=4, ProductId=3, BatchId=3, LocationId=1, Quantity=0 },
                    new InvStock { InventoryId=5, ProductId=4, BatchId=4, LocationId=2, Quantity=200 },
                    new InvStock { InventoryId=6, ProductId=5, BatchId=5, LocationId=1, Quantity=50 },
                });
            }

            if (!store.Transactions.Any())
            {
                store.Transactions.AddRange(new[]
                {
                    new InvTransaction { TransactionId=1, ProductId=1, BatchId=1, LocationId=1, UserId=1, TransactionType="OpeningStock", QuantityDelta=120, Timestamp=DateTime.Now.AddDays(-10), Notes="Opening stock" },
                    new InvTransaction { TransactionId=2, ProductId=1, BatchId=1, LocationId=2, UserId=1, TransactionType="OpeningStock", QuantityDelta=300, Timestamp=DateTime.Now.AddDays(-10), Notes="Opening stock warehouse" },
                    new InvTransaction { TransactionId=3, ProductId=2, BatchId=2, LocationId=1, UserId=1, TransactionType="Purchase", QuantityDelta=50, Timestamp=DateTime.Now.AddDays(-7), Notes="Purchase receipt" },
                    new InvTransaction { TransactionId=4, ProductId=2, BatchId=2, LocationId=1, UserId=1, TransactionType="Sale", QuantityDelta=-45, Timestamp=DateTime.Now.AddDays(-3), Notes="POS Sale" },
                    new InvTransaction { TransactionId=5, ProductId=3, BatchId=3, LocationId=1, UserId=1, TransactionType="Adjustment", QuantityDelta=-10, Timestamp=DateTime.Now.AddDays(-2), Notes="Damage write-off" },
                });
            }

            if (!store.Adjustments.Any())
            {
                store.Adjustments.AddRange(new[]
                {
                    new InvAdjustment { AdjustmentId=1, ProductId=1, BatchId=1, LocationId=1, ReportedByUserId=1, Quantity=-5, AdjustmentType="Damage", Reason="Broken bottles during stocking", Status="Pending", CreatedAt=DateTime.Now.AddDays(-2) },
                    new InvAdjustment { AdjustmentId=2, ProductId=2, BatchId=2, LocationId=1, ReportedByUserId=1, ApprovedByUserId=1, Quantity=-3, AdjustmentType="Loss", Reason="Inventory count discrepancy", Status="Approved", CreatedAt=DateTime.Now.AddDays(-5) },
                    new InvAdjustment { AdjustmentId=3, ProductId=3, BatchId=3, LocationId=1, ReportedByUserId=1, Quantity=10, AdjustmentType="Correction", Reason="Stock count correction after audit", Status="Pending", CreatedAt=DateTime.Now.AddDays(-1) },
                });
            }

            if (!store.Transfers.Any())
            {
                store.Transfers.Add(new InvTransfer { TransferId = 1, FromLocationId = 2, ToLocationId = 1, RequestedByUserId = 1, Status = "Approved", RequestedAt = DateTime.Now.AddDays(-3), Notes = "Replenish store from warehouse" });
                store.TransferItems.AddRange(new[]
                {
                    new InvTransferItem { TransferItemId=1, TransferId=1, ProductId=1, BatchId=1, Quantity=50 },
                    new InvTransferItem { TransferItemId=2, TransferId=1, ProductId=4, BatchId=4, Quantity=20 },
                });
            }

            if (!store.Returns.Any())
            {
                store.Returns.Add(new InvReturn { ReturnId = 1, SaleId = 1001, ProcessedByUserId = 1, ReturnDate = DateTime.Now.AddDays(-1), RefundAmount = 25.50m, Reason = "Product defective", LocationId = 1 });
                store.ReturnItems.Add(new InvReturnItem { ReturnItemId = 1, ReturnId = 1, ProductId = 1, BatchId = 1, Quantity = 2, RestockCondition = "Damaged" });
            }

            store.Seeded = true;
            await SaveStoreAsync(store);
        }

        // ── Locations ─────────────────────────────────────────────────────────

        public async Task<List<Location>> GetLocationsAsync()
        {
            if (await CanUseDatabaseAsync())
            {
                try
                {
                    return await _context.Locations
                        .Where(l => !l.IsDeleted)
                        .OrderBy(l => l.LocationName)
                        .ToListAsync();
                }
                catch { }
            }

            await EnsureDefaultLocationsAsync();
            var store = await LoadStoreAsync();
            return store.Locations.Where(l => !l.IsDeleted).OrderBy(l => l.LocationName)
                .Select(l => new Location
                {
                    LocationId = l.LocationId,
                    LocationName = l.LocationName,
                    LocationType = l.LocationType,
                    IsActive = l.IsActive,
                    IsDeleted = l.IsDeleted,
                    CreatedAt = l.CreatedAt,
                    UpdatedAt = l.UpdatedAt
                }).ToList();
        }

        public async Task<Location?> GetLocationByIdAsync(int id)
        {
            if (await CanUseDatabaseAsync())
            {
                try { return await _context.Locations.FirstOrDefaultAsync(l => l.LocationId == id && !l.IsDeleted); }
                catch { }
            }
            var store = await LoadStoreAsync();
            var l = store.Locations.FirstOrDefault(x => x.LocationId == id && !x.IsDeleted);
            if (l == null) return null;
            return new Location { LocationId = l.LocationId, LocationName = l.LocationName, LocationType = l.LocationType, IsActive = l.IsActive, IsDeleted = l.IsDeleted, CreatedAt = l.CreatedAt, UpdatedAt = l.UpdatedAt };
        }

        public async Task<(bool Success, string? Error, Location? Location)> CreateLocationAsync(LocationFormViewModel model)
        {
            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var loc = new Location { LocationName = model.LocationName, LocationType = model.LocationType, IsActive = model.IsActive, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now };
                    _context.Locations.Add(loc);
                    await _context.SaveChangesAsync();
                    return (true, null, loc);
                }
                catch (Exception ex) { return (false, ex.Message, null); }
            }
            await EnsureDefaultLocationsAsync();
            var store = await LoadStoreAsync();
            int nextId = store.Locations.Any() ? store.Locations.Max(x => x.LocationId) + 1 : 1;
            var newLoc = new InvLocation { LocationId = nextId, LocationName = model.LocationName, LocationType = model.LocationType, IsActive = model.IsActive };
            store.Locations.Add(newLoc);
            await SaveStoreAsync(store);
            return (true, null, new Location { LocationId = nextId, LocationName = model.LocationName, LocationType = model.LocationType, IsActive = model.IsActive });
        }

        public async Task<(bool Success, string? Error)> UpdateLocationAsync(LocationFormViewModel model)
        {
            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var loc = await _context.Locations.FindAsync(model.LocationId);
                    if (loc == null) return (false, "Location not found");
                    loc.LocationName = model.LocationName; loc.LocationType = model.LocationType;
                    loc.IsActive = model.IsActive; loc.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return (true, null);
                }
                catch (Exception ex) { return (false, ex.Message); }
            }
            var store = await LoadStoreAsync();
            var l = store.Locations.FirstOrDefault(x => x.LocationId == model.LocationId);
            if (l == null) return (false, "Location not found");
            l.LocationName = model.LocationName; l.LocationType = model.LocationType;
            l.IsActive = model.IsActive; l.UpdatedAt = DateTime.Now;
            await SaveStoreAsync(store);
            return (true, null);
        }

        public async Task<bool> DeleteLocationAsync(int id)
        {
            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var loc = await _context.Locations.FindAsync(id);
                    if (loc == null) return false;
                    loc.IsDeleted = true; loc.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return true;
                }
                catch { }
            }
            var store = await LoadStoreAsync();
            var l = store.Locations.FirstOrDefault(x => x.LocationId == id);
            if (l == null) return false;
            l.IsDeleted = true; l.UpdatedAt = DateTime.Now;
            await SaveStoreAsync(store);
            return true;
        }

        // ── Stock overview ────────────────────────────────────────────────────

        private static string ComputeStockStatus(decimal qty, int min, int reorder)
        {
            if (qty <= 0) return "OutOfStock";
            if (qty <= min) return "Low";
            if (qty <= reorder) return "Low";
            return "Normal";
        }

        public async Task<List<LocationStockViewModel>> GetStockByLocationAsync(int? locationId = null, int? productId = null, string? stockStatus = null)
        {
            await EnsureDefaultLocationsAsync();
            var result = new List<LocationStockViewModel>();
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (await CanUseDatabaseAsync())
            {
                try
                {
                    //var query = _context.InventoryStocks
                    //    .Include(s => s.Product).ThenInclude(p => p.Category)
                    //    .Include(s => s.Batch)
                    //    .Include(s => s.Location)
                    //    .Where(s => !s.Location.IsDeleted && !s.Product.IsDeleted && s.Product.IsActive);
                    //if (locationId.HasValue) query = query.Where(s => s.LocationId == locationId.Value);
                    //if (productId.HasValue) query = query.Where(s => s.ProductId == productId.Value);
                    //var stocks = await query.ToListAsync();
                    var query = _context.InventoryStocks
                            .Include(s => s.Product)
                                .ThenInclude(p => p.Category)
                            .Include(s => s.Batch)
                            .Include(s => s.Location)
                            .Where(s =>
                                s.Location.IsDeleted == false &&
                                s.Product.IsDeleted == false &&
                                s.Product.IsActive == true
                            );

                    if (locationId.HasValue)
                        query = query.Where(s => s.LocationId == locationId.Value);

                    if (productId.HasValue)
                        query = query.Where(s => s.ProductId == productId.Value);

                    var stocks = await query.ToListAsync();
                    foreach (var s in stocks)
                    {
                        //var status = ComputeStockStatus(s.Quantity, s.Product.MinStockLevel, s.Product.ReorderPoint);
                        bool expiring = s.Batch.ExpirationDate.HasValue && s.Batch.ExpirationDate.Value > today && s.Batch.ExpirationDate.Value <= today.AddDays(30);
                        bool expired = s.Batch.ExpirationDate.HasValue && s.Batch.ExpirationDate.Value <= today;
                        result.Add(new LocationStockViewModel
                        {
                            InventoryId = s.InventoryId,
                            LocationId = s.LocationId,
                            LocationName = s.Location.LocationName,
                            LocationType = s.Location.LocationType,
                            ProductId = s.ProductId,
                            ProductName = s.Product.ProductName,
                            ProductSku = s.Product.Sku,
                            CategoryName = s.Product.Category?.CategoryName ?? "",
                            BatchId = s.BatchId,
                            BatchNumber = s.Batch.BatchNumber,
                            ExpirationDate = s.Batch.ExpirationDate,
                            Quantity = s.Quantity,
                            //MinStockLevel=s.Product.MinStockLevel, ReorderPoint=s.Product.ReorderPoint,
                            //MaxStockLevel=s.Product.MaxStockLevel, StockStatus=status,
                            UnitCost = s.Batch.UnitCost,
                            InventoryValue = s.Quantity * s.Batch.UnitCost,
                            IsExpiringSoon = expiring,
                            IsExpired = expired
                        });
                    }
                    if (!string.IsNullOrEmpty(stockStatus) && stockStatus != "All")
                        result = result.Where(r => r.StockStatus == stockStatus || (stockStatus == "Expiring" && r.IsExpiringSoon)).ToList();
                    return result;
                }
                catch { }
            }

            // JSON fallback
            var store = await LoadStoreAsync();
            var products = await GetProductsFromStoreAsync();
            var batches = await GetBatchesFromStoreAsync();
            var locs = store.Locations.Where(l => !l.IsDeleted).ToList();

            var stockList = store.Stocks.AsEnumerable();
            if (locationId.HasValue) stockList = stockList.Where(s => s.LocationId == locationId.Value);
            if (productId.HasValue) stockList = stockList.Where(s => s.ProductId == productId.Value);

            foreach (var s in stockList)
            {
                var prod = products.FirstOrDefault(p => p.ProductId == s.ProductId);
                var batch = batches.FirstOrDefault(b => b.BatchId == s.BatchId);
                var loc = locs.FirstOrDefault(l => l.LocationId == s.LocationId);
                if (prod == null || batch == null || loc == null) continue;
                //var status = ComputeStockStatus(s.Quantity, prod.MinStockLevel, prod.ReorderPoint);
                bool expiring = batch.ExpirationDate.HasValue && batch.ExpirationDate.Value > today && batch.ExpirationDate.Value <= today.AddDays(30);
                bool expired = batch.ExpirationDate.HasValue && batch.ExpirationDate.Value <= today;
                result.Add(new LocationStockViewModel
                {
                    InventoryId = s.InventoryId,
                    LocationId = s.LocationId,
                    LocationName = loc.LocationName,
                    LocationType = loc.LocationType,
                    ProductId = s.ProductId,
                    ProductName = prod.ProductName,
                    ProductSku = prod.Sku,
                    CategoryName = prod.CategoryName,
                    BatchId = s.BatchId,
                    BatchNumber = batch.BatchNumber,
                    ExpirationDate = batch.ExpirationDate,
                    Quantity = s.Quantity,
                    MinStockLevel = prod.MinStockLevel,
                    ReorderPoint = prod.ReorderPoint,
                    MaxStockLevel = prod.MaxStockLevel,
                    StockStatus = status,
                    UnitCost = batch.UnitCost,
                    InventoryValue = s.Quantity * batch.UnitCost,
                    IsExpiringSoon = expiring,
                    IsExpired = expired
                });
            }
            if (!string.IsNullOrEmpty(stockStatus) && stockStatus != "All")
                result = result.Where(r => r.StockStatus == stockStatus || (stockStatus == "Expiring" && r.IsExpiringSoon)).ToList();
            return result;
        }

        public async Task<List<LocationSummaryViewModel>> GetLocationSummariesAsync()
        {
            var locations = await GetLocationsAsync();
            var allStock = await GetStockByLocationAsync();
            var result = new List<LocationSummaryViewModel>();
            foreach (var loc in locations)
            {
                var locStock = allStock.Where(s => s.LocationId == loc.LocationId).ToList();
                result.Add(new LocationSummaryViewModel
                {
                    LocationId = loc.LocationId,
                    LocationName = loc.LocationName,
                    LocationType = loc.LocationType,
                    IsActive = loc.IsActive ?? true,
                    TotalProducts = locStock.Select(s => s.ProductId).Distinct().Count(),
                    TotalUnits = locStock.Sum(s => s.Quantity),
                    TotalInventoryValue = locStock.Sum(s => s.InventoryValue),
                    LowStockCount = locStock.Count(s => s.StockStatus == "Low"),
                    OutOfStockCount = locStock.Count(s => s.StockStatus == "OutOfStock"),
                    ExpiringBatchCount = locStock.Count(s => s.IsExpiringSoon)
                });
            }
            return result;
        }

        public async Task<decimal> GetTotalInventoryValueAsync()
        {
            var allStock = await GetStockByLocationAsync();
            return allStock.Sum(s => s.InventoryValue);
        }

        public async Task<StockDashboardViewModel> GetStockDashboardAsync()
        {
            await EnsureDefaultLocationsAsync();
            var summaries = await GetLocationSummariesAsync();
            var allStock = await GetStockByLocationAsync();
            var recentTxns = await GetTransactionsAsync();
            var adjustments = await GetAdjustmentsAsync("Pending");
            var transfers = await GetTransfersAsync("Requested");

            return new StockDashboardViewModel
            {
                Locations = summaries,
                TotalInventoryValue = summaries.Sum(l => l.TotalInventoryValue),
                TotalLowStockAlerts = summaries.Sum(l => l.LowStockCount + l.OutOfStockCount),
                TotalExpiringBatches = summaries.Sum(l => l.ExpiringBatchCount),
                TotalActiveLocations = summaries.Count(l => l.IsActive),
                LowStockAlerts = allStock.Where(s => s.StockStatus == "Low" || s.StockStatus == "OutOfStock").Take(10).ToList(),
                RecentTransactions = recentTxns.Transactions.OrderByDescending(t => t.Timestamp).Take(8).ToList(),
                PendingAdjustments = adjustments.Count,
                PendingTransfers = transfers.Count
            };
        }

        // ── Transactions (immutable) ───────────────────────────────────────────

        public async Task<InventoryTransactionFilterViewModel> GetTransactionsAsync(
            string? type = null, int? productId = null, int? locationId = null,
            DateTime? from = null, DateTime? to = null, string? search = null)
        {
            await EnsureDefaultLocationsAsync();
            var vm = new InventoryTransactionFilterViewModel
            {
                TransactionType = type,
                ProductId = productId,
                LocationId = locationId,
                DateFrom = from,
                DateTo = to,
                SearchTerm = search,
                AvailableTypes = new List<SelectListItem>
                {
                    new("All Types",""),
                    new("Purchase","Purchase"), new("Sale","Sale"),
                    new("Adjustment","Adjustment"), new("Transfer Out","TransferOut"),
                    new("Transfer In","TransferIn"), new("Return","Return"),
                    new("Opening Stock","OpeningStock")
                }
            };

            var products = await GetProductsFromStoreAsync();
            var locs = await GetLocationsAsync();
            vm.AvailableProducts = products.Select(p => new SelectListItem(p.ProductName, p.ProductId.ToString()))
                .Prepend(new SelectListItem("All Products", "")).ToList();
            vm.AvailableLocations = locs.Select(l => new SelectListItem(l.LocationName, l.LocationId.ToString()))
                .Prepend(new SelectListItem("All Locations", "")).ToList();

            var txnList = new List<InventoryTransactionListViewModel>();

            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var query = _context.InventoryTransactions
                        .Include(t => t.Product).Include(t => t.Batch)
                        .Include(t => t.Location).Include(t => t.User)
                        .AsQueryable();
                    if (!string.IsNullOrEmpty(type)) query = query.Where(t => t.TransactionType == type);
                    if (productId.HasValue) query = query.Where(t => t.ProductId == productId.Value);
                    if (locationId.HasValue) query = query.Where(t => t.LocationId == locationId.Value);
                    if (from.HasValue) query = query.Where(t => t.Timestamp >= from.Value);
                    if (to.HasValue) query = query.Where(t => t.Timestamp <= to.Value);
                    var txns = await query.OrderByDescending(t => t.Timestamp).Take(200).ToListAsync();
                    foreach (var t in txns)
                    {
                        var row = new InventoryTransactionListViewModel
                        {
                            TransactionId = t.TransactionId,
                            ProductName = t.Product.ProductName,
                            ProductSku = t.Product.Sku,
                            BatchNumber = t.Batch?.BatchNumber,
                            LocationName = t.Location.LocationName,
                            UserName = t.User?.FullName ?? $"User#{t.UserId}",
                            TransactionType = t.TransactionType,
                            QuantityDelta = t.QuantityDelta,
                            ReferenceId = t.ReferenceId,
                            Timestamp = t.Timestamp
                        };
                        if (!string.IsNullOrEmpty(search))
                        {
                            var s = search.ToLower();
                            if (!row.ProductName.ToLower().Contains(s) && !row.TransactionType.ToLower().Contains(s)) continue;
                        }
                        txnList.Add(row);
                    }
                    vm.Transactions = txnList;
                    return vm;
                }
                catch { }
            }

            // JSON fallback
            var store = await LoadStoreAsync();
            var prodsFb = await GetProductsFromStoreAsync();
            var batchesFb = await GetBatchesFromStoreAsync();
            var locsFb = await GetLocationsAsync();
            var txnsJson = store.Transactions.AsEnumerable();
            if (!string.IsNullOrEmpty(type)) txnsJson = txnsJson.Where(t => t.TransactionType == type);
            if (productId.HasValue) txnsJson = txnsJson.Where(t => t.ProductId == productId.Value);
            if (locationId.HasValue) txnsJson = txnsJson.Where(t => t.LocationId == locationId.Value);
            if (from.HasValue) txnsJson = txnsJson.Where(t => t.Timestamp >= from.Value);
            if (to.HasValue) txnsJson = txnsJson.Where(t => t.Timestamp <= to.Value);

            foreach (var t in txnsJson.OrderByDescending(x => x.Timestamp).Take(200))
            {
                var prod = prodsFb.FirstOrDefault(p => p.ProductId == t.ProductId);
                var batch = batchesFb.FirstOrDefault(b => b.BatchId == t.BatchId);
                var loc = locsFb.FirstOrDefault(l => l.LocationId == t.LocationId);
                var row = new InventoryTransactionListViewModel
                {
                    TransactionId = t.TransactionId,
                    ProductName = prod?.ProductName ?? $"Product#{t.ProductId}",
                    ProductSku = prod?.Sku ?? "",
                    BatchNumber = batch?.BatchNumber,
                    LocationName = loc?.LocationName ?? $"Location#{t.LocationId}",
                    UserName = $"User#{t.UserId}",
                    TransactionType = t.TransactionType,
                    QuantityDelta = t.QuantityDelta,
                    ReferenceId = t.ReferenceId,
                    Timestamp = t.Timestamp,
                    Notes = t.Notes
                };
                if (!string.IsNullOrEmpty(search))
                {
                    var s = search.ToLower();
                    if (!row.ProductName.ToLower().Contains(s) && !row.TransactionType.ToLower().Contains(s)) continue;
                }
                txnList.Add(row);
            }
            vm.Transactions = txnList;
            return vm;
        }

        public async Task<InventoryTransactionListViewModel?> GetTransactionByIdAsync(int id)
        {
            var all = await GetTransactionsAsync();
            return all.Transactions.FirstOrDefault(t => t.TransactionId == id);
        }

        // CRITICAL: append-only, never update/delete
        public async Task RecordTransactionAsync(int productId, int batchId, int locationId, int userId,
            string transactionType, decimal quantityDelta, int? referenceId = null, string? notes = null)
        {
            if (await CanUseDatabaseAsync())
            {
                try
                {
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = productId,
                        BatchId = batchId,
                        LocationId = locationId,
                        UserId = userId,
                        TransactionType = transactionType,
                        QuantityDelta = quantityDelta,
                        ReferenceId = referenceId,
                        Timestamp = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                    return;
                }
                catch { }
            }
            var store = await LoadStoreAsync();
            int nextId = store.Transactions.Any() ? store.Transactions.Max(t => t.TransactionId) + 1 : 1;
            store.Transactions.Add(new InvTransaction
            {
                TransactionId = nextId,
                ProductId = productId,
                BatchId = batchId,
                LocationId = locationId,
                UserId = userId,
                TransactionType = transactionType,
                QuantityDelta = quantityDelta,
                ReferenceId = referenceId,
                Timestamp = DateTime.Now,
                Notes = notes
            });
            await SaveStoreAsync(store);
        }

        // ── Adjustments ───────────────────────────────────────────────────────

        public async Task<List<StockAdjustmentListViewModel>> GetAdjustmentsAsync(string? status = null)
        {
            await EnsureDefaultLocationsAsync();
            var result = new List<StockAdjustmentListViewModel>();

            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var query = _context.InventoryAdjustments
                        .Include(a => a.Product).Include(a => a.Batch).Include(a => a.Location)
                        .Include(a => a.ReportedByUser).Include(a => a.ApprovedByUser)
                        .AsQueryable();
                    if (!string.IsNullOrEmpty(status)) query = query.Where(a => a.Status == status);
                    var list = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
                    foreach (var a in list)
                    {
                        result.Add(new StockAdjustmentListViewModel
                        {
                            AdjustmentId = a.AdjustmentId,
                            ProductName = a.Product.ProductName,
                            BatchNumber = a.Batch?.BatchNumber,
                            LocationName = a.Location.LocationName,
                            AdjustmentType = a.AdjustmentType,
                            Quantity = a.Quantity,
                            Reason = a.Reason,
                            Status = a.Status,
                            ReportedByUser = a.ReportedByUser?.FullName ?? $"User#{a.ReportedByUserId}",
                            ApprovedByUser = a.ApprovedByUser?.FullName,
                            CreatedAt = a.CreatedAt
                        });
                    }
                    return result;
                }
                catch { }
            }

            var store = await LoadStoreAsync();
            var prods = await GetProductsFromStoreAsync();
            var batches = await GetBatchesFromStoreAsync();
            var locs = await GetLocationsAsync();
            var adjustments = store.Adjustments.AsEnumerable();
            if (!string.IsNullOrEmpty(status)) adjustments = adjustments.Where(a => a.Status == status);
            foreach (var a in adjustments.OrderByDescending(x => x.CreatedAt))
            {
                var prod = prods.FirstOrDefault(p => p.ProductId == a.ProductId);
                var batch = batches.FirstOrDefault(b => b.BatchId == a.BatchId);
                var loc = locs.FirstOrDefault(l => l.LocationId == a.LocationId);
                result.Add(new StockAdjustmentListViewModel
                {
                    AdjustmentId = a.AdjustmentId,
                    ProductName = prod?.ProductName ?? $"Product#{a.ProductId}",
                    BatchNumber = batch?.BatchNumber,
                    LocationName = loc?.LocationName ?? $"Loc#{a.LocationId}",
                    AdjustmentType = a.AdjustmentType,
                    Quantity = a.Quantity,
                    Reason = a.Reason,
                    Status = a.Status,
                    ReportedByUser = $"User#{a.ReportedByUserId}",
                    ApprovedByUser = a.ApprovedByUserId.HasValue ? $"User#{a.ApprovedByUserId}" : null,
                    CreatedAt = a.CreatedAt
                });
            }
            return result;
        }

        public async Task<InventoryAdjustment?> GetAdjustmentByIdAsync(int id)
        {
            if (await CanUseDatabaseAsync())
            {
                try
                {
                    return await _context.InventoryAdjustments
                        .Include(a => a.Product).Include(a => a.Batch).Include(a => a.Location)
                        .Include(a => a.ReportedByUser).Include(a => a.ApprovedByUser)
                        .FirstOrDefaultAsync(a => a.AdjustmentId == id);
                }
                catch { }
            }
            var store = await LoadStoreAsync();
            var a2 = store.Adjustments.FirstOrDefault(x => x.AdjustmentId == id);
            if (a2 == null) return null;
            return new InventoryAdjustment
            {
                AdjustmentId = a2.AdjustmentId,
                ProductId = a2.ProductId,
                BatchId = a2.BatchId,
                LocationId = a2.LocationId,
                ReportedByUserId = a2.ReportedByUserId,
                ApprovedByUserId = a2.ApprovedByUserId,
                Quantity = a2.Quantity,
                AdjustmentType = a2.AdjustmentType,
                Reason = a2.Reason,
                Status = a2.Status,
                CreatedAt = a2.CreatedAt
            };
        }

        public async Task<(bool Success, string? Error, InventoryAdjustment? Adjustment)> CreateAdjustmentAsync(
            StockAdjustmentFormViewModel model, int userId)
        {
            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var adj = new InventoryAdjustment
                    {
                        ProductId = model.ProductId,
                        BatchId = model.BatchId,
                        LocationId = model.LocationId,
                        ReportedByUserId = userId,
                        Quantity = model.Quantity,
                        AdjustmentType = model.AdjustmentType,
                        Reason = model.Reason,
                        Status = "Pending",
                        CreatedAt = DateTime.Now
                    };
                    _context.InventoryAdjustments.Add(adj);
                    await _context.SaveChangesAsync();
                    return (true, null, adj);
                }
                catch (Exception ex) { return (false, ex.Message, null); }
            }
            var store = await LoadStoreAsync();
            int nextId = store.Adjustments.Any() ? store.Adjustments.Max(a => a.AdjustmentId) + 1 : 1;
            var newAdj = new InvAdjustment
            {
                AdjustmentId = nextId,
                ProductId = model.ProductId,
                BatchId = model.BatchId,
                LocationId = model.LocationId,
                ReportedByUserId = userId,
                Quantity = model.Quantity,
                AdjustmentType = model.AdjustmentType,
                Reason = model.Reason,
                Status = "Pending",
                CreatedAt = DateTime.Now,
                Notes = model.Notes
            };
            store.Adjustments.Add(newAdj);
            await SaveStoreAsync(store);
            return (true, null, new InventoryAdjustment
            {
                AdjustmentId = nextId,
                ProductId = model.ProductId,
                BatchId = model.BatchId,
                LocationId = model.LocationId,
                ReportedByUserId = userId,
                Quantity = model.Quantity,
                AdjustmentType = model.AdjustmentType,
                Reason = model.Reason,
                Status = "Pending",
                CreatedAt = DateTime.Now
            });
        }

        private async Task UpdateStockQuantityAsync(int productId, int batchId, int locationId, decimal delta)
        {
            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var stock = await _context.InventoryStocks.FirstOrDefaultAsync(
                        s => s.ProductId == productId && s.BatchId == batchId && s.LocationId == locationId);
                    if (stock != null)
                    {
                        stock.Quantity += delta;
                        stock.LastUpdated = DateTime.Now;
                    }
                    else
                    {
                        _context.InventoryStocks.Add(new InventoryStock
                        {
                            ProductId = productId,
                            BatchId = batchId,
                            LocationId = locationId,
                            Quantity = Math.Max(0, delta),
                            LastUpdated = DateTime.Now
                        });
                    }
                    await _context.SaveChangesAsync();
                    return;
                }
                catch { }
            }
            var store = await LoadStoreAsync();
            var s2 = store.Stocks.FirstOrDefault(x => x.ProductId == productId && x.BatchId == batchId && x.LocationId == locationId);
            if (s2 != null)
            {
                s2.Quantity = Math.Max(0, s2.Quantity + delta);
                s2.LastUpdated = DateTime.Now;
            }
            else
            {
                int nextId = store.Stocks.Any() ? store.Stocks.Max(x => x.InventoryId) + 1 : 1;
                store.Stocks.Add(new InvStock { InventoryId = nextId, ProductId = productId, BatchId = batchId, LocationId = locationId, Quantity = Math.Max(0, delta) });
            }
            await SaveStoreAsync(store);
        }

        public async Task<(bool Success, string? Error)> ApproveAdjustmentAsync(int adjustmentId, int approverUserId)
        {
            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var adj = await _context.InventoryAdjustments.FindAsync(adjustmentId);
                    if (adj == null || adj.Status != "Pending") return (false, "Adjustment not found or not pending");
                    adj.Status = "Approved"; adj.ApprovedByUserId = approverUserId;
                    await _context.SaveChangesAsync();
                    await UpdateStockQuantityAsync(adj.ProductId, adj.BatchId, adj.LocationId, adj.Quantity);
                    await RecordTransactionAsync(adj.ProductId, adj.BatchId, adj.LocationId, approverUserId, "Adjustment", adj.Quantity, adj.AdjustmentId, adj.Reason);
                    return (true, null);
                }
                catch (Exception ex) { return (false, ex.Message); }
            }
            var store = await LoadStoreAsync();
            var a = store.Adjustments.FirstOrDefault(x => x.AdjustmentId == adjustmentId);
            if (a == null || a.Status != "Pending") return (false, "Adjustment not found or not pending");
            a.Status = "Approved"; a.ApprovedByUserId = approverUserId;
            await SaveStoreAsync(store);
            await UpdateStockQuantityAsync(a.ProductId, a.BatchId, a.LocationId, a.Quantity);
            await RecordTransactionAsync(a.ProductId, a.BatchId, a.LocationId, approverUserId, "Adjustment", a.Quantity, a.AdjustmentId, a.Reason);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> RejectAdjustmentAsync(int adjustmentId, int approverUserId)
        {
            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var adj = await _context.InventoryAdjustments.FindAsync(adjustmentId);
                    if (adj == null || adj.Status != "Pending") return (false, "Not found or not pending");
                    adj.Status = "Rejected"; adj.ApprovedByUserId = approverUserId;
                    await _context.SaveChangesAsync();
                    return (true, null);
                }
                catch (Exception ex) { return (false, ex.Message); }
            }
            var store = await LoadStoreAsync();
            var a = store.Adjustments.FirstOrDefault(x => x.AdjustmentId == adjustmentId);
            if (a == null || a.Status != "Pending") return (false, "Not found or not pending");
            a.Status = "Rejected"; a.ApprovedByUserId = approverUserId;
            await SaveStoreAsync(store);
            return (true, null);
        }

        // ── Transfers ─────────────────────────────────────────────────────────

        public async Task<List<StockTransferListViewModel>> GetTransfersAsync(string? status = null)
        {
            await EnsureDefaultLocationsAsync();
            var result = new List<StockTransferListViewModel>();

            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var query = _context.StockTransfers
                        .Include(t => t.FromLocation).Include(t => t.ToLocation)
                        .Include(t => t.RequestedByUser).Include(t => t.ApprovedByUser)
                        .Include(t => t.StockTransferItems)
                        .Where(t => !t.IsDeleted);
                    if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status);
                    var list = await query.OrderByDescending(t => t.RequestedAt).ToListAsync();
                    foreach (var t in list)
                    {
                        result.Add(new StockTransferListViewModel
                        {
                            TransferId = t.TransferId,
                            FromLocationName = t.FromLocation.LocationName,
                            ToLocationName = t.ToLocation.LocationName,
                            Status = t.Status,
                            RequestedAt = t.RequestedAt,
                            CompletedAt = t.CompletedAt,
                            RequestedByUser = t.RequestedByUser?.FullName ?? $"User#{t.RequestedByUserId}",
                            ApprovedByUser = t.ApprovedByUser?.FullName,
                            ItemCount = t.StockTransferItems.Count,
                            TotalUnits = t.StockTransferItems.Sum(i => i.Quantity)
                        });
                    }
                    return result;
                }
                catch { }
            }

            var store = await LoadStoreAsync();
            var locs = await GetLocationsAsync();
            var transfers = store.Transfers.AsEnumerable().Where(t => !t.IsDeleted);
            if (!string.IsNullOrEmpty(status)) transfers = transfers.Where(t => t.Status == status);
            foreach (var t in transfers.OrderByDescending(x => x.RequestedAt))
            {
                var fromLoc = locs.FirstOrDefault(l => l.LocationId == t.FromLocationId);
                var toLoc = locs.FirstOrDefault(l => l.LocationId == t.ToLocationId);
                var items = store.TransferItems.Where(i => i.TransferId == t.TransferId).ToList();
                result.Add(new StockTransferListViewModel
                {
                    TransferId = t.TransferId,
                    FromLocationName = fromLoc?.LocationName ?? $"Loc#{t.FromLocationId}",
                    ToLocationName = toLoc?.LocationName ?? $"Loc#{t.ToLocationId}",
                    Status = t.Status,
                    RequestedAt = t.RequestedAt,
                    CompletedAt = t.CompletedAt,
                    RequestedByUser = $"User#{t.RequestedByUserId}",
                    ApprovedByUser = t.ApprovedByUserId.HasValue ? $"User#{t.ApprovedByUserId}" : null,
                    ItemCount = items.Count,
                    TotalUnits = items.Sum(i => i.Quantity),
                    Notes = t.Notes
                });
            }
            return result;
        }

        public async Task<StockTransferDetailsViewModel?> GetTransferDetailsAsync(int id)
        {
            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var t = await _context.StockTransfers
                        .Include(x => x.FromLocation).Include(x => x.ToLocation)
                        .Include(x => x.RequestedByUser).Include(x => x.ApprovedByUser)
                        .Include(x => x.StockTransferItems).ThenInclude(i => i.Product)
                        .Include(x => x.StockTransferItems).ThenInclude(i => i.Batch)
                        .FirstOrDefaultAsync(x => x.TransferId == id && !x.IsDeleted);
                    if (t == null) return null;
                    return new StockTransferDetailsViewModel
                    {
                        TransferId = t.TransferId,
                        FromLocationName = t.FromLocation.LocationName,
                        ToLocationName = t.ToLocation.LocationName,
                        Status = t.Status,
                        RequestedAt = t.RequestedAt,
                        CompletedAt = t.CompletedAt,
                        RequestedByUser = t.RequestedByUser?.FullName ?? $"User#{t.RequestedByUserId}",
                        ApprovedByUser = t.ApprovedByUser?.FullName,
                        Items = t.StockTransferItems.Select(i => new StockTransferItemDetailViewModel
                        {
                            TransferItemId = i.TransferItemId,
                            ProductId = i.ProductId,
                            ProductName = i.Product.ProductName,
                            ProductSku = i.Product.Sku,
                            BatchId = i.BatchId,
                            BatchNumber = i.Batch.BatchNumber,
                            ExpirationDate = i.Batch.ExpirationDate,
                            Quantity = i.Quantity
                        }).ToList()
                    };
                }
                catch { }
            }

            var store = await LoadStoreAsync();
            var transfer = store.Transfers.FirstOrDefault(x => x.TransferId == id && !x.IsDeleted);
            if (transfer == null) return null;
            var locs2 = await GetLocationsAsync();
            var prods2 = await GetProductsFromStoreAsync();
            var batches2 = await GetBatchesFromStoreAsync();
            var fromLoc2 = locs2.FirstOrDefault(l => l.LocationId == transfer.FromLocationId);
            var toLoc2 = locs2.FirstOrDefault(l => l.LocationId == transfer.ToLocationId);
            var items2 = store.TransferItems.Where(i => i.TransferId == id).Select(i =>
            {
                var prod = prods2.FirstOrDefault(p => p.ProductId == i.ProductId);
                var batch = batches2.FirstOrDefault(b => b.BatchId == i.BatchId);
                return new StockTransferItemDetailViewModel
                {
                    TransferItemId = i.TransferItemId,
                    ProductId = i.ProductId,
                    ProductName = prod?.ProductName ?? $"Product#{i.ProductId}",
                    ProductSku = prod?.Sku ?? "",
                    BatchId = i.BatchId,
                    BatchNumber = batch?.BatchNumber ?? $"Batch#{i.BatchId}",
                    ExpirationDate = batch?.ExpirationDate,
                    Quantity = i.Quantity
                };
            }).ToList();
            return new StockTransferDetailsViewModel
            {
                TransferId = transfer.TransferId,
                FromLocationName = fromLoc2?.LocationName ?? $"Loc#{transfer.FromLocationId}",
                ToLocationName = toLoc2?.LocationName ?? $"Loc#{transfer.ToLocationId}",
                Status = transfer.Status,
                RequestedAt = transfer.RequestedAt,
                CompletedAt = transfer.CompletedAt,
                RequestedByUser = $"User#{transfer.RequestedByUserId}",
                ApprovedByUser = transfer.ApprovedByUserId.HasValue ? $"User#{transfer.ApprovedByUserId}" : null,
                Notes = transfer.Notes,
                Items = items2
            };
        }

        public async Task<(bool Success, string? Error, StockTransfer? Transfer)> CreateTransferAsync(
            StockTransferFormViewModel model, int requestedByUserId)
        {
            if (model.FromLocationId == model.ToLocationId) return (false, "Source and destination cannot be the same", null);
            if (!model.Items.Any()) return (false, "At least one item is required", null);

            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var transfer = new StockTransfer
                    {
                        FromLocationId = model.FromLocationId,
                        ToLocationId = model.ToLocationId,
                        RequestedByUserId = requestedByUserId,
                        Status = "Requested",
                        RequestedAt = DateTime.Now
                    };
                    _context.StockTransfers.Add(transfer);
                    await _context.SaveChangesAsync();
                    foreach (var item in model.Items.Where(i => i.Quantity > 0))
                    {
                        _context.StockTransferItems.Add(new StockTransferItem
                        {
                            TransferId = transfer.TransferId,
                            ProductId = item.ProductId,
                            BatchId = item.BatchId,
                            Quantity = item.Quantity
                        });
                    }
                    await _context.SaveChangesAsync();
                    return (true, null, transfer);
                }
                catch (Exception ex) { return (false, ex.Message, null); }
            }

            var store = await LoadStoreAsync();
            int nextId = store.Transfers.Any() ? store.Transfers.Max(t => t.TransferId) + 1 : 1;
            store.Transfers.Add(new InvTransfer
            {
                TransferId = nextId,
                FromLocationId = model.FromLocationId,
                ToLocationId = model.ToLocationId,
                RequestedByUserId = requestedByUserId,
                Status = "Requested",
                RequestedAt = DateTime.Now,
                Notes = model.Notes
            });
            int nextItemId = store.TransferItems.Any() ? store.TransferItems.Max(i => i.TransferItemId) + 1 : 1;
            foreach (var item in model.Items.Where(i => i.Quantity > 0))
            {
                store.TransferItems.Add(new InvTransferItem
                {
                    TransferItemId = nextItemId++,
                    TransferId = nextId,
                    ProductId = item.ProductId,
                    BatchId = item.BatchId,
                    Quantity = item.Quantity
                });
            }
            await SaveStoreAsync(store);
            return (true, null, new StockTransfer
            {
                TransferId = nextId,
                FromLocationId = model.FromLocationId,
                ToLocationId = model.ToLocationId,
                RequestedByUserId = requestedByUserId,
                Status = "Requested",
                RequestedAt = DateTime.Now
            });
        }

        private async Task<(bool, string?)> UpdateTransferStatusAsync(int transferId, string newStatus, int userId, bool applyStock = false)
        {
            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var t = await _context.StockTransfers
                        .Include(x => x.StockTransferItems)
                        .FirstOrDefaultAsync(x => x.TransferId == transferId && !x.IsDeleted);
                    if (t == null) return (false, "Transfer not found");
                    t.Status = newStatus;
                    if (newStatus == "Approved") t.ApprovedByUserId = userId;
                    if (newStatus == "Received" || newStatus == "Cancelled") t.CompletedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    if (applyStock && newStatus == "Received")
                    {
                        foreach (var item in t.StockTransferItems)
                        {
                            await UpdateStockQuantityAsync(item.ProductId, item.BatchId, t.FromLocationId, -item.Quantity);
                            await UpdateStockQuantityAsync(item.ProductId, item.BatchId, t.ToLocationId, item.Quantity);
                            await RecordTransactionAsync(item.ProductId, item.BatchId, t.FromLocationId, userId, "TransferOut", -item.Quantity, transferId);
                            await RecordTransactionAsync(item.ProductId, item.BatchId, t.ToLocationId, userId, "TransferIn", item.Quantity, transferId);
                        }
                    }
                    return (true, null);
                }
                catch (Exception ex) { return (false, ex.Message); }
            }

            var store = await LoadStoreAsync();
            var transfer = store.Transfers.FirstOrDefault(x => x.TransferId == transferId && !x.IsDeleted);
            if (transfer == null) return (false, "Transfer not found");
            transfer.Status = newStatus;
            if (newStatus == "Approved") transfer.ApprovedByUserId = userId;
            if (newStatus == "Received" || newStatus == "Cancelled") transfer.CompletedAt = DateTime.Now;
            await SaveStoreAsync(store);
            if (applyStock && newStatus == "Received")
            {
                var items = store.TransferItems.Where(i => i.TransferId == transferId).ToList();
                foreach (var item in items)
                {
                    await UpdateStockQuantityAsync(item.ProductId, item.BatchId, transfer.FromLocationId, -item.Quantity);
                    await UpdateStockQuantityAsync(item.ProductId, item.BatchId, transfer.ToLocationId, item.Quantity);
                    await RecordTransactionAsync(item.ProductId, item.BatchId, transfer.FromLocationId, userId, "TransferOut", -item.Quantity, transferId);
                    await RecordTransactionAsync(item.ProductId, item.BatchId, transfer.ToLocationId, userId, "TransferIn", item.Quantity, transferId);
                }
            }
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> ApproveTransferAsync(int transferId, int approverUserId) =>
            await UpdateTransferStatusAsync(transferId, "Approved", approverUserId);
        public async Task<(bool Success, string? Error)> DispatchTransferAsync(int transferId, int userId) =>
            await UpdateTransferStatusAsync(transferId, "Dispatched", userId);
        public async Task<(bool Success, string? Error)> ReceiveTransferAsync(int transferId, int userId) =>
            await UpdateTransferStatusAsync(transferId, "Received", userId, applyStock: true);
        public async Task<(bool Success, string? Error)> CancelTransferAsync(int transferId, int userId) =>
            await UpdateTransferStatusAsync(transferId, "Cancelled", userId);

        // ── Returns ───────────────────────────────────────────────────────────

        public async Task<List<CustomerReturnListViewModel>> GetReturnsAsync()
        {
            await EnsureDefaultLocationsAsync();
            var result = new List<CustomerReturnListViewModel>();

            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var list = await _context.CustomerReturns
                        .Include(r => r.ProcessedByUser).Include(r => r.CustomerReturnItems)
                        .OrderByDescending(r => r.ReturnDate).ToListAsync();
                    foreach (var r in list)
                    {
                        result.Add(new CustomerReturnListViewModel
                        {
                            ReturnId = r.ReturnId,
                            ReturnDate = r.ReturnDate,
                            SaleId = r.SaleId,
                            Reason = r.Reason,
                            RefundAmount = r.RefundAmount,
                            ProcessedByUser = r.ProcessedByUser?.FullName ?? $"User#{r.ProcessedByUserId}",
                            ItemCount = r.CustomerReturnItems.Count,
                            TotalQuantity = r.CustomerReturnItems.Sum(i => i.Quantity)
                        });
                    }
                    return result;
                }
                catch { }
            }

            var store = await LoadStoreAsync();
            foreach (var r in store.Returns.OrderByDescending(x => x.ReturnDate))
            {
                var items = store.ReturnItems.Where(i => i.ReturnId == r.ReturnId).ToList();
                result.Add(new CustomerReturnListViewModel
                {
                    ReturnId = r.ReturnId,
                    ReturnDate = r.ReturnDate,
                    SaleId = r.SaleId,
                    Reason = r.Reason,
                    RefundAmount = r.RefundAmount,
                    ProcessedByUser = $"User#{r.ProcessedByUserId}",
                    ItemCount = items.Count,
                    TotalQuantity = items.Sum(i => i.Quantity)
                });
            }
            return result;
        }

        public async Task<CustomerReturn?> GetReturnByIdAsync(int id)
        {
            if (await CanUseDatabaseAsync())
            {
                try
                {
                    return await _context.CustomerReturns
                        .Include(r => r.CustomerReturnItems).ThenInclude(i => i.Product)
                        .Include(r => r.CustomerReturnItems).ThenInclude(i => i.Batch)
                        .Include(r => r.ProcessedByUser)
                        .FirstOrDefaultAsync(r => r.ReturnId == id);
                }
                catch { }
            }
            var store = await LoadStoreAsync();
            var r2 = store.Returns.FirstOrDefault(x => x.ReturnId == id);
            if (r2 == null) return null;
            var items2 = store.ReturnItems.Where(i => i.ReturnId == id)
                .Select(i => new CustomerReturnItem { ReturnItemId = i.ReturnItemId, ReturnId = i.ReturnId, ProductId = i.ProductId, BatchId = i.BatchId, Quantity = i.Quantity, RestockCondition = i.RestockCondition })
                .ToList();
            return new CustomerReturn { ReturnId = r2.ReturnId, SaleId = r2.SaleId, ProcessedByUserId = r2.ProcessedByUserId, ReturnDate = r2.ReturnDate, RefundAmount = r2.RefundAmount, Reason = r2.Reason, CustomerReturnItems = items2 };
        }

        public async Task<(bool Success, string? Error, CustomerReturn? Return)> CreateReturnAsync(
            CustomerReturnFormViewModel model, int userId)
        {
            if (!model.Items.Any()) return (false, "At least one return item is required", null);

            if (await CanUseDatabaseAsync())
            {
                try
                {
                    var ret = new CustomerReturn
                    {
                        SaleId = model.SaleId ?? 0,
                        ProcessedByUserId = userId,
                        ReturnDate = model.ReturnDate,
                        RefundAmount = model.RefundAmount,
                        Reason = model.Reason
                    };
                    _context.CustomerReturns.Add(ret);
                    await _context.SaveChangesAsync();
                    foreach (var item in model.Items.Where(i => i.Quantity > 0))
                    {
                        _context.CustomerReturnItems.Add(new CustomerReturnItem
                        {
                            ReturnId = ret.ReturnId,
                            ProductId = item.ProductId,
                            BatchId = item.BatchId,
                            Quantity = item.Quantity,
                            RestockCondition = item.RestockCondition
                        });
                        if (item.RestockCondition == "Resalable")
                        {
                            await UpdateStockQuantityAsync(item.ProductId, item.BatchId, model.LocationId, item.Quantity);
                            await RecordTransactionAsync(item.ProductId, item.BatchId, model.LocationId, userId, "Return", item.Quantity, ret.ReturnId, model.Reason);
                        }
                    }
                    await _context.SaveChangesAsync();
                    return (true, null, ret);
                }
                catch (Exception ex) { return (false, ex.Message, null); }
            }

            var store = await LoadStoreAsync();
            int nextId = store.Returns.Any() ? store.Returns.Max(r => r.ReturnId) + 1 : 1;
            store.Returns.Add(new InvReturn
            {
                ReturnId = nextId,
                SaleId = model.SaleId ?? 0,
                ProcessedByUserId = userId,
                ReturnDate = model.ReturnDate,
                RefundAmount = model.RefundAmount,
                Reason = model.Reason,
                LocationId = model.LocationId,
                Notes = model.Notes
            });
            int nextItemId = store.ReturnItems.Any() ? store.ReturnItems.Max(i => i.ReturnItemId) + 1 : 1;
            foreach (var item in model.Items.Where(i => i.Quantity > 0))
            {
                store.ReturnItems.Add(new InvReturnItem
                {
                    ReturnItemId = nextItemId++,
                    ReturnId = nextId,
                    ProductId = item.ProductId,
                    BatchId = item.BatchId,
                    Quantity = item.Quantity,
                    RestockCondition = item.RestockCondition
                });
                if (item.RestockCondition == "Resalable")
                {
                    await UpdateStockQuantityAsync(item.ProductId, item.BatchId, model.LocationId, item.Quantity);
                    await RecordTransactionAsync(item.ProductId, item.BatchId, model.LocationId, userId, "Return", item.Quantity, nextId, model.Reason);
                }
            }
            await SaveStoreAsync(store);
            return (true, null, new CustomerReturn { ReturnId = nextId, SaleId = model.SaleId ?? 0, ProcessedByUserId = userId, ReturnDate = model.ReturnDate, RefundAmount = model.RefundAmount, Reason = model.Reason });
        }

        // ── Valuation ─────────────────────────────────────────────────────────

        public async Task<StockValuationViewModel> GetStockValuationAsync()
        {
            var allStock = await GetStockByLocationAsync();
            var vm = new StockValuationViewModel { GeneratedAt = DateTime.Now };
            var grouped = allStock.GroupBy(s => new { s.LocationId, s.LocationName, s.LocationType });
            foreach (var grp in grouped.OrderBy(g => g.Key.LocationName))
            {
                var locVm = new StockValuationLocationViewModel { LocationName = grp.Key.LocationName, LocationType = grp.Key.LocationType };
                foreach (var s in grp.OrderBy(x => x.ProductName))
                {
                    locVm.Lines.Add(new StockValuationLineViewModel
                    {
                        ProductName = s.ProductName,
                        Sku = s.ProductSku,
                        BatchNumber = s.BatchNumber,
                        Quantity = s.Quantity,
                        UnitCost = s.UnitCost,
                        TotalValue = s.InventoryValue
                    });
                }
                locVm.Subtotal = locVm.Lines.Sum(l => l.TotalValue);
                vm.Locations.Add(locVm);
            }
            vm.GrandTotal = vm.Locations.Sum(l => l.Subtotal);
            vm.EstimatedRetailValue = allStock.Sum(s => s.Quantity * s.UnitCost * 1.3m);
            vm.EstimatedMargin = vm.EstimatedRetailValue - vm.GrandTotal;
            return vm;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        public async Task<List<ProductBatch>> GetBatchesForProductAsync(int productId)
        {
            if (await CanUseDatabaseAsync())
            {
                try
                {
                    return await _context.ProductBatches
                        .Where(b => b.ProductId == productId && b.IsActive)
                        .OrderBy(b => b.ExpirationDate).ToListAsync();
                }
                catch { }
            }
            var batches = await GetBatchesFromStoreAsync();
            return batches.Where(b => b.ProductId == productId && b.IsActive)
                .Select(b => new ProductBatch
                {
                    BatchId = b.BatchId,
                    ProductId = b.ProductId,
                    BatchNumber = b.BatchNumber,
                    ManufactureDate = b.ManufactureDate,
                    ExpirationDate = b.ExpirationDate,
                    UnitCost = b.UnitCost,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt
                }).ToList();
        }

        public async Task<List<ProductListViewModel>> GetActiveProductsForDropdownAsync()
        {
            if (await CanUseDatabaseAsync())
            {
                try
                {
                    return await _context.Products
                        //.Where(p => p.IsActive && !p.IsDeleted)
                        .Where(p => p.IsActive == true && p.IsDeleted == false)
                        .Select(p => new ProductListViewModel { ProductId = p.ProductId, ProductName = p.ProductName, Sku = p.Sku })
                        .OrderBy(p => p.ProductName).ToListAsync();
                }
                catch { }
            }
            return (await GetProductsFromStoreAsync()).Where(p => p.IsActive).OrderBy(p => p.ProductName).ToList();
        }

        Reads products from ProductService's JSON store
        private async Task<List<ProductListViewModel>> GetProductsFromStoreAsync()
        {
            try
            {
                string path = Path.Combine(_env.ContentRootPath, "App_Data", "product_module_data.json");
                if (!File.Exists(path)) return GetFallbackProducts();
                var json = await File.ReadAllTextAsync(path);
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("Products", out var prods)) return GetFallbackProducts();
                var result = new List<ProductListViewModel>();
                foreach (var p in prods.EnumerateArray())
                {
                    bool isDeleted = p.TryGetProperty("IsDeleted", out var d) && d.GetBoolean();
                    if (isDeleted) continue;
                    bool isActive = !p.TryGetProperty("IsActive", out var a) || a.GetBoolean();
                    int prodId = p.TryGetProperty("ProductId", out var id) ? id.GetInt32() : 0;
                    string name = p.TryGetProperty("ProductName", out var n) ? n.GetString() ?? "" : "";
                    string sku = p.TryGetProperty("Sku", out var s) ? s.GetString() ?? "" : "";
                    int catId = p.TryGetProperty("CategoryId", out var ci) ? ci.GetInt32() : 0;
                    decimal cost = p.TryGetProperty("PurchaseCost", out var pc) ? pc.GetDecimal() : 0;
                    decimal price = p.TryGetProperty("SellingPrice", out var sp) ? sp.GetDecimal() : 0;
                    int minStock = p.TryGetProperty("MinStockLevel", out var ms) ? ms.GetInt32() : 0;
                    int reorder = p.TryGetProperty("ReorderPoint", out var rp) ? rp.GetInt32() : 0;
                    int maxStock = p.TryGetProperty("MaxStockLevel", out var mx) ? mx.GetInt32() : 0;
                    result.Add(new ProductListViewModel
                    {
                        ProductId = prodId,
                        ProductName = name,
                        Sku = sku,
                        CategoryId = catId,
                        PurchaseCost = cost,
                        SellingPrice = price,
                        MinStockLevel = minStock,
                        //ReorderPoint=reorder, MaxStockLevel=maxStock, IsActive=isActive
                    });
                }
                return result;
            }
            catch { return GetFallbackProducts(); }
        }

        private List<ProductListViewModel> GetFallbackProducts() => new()
        {
            new() { ProductId=1, ProductName="Coca-Cola 330ml", Sku="SKU-001", IsActive=true, PurchaseCost=0.80m, SellingPrice=1.20m, MinStockLevel=50, ReorderPoint=100, MaxStockLevel=500 },
            new() { ProductId=2, ProductName="Pepsi 330ml", Sku="SKU-002", IsActive=true, PurchaseCost=0.75m, SellingPrice=1.15m, MinStockLevel=50, ReorderPoint=100, MaxStockLevel=500 },
            new() { ProductId=3, ProductName="Lays Classic Chips", Sku="SKU-003", IsActive=true, PurchaseCost=1.50m, SellingPrice=2.00m, MinStockLevel=30, ReorderPoint=60, MaxStockLevel=300 },
            new() { ProductId=4, ProductName="Mineral Water 1.5L", Sku="SKU-004", IsActive=true, PurchaseCost=0.40m, SellingPrice=0.75m, MinStockLevel=100, ReorderPoint=200, MaxStockLevel=1000 },
            new() { ProductId=5, ProductName="Oreo Cookies", Sku="SKU-005", IsActive=true, PurchaseCost=1.20m, SellingPrice=1.80m, MinStockLevel=40, ReorderPoint=80, MaxStockLevel=400 },
        };

        private async Task<List<BatchStoreItem>> GetBatchesFromStoreAsync()
        {
            try
            {
                string path = Path.Combine(_env.ContentRootPath, "App_Data", "product_module_data.json");
                if (!File.Exists(path)) return GetFallbackBatches();
                var json = await File.ReadAllTextAsync(path);
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("Batches", out var batches)) return GetFallbackBatches();
                var result = new List<BatchStoreItem>();
                foreach (var b in batches.EnumerateArray())
                {
                    int batchId = b.TryGetProperty("BatchId", out var bi) ? bi.GetInt32() : 0;
                    int prodId = b.TryGetProperty("ProductId", out var pi) ? pi.GetInt32() : 0;
                    string num = b.TryGetProperty("BatchNumber", out var bn) ? bn.GetString() ?? "" : "";
                    decimal cost = b.TryGetProperty("UnitCost", out var uc) ? uc.GetDecimal() : 0;
                    bool isActive = !b.TryGetProperty("IsActive", out var ia) || ia.GetBoolean();
                    DateOnly? expDate = null;
                    if (b.TryGetProperty("ExpirationDate", out var ed) && ed.ValueKind != JsonValueKind.Null)
                        if (DateOnly.TryParse(ed.GetString(), out var dv)) expDate = dv;
                    DateOnly? mfgDate = null;
                    if (b.TryGetProperty("ManufactureDate", out var md) && md.ValueKind != JsonValueKind.Null)
                        if (DateOnly.TryParse(md.GetString(), out var dv)) mfgDate = dv;
                    result.Add(new BatchStoreItem { BatchId = batchId, ProductId = prodId, BatchNumber = num, ExpirationDate = expDate, ManufactureDate = mfgDate, UnitCost = cost, IsActive = isActive });
                }
                return result;
            }
            catch { return GetFallbackBatches(); }
        }

        private List<BatchStoreItem> GetFallbackBatches() => new()
        {
            new() { BatchId=1, ProductId=1, BatchNumber="BATCH-2024-001", ExpirationDate=DateOnly.FromDateTime(DateTime.Today.AddMonths(6)), UnitCost=0.80m, IsActive=true },
            new() { BatchId=2, ProductId=2, BatchNumber="BATCH-2024-002", ExpirationDate=DateOnly.FromDateTime(DateTime.Today.AddMonths(4)), UnitCost=0.75m, IsActive=true },
            new() { BatchId=3, ProductId=3, BatchNumber="BATCH-2024-003", ExpirationDate=DateOnly.FromDateTime(DateTime.Today.AddDays(15)), UnitCost=1.50m, IsActive=true },
            new() { BatchId=4, ProductId=4, BatchNumber="BATCH-2024-004", ExpirationDate=DateOnly.FromDateTime(DateTime.Today.AddMonths(12)), UnitCost=0.40m, IsActive=true },
            new() { BatchId=5, ProductId=5, BatchNumber="BATCH-2024-005", ExpirationDate=DateOnly.FromDateTime(DateTime.Today.AddMonths(8)), UnitCost=1.20m, IsActive=true },
        };
    }
}

