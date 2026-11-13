//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text.Json;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;
//using SuperMarket.Models;
//using SuperMarket.Models.ViewModels;

//namespace SuperMarket.Services
//{
//    public class ProductService : IProductService
//    {
//        private readonly DBContext _context;
//        private readonly IWebHostEnvironment _env;
//        //private readonly string _storeFilePath;
//        private static readonly SemaphoreSlim _fileLock = new(1, 1);

//        public ProductService(DBContext context, IWebHostEnvironment env)
//        {
//            _context = context;
//            _env = env;
//            string appDataDir = Path.Combine(_env.ContentRootPath, "App_Data");
//            if (!Directory.Exists(appDataDir))
//            {
//                Directory.CreateDirectory(appDataDir);
//            }
//            //_storeFilePath = Path.Combine(appDataDir, "product_module_data.json");
//        }

//        private async Task<bool> CanUseDatabaseAsync()
//        {
//            try
//            {
//                if (_context.Database.CanConnect())
//                {
//                    _context.Products.Take(1).ToList();
//                    return true;
//                }
//            }
//            catch
//            {
//            }
//            return false;
//        }

//        #region Products

//        public async Task<ProductIndexFilterViewModel> GetProductsAsync(int? categoryId = null, string? search = null, string? stockFilter = null)
//        {
//            await EnsureDefaultDataAsync();

//            var productsList = new List<ProductListViewModel>();
//            var categories = await GetCategoriesAsync();

//            if (await CanUseDatabaseAsync())
//            {
//                try
//                {
//                    var query = _context.Products
//                        .Where(p => !p.IsDeleted)
//                        .Include(p => p.Category)
//                        .Include(p => p.ProductBatches)
//                            .ThenInclude(b => b.InventoryStocks)
//                        .AsQueryable();

//                    if (categoryId.HasValue && categoryId.Value > 0)
//                    {
//                        query = query.Where(p => p.CategoryId == categoryId.Value);
//                    }

//                    if (!string.IsNullOrWhiteSpace(search))
//                    {
//                        var s = search.Trim().ToLower();
//                        query = query.Where(p =>
//                            p.ProductName.ToLower().Contains(s) ||
//                            p.Sku.ToLower().Contains(s) ||
//                            p.Barcode.ToLower().Contains(s) ||
//                            p.Category.CategoryName.ToLower().Contains(s));
//                    }

//                    var dbProducts = await query.ToListAsync();
//                    var descMap = await LoadDescriptionMapAsync();
//                    var today = DateOnly.FromDateTime(DateTime.Today);

//                    foreach (var p in dbProducts)
//                    {
//                        var totalStock = p.ProductBatches
//                            .SelectMany(b => b.InventoryStocks)
//                            .Sum(s => s.Quantity);

//                        var activeBatches = p.ProductBatches.Where(b => b.IsActive).ToList();
//                        bool hasExpiring = activeBatches.Any(b => b.ExpirationDate.HasValue && b.ExpirationDate.Value > today && b.ExpirationDate.Value <= today.AddDays(30));
//                        bool hasExpired = activeBatches.Any(b => b.ExpirationDate.HasValue && b.ExpirationDate.Value < today);

//                        string? desc = descMap.TryGetValue(p.ProductId, out var d) ? d : p.Description;

//                        productsList.Add(new ProductListViewModel
//                        {
//                            ProductId = p.ProductId,
//                            Sku = p.Sku,
//                            Barcode = p.Barcode,
//                            ProductName = p.ProductName,
//                            Description = desc,
//                            CategoryId = p.CategoryId,
//                            CategoryName = p.Category?.CategoryName ?? "General",
//                            UnitOfMeasure = p.UnitOfMeasure,
//                            PurchaseCost = p.PurchaseCost,
//                            SellingPrice = p.SellingPrice,
//                            MinStockLevel = p.MinStockLevel,
//                            TotalStock = totalStock,
//                            ActiveBatchCount = activeBatches.Count,
//                            IsActive = p.IsActive ?? true,
//                            HasExpiringBatches = hasExpiring,
//                            HasExpiredBatches = hasExpired
//                        });
//                    }
//                }
//                catch
//                {
//                    productsList = await LoadProductsFromStoreAsync(categoryId, search);
//                }
//            }
//            else
//            {
//                productsList = await LoadProductsFromStoreAsync(categoryId, search);
//            }

//            // Apply stock level filter
//            if (!string.IsNullOrWhiteSpace(stockFilter) && stockFilter.ToLower() != "all")
//            {
//                productsList = stockFilter.ToLower() switch
//                {
//                    "lowstock" => productsList.Where(p => p.TotalStock > 0 && p.TotalStock <= p.MinStockLevel).ToList(),
//                    "outofstock" => productsList.Where(p => p.TotalStock <= 0).ToList(),
//                    "instock" => productsList.Where(p => p.TotalStock > p.MinStockLevel).ToList(),
//                    _ => productsList
//                };
//            }

//            var categoryItems = categories.Select(c => new SelectListItem
//            {
//                Value = c.CategoryId.ToString(),
//                Text = c.CategoryName,
//                Selected = categoryId.HasValue && categoryId.Value == c.CategoryId
//            }).ToList();

//            var totalVal = productsList.Sum(p => p.TotalStock * p.PurchaseCost);

//            return new ProductIndexFilterViewModel
//            {
//                Products = productsList.OrderBy(p => p.ProductName).ToList(),
//                Categories = categoryItems,
//                SelectedCategoryId = categoryId,
//                SearchTerm = search,
//                StockFilter = stockFilter ?? "all",
//                TotalProducts = productsList.Count,
//                InStockCount = productsList.Count(p => p.TotalStock > p.MinStockLevel),
//                LowStockCount = productsList.Count(p => p.TotalStock > 0 && p.TotalStock <= p.MinStockLevel),
//                OutOfStockCount = productsList.Count(p => p.TotalStock <= 0),
//                TotalInventoryValue = Math.Round(totalVal, 2)
//            };
//        }

//        public async Task<Product?> GetProductByIdAsync(int id)
//        {
//            if (await CanUseDatabaseAsync())
//            {
//                try
//                {
//                    var p = await _context.Products
//                        .Include(x => x.Category)
//                        .Include(x => x.ProductBatches)
//                            .ThenInclude(b => b.InventoryStocks)
//                        .FirstOrDefaultAsync(x => x.ProductId == id && !x.IsDeleted);

//                    if (p != null)
//                    {
//                        var descMap = await LoadDescriptionMapAsync();
//                        if (descMap.TryGetValue(p.ProductId, out var desc))
//                        {
//                            p.Description = desc;
//                        }
//                        return p;
//                    }
//                }
//                catch
//                {
//                }
//            }

//            var store = await LoadStoreAsync();
//            return store.Products.FirstOrDefault(p => p.ProductId == id && !p.IsDeleted);
//        }

//        public async Task<ProductDetailsViewModel?> GetProductDetailsAsync(int id)
//        {
//            var product = await GetProductByIdAsync(id);
//            if (product == null) return null;

//            var batches = await GetBatchesAsync(id);
//            var locations = await GetLocationsAsync();
//            var locDict = locations.ToDictionary(l => l.LocationId, l => l.LocationName);

//            var stockByLocation = new Dictionary<string, decimal>();

//            if (await CanUseDatabaseAsync())
//            {
//                try
//                {
//                    var stocks = await _context.InventoryStocks
//                        .Where(s => s.ProductId == id)
//                        .Include(s => s.Location)
//                        .ToListAsync();

//                    foreach (var s in stocks)
//                    {
//                        string locName = s.Location?.LocationName ?? "Store Floor";
//                        if (!stockByLocation.ContainsKey(locName)) stockByLocation[locName] = 0;
//                        stockByLocation[locName] += s.Quantity;
//                    }
//                }
//                catch
//                {
//                }
//            }

//            if (!stockByLocation.Any())
//            {
//                stockByLocation["Main Store Floor"] = batches.Sum(b => b.TotalQuantity);
//            }

//            var totalStock = batches.Sum(b => b.TotalQuantity);

//            return new ProductDetailsViewModel
//            {
//                Product = product,
//                CategoryName = product.Category?.CategoryName ?? "General",
//                TotalStock = totalStock,
//                StockStatus = totalStock <= 0 ? "Out of Stock" : (totalStock <= product.MinStockLevel ? "Low Stock" : "In Stock"),
//                Batches = batches.OrderBy(b => b.ExpirationDate).ToList(),
//                StockByLocation = stockByLocation
//            };
//        }

//        public async Task<(bool Success, string? ErrorMessage, Product? Product)> CreateProductAsync(ProductFormViewModel model)
//        {
//            // Validate unique SKU
//            if (!await IsSkuUniqueAsync(model.Sku))
//            {
//                return (false, $"Product Code (SKU) '{model.Sku}' is already registered.", null);
//            }

//            // Validate unique Barcode
//            if (!await IsBarcodeUniqueAsync(model.Barcode))
//            {
//                return (false, $"Barcode '{model.Barcode}' is already registered to another product.", null);
//            }

//            var product = new Product
//            {
//                Sku = model.Sku.Trim().ToUpper(),
//                Barcode = model.Barcode.Trim(),
//                ProductName = model.ProductName.Trim(),
//                CategoryId = model.CategoryId,
//                UnitOfMeasure = model.UnitOfMeasure.Trim().ToUpper(),
//                PurchaseCost = model.PurchaseCost,
//                SellingPrice = model.SellingPrice,
//                MinStockLevel = model.MinStockLevel,
//                IsActive = model.IsActive,
//                CreatedAt = DateTime.UtcNow,
//                IsDeleted = false
//            };

//            if (await CanUseDatabaseAsync())
//            {
//                try
//                {
//                    _context.Products.Add(product);
//                    await _context.SaveChangesAsync();

//                    if (!string.IsNullOrWhiteSpace(model.Description))
//                    {
//                        await SaveDescriptionAsync(product.ProductId, model.Description.Trim());
//                    }

//                    // Handle initial batch if requested
//                    if (model.CreateInitialBatch && !string.IsNullOrWhiteSpace(model.InitialBatchNumber) && (model.InitialQuantity ?? 0) > 0)
//                    {
//                        int locId = model.InitialLocationId ?? (await GetLocationsAsync()).FirstOrDefault()?.LocationId ?? 1;

//                        var batch = new ProductBatch
//                        {
//                            ProductId = product.ProductId,
//                            BatchNumber = model.InitialBatchNumber.Trim().ToUpper(),
//                            ManufactureDate = model.InitialManufactureDate,
//                            ExpirationDate = model.InitialExpirationDate,
//                            UnitCost = model.PurchaseCost,
//                            IsActive = true,
//                            CreatedAt = DateTime.UtcNow
//                        };

//                        _context.ProductBatches.Add(batch);
//                        await _context.SaveChangesAsync();

//                        var stock = new InventoryStock
//                        {
//                            ProductId = product.ProductId,
//                            BatchId = batch.BatchId,
//                            LocationId = locId,
//                            Quantity = model.InitialQuantity ?? 0,
//                            LastUpdated = DateTime.UtcNow
//                        };

//                        _context.InventoryStocks.Add(stock);
//                        await _context.SaveChangesAsync();
//                    }

//                    return (true, null, product);
//                }
//                catch
//                {
//                    // fallback to store
//                    return await CreateProductInStoreAsync(model);
//                }
//            }

//            return await CreateProductInStoreAsync(model);
//        }

//        public async Task<(bool Success, string? ErrorMessage)> UpdateProductAsync(ProductFormViewModel model)
//        {
//            if (!await IsSkuUniqueAsync(model.Sku, model.ProductId))
//            {
//                return (false, $"Product Code (SKU) '{model.Sku}' is already registered.");
//            }

//            if (!await IsBarcodeUniqueAsync(model.Barcode, model.ProductId))
//            {
//                return (false, $"Barcode '{model.Barcode}' is already registered to another product.");
//            }

//            if (await CanUseDatabaseAsync())
//            {
//                try
//                {
//                    var product = await _context.Products.FindAsync(model.ProductId);
//                    if (product == null || product.IsDeleted) return (false, "Product not found.");

//                    product.Sku = model.Sku.Trim().ToUpper();
//                    product.Barcode = model.Barcode.Trim();
//                    product.ProductName = model.ProductName.Trim();
//                    product.CategoryId = model.CategoryId;
//                    product.UnitOfMeasure = model.UnitOfMeasure.Trim().ToUpper();
//                    product.PurchaseCost = model.PurchaseCost;
//                    product.SellingPrice = model.SellingPrice;
//                    product.MinStockLevel = model.MinStockLevel;
//                    product.IsActive = model.IsActive;
//                    product.UpdatedAt = DateTime.UtcNow;

//                    await _context.SaveChangesAsync();

//                    if (model.Description != null)
//                    {
//                        await SaveDescriptionAsync(product.ProductId, model.Description.Trim());
//                    }

//                    return (true, null);
//                }
//                catch
//                {
//                }
//            }

//            // Update in store
//            var store = await LoadStoreAsync();
//            var item = store.Products.FirstOrDefault(p => p.ProductId == model.ProductId);
//            if (item != null)
//            {
//                item.Sku = model.Sku.Trim().ToUpper();
//                item.Barcode = model.Barcode.Trim();
//                item.ProductName = model.ProductName.Trim();
//                item.CategoryId = model.CategoryId;
//                item.UnitOfMeasure = model.UnitOfMeasure.Trim().ToUpper();
//                item.PurchaseCost = model.PurchaseCost;
//                item.SellingPrice = model.SellingPrice;
//                item.MinStockLevel = model.MinStockLevel;
//                item.IsActive = model.IsActive;
//                item.UpdatedAt = DateTime.UtcNow;

//                var descMap = await LoadDescriptionMapAsync();
//                descMap[model.ProductId] = model.Description ?? string.Empty;
//                await SaveDescriptionMapAsync(descMap);

//                await SaveStoreAsync(store);
//                return (true, null);
//            }

//            return (false, "Product not found.");
//        }

//        public async Task<bool> DeleteProductAsync(int id)
//        {
//            if (await CanUseDatabaseAsync())
//            {
//                try
//                {
//                    var product = await _context.Products.FindAsync(id);
//                    if (product != null)
//                    {
//                        product.IsDeleted = true;
//                        product.IsActive = false;
//                        product.UpdatedAt = DateTime.UtcNow;
//                        await _context.SaveChangesAsync();
//                        return true;
//                    }
//                }
//                catch
//                {
//                }
//            }

//            var store = await LoadStoreAsync();
//            var item = store.Products.FirstOrDefault(p => p.ProductId == id);
//            if (item != null)
//            {
//                item.IsDeleted = true;
//                item.IsActive = false;
//                item.UpdatedAt = DateTime.UtcNow;
//                await SaveStoreAsync(store);
//                return true;
//            }

//            return false;
//        }

//        public async Task<bool> IsSkuUniqueAsync(string sku, int? currentProductId = null)
//        {
//            if (string.IsNullOrWhiteSpace(sku)) return false;
//            string cleanSku = sku.Trim().ToUpper();

//            if (await CanUseDatabaseAsync())
//            {
//                try
//                {
//                    bool exists = await _context.Products
//                        .AnyAsync(p => p.Sku.ToUpper() == cleanSku && !p.IsDeleted && (!currentProductId.HasValue || p.ProductId != currentProductId.Value));
//                    return !exists;
//                }
//                catch
//                {
//                }
//            }

//            var store = await LoadStoreAsync();
//            bool storeExists = store.Products
//                .Any(p => p.Sku.ToUpper() == cleanSku && !p.IsDeleted && (!currentProductId.HasValue || p.ProductId != currentProductId.Value));
//            return !storeExists;
//        }

//        public async Task<bool> IsBarcodeUniqueAsync(string barcode, int? currentProductId = null)
//        {
//            if (string.IsNullOrWhiteSpace(barcode)) return false;
//            string cleanBarcode = barcode.Trim();

//            if (await CanUseDatabaseAsync())
//            {
//                try
//                {
//                    bool exists = await _context.Products
//                        .AnyAsync(p => p.Barcode == cleanBarcode && !p.IsDeleted && (!currentProductId.HasValue || p.ProductId != currentProductId.Value));
//                    return !exists;
//                }
//                catch
//                {
//                }
//            }

//            var store = await LoadStoreAsync();
//            bool storeExists = store.Products
//                .Any(p => p.Barcode == cleanBarcode && !p.IsDeleted && (!currentProductId.HasValue || p.ProductId != currentProductId.Value));
//            return !storeExists;
//        }

//        public string GenerateUniqueSku()
//        {
//            var random = new Random();
//            return $"PRD-{DateTime.UtcNow:yyMMdd}-{random.Next(100, 999)}";
//        }

//        public string GenerateUniqueBarcode()
//        {
//            var random = new Random();
//            long prefix = 890000000000L;
//            long val = prefix + (long)random.Next(100000, 999999);
//            return val.ToString();
//        }

//        #endregion

//        #region Categories

//        public async Task<List<CategoryFormViewModel>> GetCategoriesAsync()
//        {
//            await EnsureDefaultDataAsync();

//            if (await CanUseDatabaseAsync())
//            {
//                try
//                {
//                    var list = await _context.Categories
//                        .Where(c => !c.IsDeleted)
//                        .Select(c => new CategoryFormViewModel
//                        {
//                            CategoryId = c.CategoryId,
//                            CategoryName = c.CategoryName,
//                            Description = c.Description,
//                            IsActive = c.IsActive,
//                            ProductCount = c.Products.Count(p => !p.IsDeleted)
//                        })
//                        .OrderBy(c => c.CategoryName)
//                        .ToListAsync();

//                    if (list.Any()) return list;
//                }
//                catch
//                {
//                }
//            }

//            var store = await LoadStoreAsync();
//            return store.Categories.Where(c => !c.IsDeleted).Select(c => new CategoryFormViewModel
//            {
//                CategoryId = c.CategoryId,
//                CategoryName = c.CategoryName,
//                Description = c.Description,
//                IsActive = c.IsActive,
//                ProductCount = store.Products.Count(p => p.CategoryId == c.CategoryId && !p.IsDeleted)
//            }).OrderBy(c => c.CategoryName).ToList();
//        }

//        public async Task<Category?> GetCategoryByIdAsync(int id)
//        {
//            if (await CanUseDatabaseAsync())
//            {
//                try
//                {
//                    return await _context.Categories
//                        .Include(c => c.Products)
//                        .FirstOrDefaultAsync(c => c.CategoryId == id && !c.IsDeleted);
//                }
//                catch
//                {
//                }
//            }

//            var store = await LoadStoreAsync();
//            return store.Categories.FirstOrDefault(c => c.CategoryId == id && !c.IsDeleted);
//        }

//        public async Task<(bool Success, string? ErrorMessage, Category? Category)> CreateCategoryAsync(CategoryFormViewModel model)
//        {
//            string catName = model.CategoryName.Trim();

//            if (await CanUseDatabaseAsync())
//            {
//                try
//                {
//                    bool exists = await _context.Categories
//                        .AnyAsync(c => c.CategoryName.ToLower() == catName.ToLower() && !c.IsDeleted);
//                    if (exists) return (false, $"Category '{catName}' already exists.", null);

//                    var cat = new Category
//                    {
//                        CategoryName = catName,
//                        Description = model.Description?.Trim(),
//                        IsActive = model.IsActive,
//                        CreatedAt = DateTime.UtcNow,
//                        IsDeleted = false
//                    };

//                    _context.Categories.Add(cat);
//                    await _context.SaveChangesAsync();
//                    return (true, null, cat);
//                }
//                catch
//                {
//                }
//            }

//            var store = await LoadStoreAsync();
//            if (store.Categories.Any(c => c.CategoryName.Equals(catName, StringComparison.OrdinalIgnoreCase) && !c.IsDeleted))
//            {
//                return (false, $"Category '{catName}' already exists.", null);
//            }

//            int nextId = store.Categories.Any() ? store.Categories.Max(c => c.CategoryId) + 1 : 1;
//            var newCat = new Category
//            {
//                CategoryId = nextId,
//                CategoryName = catName,
//                Description = model.Description?.Trim(),
//                IsActive = model.IsActive,
//                CreatedAt = DateTime.UtcNow,
//                IsDeleted = false
//            };
//            store.Categories.Add(newCat);
//            await SaveStoreAsync(store);
//            return (true, null, newCat);
//        }

//        public async Task<(bool Success, string? ErrorMessage)> UpdateCategoryAsync(CategoryFormViewModel model)
//        {
//            string catName = model.CategoryName.Trim();

//            if (await CanUseDatabaseAsync())
//            {
//                try
//                {
//                    bool exists = await _context.Categories
//                        .AnyAsync(c => c.CategoryName.ToLower() == catName.ToLower() && c.CategoryId != model.CategoryId && !c.IsDeleted);
//                    if (exists) return (false, $"Another category named '{catName}' already exists.");

//                    var cat = await _context.Categories.FindAsync(model.CategoryId);
//                    if (cat == null || cat.IsDeleted) return (false, "Category not found.");

//                    cat.CategoryName = catName;
//                    cat.Description = model.Description?.Trim();
//                    cat.IsActive = model.IsActive;
//                    cat.UpdatedAt = DateTime.UtcNow;

//                    await _context.SaveChangesAsync();
//                    return (true, null);
//                }
//                catch
//                {
//                }
//            }

//            var store = await LoadStoreAsync();
//            var target = store.Categories.FirstOrDefault(c => c.CategoryId == model.CategoryId && !c.IsDeleted);
//            if (target == null) return (false, "Category not found.");

//            target.CategoryName = catName;
//            target.Description = model.Description?.Trim();
//            target.IsActive = model.IsActive;
//            target.UpdatedAt = DateTime.UtcNow;
//            await SaveStoreAsync(store);
//            return (true, null);
//        }

//        public async Task<bool> DeleteCategoryAsync(int id)
//        {
//            if (await CanUseDatabaseAsync())
//            {
//                try
//                {
//                    var cat = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.CategoryId == id);
//                    if (cat != null)
//                    {
//                        cat.IsDeleted = true;
//                        cat.IsActive = false;
//                        cat.UpdatedAt = DateTime.UtcNow;
//                        await _context.SaveChangesAsync();
//                        return true;
//                    }
//                }
//                catch
//                {
//                }
//            }

//            var store = await LoadStoreAsync();
//            var cItem = store.Categories.FirstOrDefault(c => c.CategoryId == id);
//            if (cItem != null)
//            {
//                cItem.IsDeleted = true;
//                cItem.IsActive = false;
//                cItem.UpdatedAt = DateTime.UtcNow;
//                await SaveStoreAsync(store);
//                return true;
//            }

//            return false;
//        }

//        #endregion

//        #region Batches

//        public async Task<List<BatchItemViewModel>> GetBatchesAsync(int? productId = null)
//        {
//            await EnsureDefaultDataAsync();

//            var list = new List<BatchItemViewModel>();

//            if (await CanUseDatabaseAsync())
//            {
//                try
//                {
//                    var query = _context.ProductBatches
//                        .Include(b => b.Product)
//                        .Include(b => b.InventoryStocks)
//                        .AsQueryable();

//                    if (productId.HasValue && productId.Value > 0)
//                    {
//                        query = query.Where(b => b.ProductId == productId.Value);
//                    }

//                    var dbBatches = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
//                    foreach (var b in dbBatches)
//                    {
//                        decimal qty = b.InventoryStocks?.Sum(s => s.Quantity) ?? 0;
//                        list.Add(new BatchItemViewModel
//                        {
//                            BatchId = b.BatchId,
//                            ProductId = b.ProductId,
//                            ProductName = b.Product?.ProductName ?? "Product #" + b.ProductId,
//                            ProductSku = b.Product?.Sku ?? "",
//                            BatchNumber = b.BatchNumber,
//                            ManufactureDate = b.ManufactureDate,
//                            ExpirationDate = b.ExpirationDate,
//                            UnitCost = b.UnitCost,
//                            TotalQuantity = qty,
//                            IsActive = b.IsActive,
//                            CreatedAt = b.CreatedAt
//                        });
//                    }

//                    if (list.Any()) return list;
//                }
//                catch
//                {
//                }
//            }

//            var store = await LoadStoreAsync();
//            var storeBatches = productId.HasValue && productId.Value > 0
//                ? store.Batches.Where(b => b.ProductId == productId.Value).ToList()
//                : store.Batches;

//            foreach (var b in storeBatches)
//            {
//                var prod = store.Products.FirstOrDefault(p => p.ProductId == b.ProductId);
//                decimal qty = store.Stocks.Where(s => s.BatchId == b.BatchId).Sum(s => s.Quantity);

//                list.Add(new BatchItemViewModel
//                {
//                    BatchId = b.BatchId,
//                    ProductId = b.ProductId,
//                    ProductName = prod?.ProductName ?? "Product #" + b.ProductId,
//                    ProductSku = prod?.Sku ?? "",
//                    BatchNumber = b.BatchNumber,
//                    ManufactureDate = b.ManufactureDate,
//                    ExpirationDate = b.ExpirationDate,
//                    UnitCost = b.UnitCost,
//                    TotalQuantity = qty,
//                    IsActive = b.IsActive,
//                    CreatedAt = b.CreatedAt
//                });
//            }

//            return list;
//        }

//        public async Task<BatchItemViewModel?> GetBatchByIdAsync(int id)
//        {
//            var batches = await GetBatchesAsync();
//            return batches.FirstOrDefault(b => b.BatchId == id);
//        }

//        public async Task<(bool Success, string? ErrorMessage, ProductBatch? Batch)> CreateBatchAsync(BatchFormViewModel model)
//        {
//            string batchNum = model.BatchNumber.Trim().ToUpper();

//            if (await CanUseDatabaseAsync())
//            {
//                try
//                {
//                    bool exists = await _context.ProductBatches
//                        .AnyAsync(b => b.ProductId == model.ProductId && b.BatchNumber.ToUpper() == batchNum);
//                    if (exists) return (false, $"Batch Number '{batchNum}' already exists for this product.", null);

//                    var batch = new ProductBatch
//                    {
//                        ProductId = model.ProductId,
//                        BatchNumber = batchNum,
//                        ManufactureDate = model.ManufactureDate,
//                        ExpirationDate = model.ExpirationDate,
//                        UnitCost = model.UnitCost,
//                        IsActive = model.IsActive,
//                        CreatedAt = DateTime.UtcNow
//                    };

//                    _context.ProductBatches.Add(batch);
//                    await _context.SaveChangesAsync();

//                    if (model.Quantity > 0)
//                    {
//                        int locId = model.LocationId ?? (await GetLocationsAsync()).FirstOrDefault()?.LocationId ?? 1;

//                        var stock = new InventoryStock
//                        {
//                            ProductId = model.ProductId,
//                            BatchId = batch.BatchId,
//                            LocationId = locId,
//                            Quantity = model.Quantity,
//                            LastUpdated = DateTime.UtcNow
//                        };

//                        _context.InventoryStocks.Add(stock);
//                        await _context.SaveChangesAsync();
//                    }

//                    return (true, null, batch);
//                }
//                catch
//                {
//                }
//            }

//            var store = await LoadStoreAsync();
//            if (store.Batches.Any(b => b.ProductId == model.ProductId && b.BatchNumber.Equals(batchNum, StringComparison.OrdinalIgnoreCase)))
//            {
//                return (false, $"Batch Number '{batchNum}' already exists for this product.", null);
//            }

//            int nextBatchId = store.Batches.Any() ? store.Batches.Max(b => b.BatchId) + 1 : 1;
//            var newBatch = new ProductBatch
//            {
//                BatchId = nextBatchId,
//                ProductId = model.ProductId,
//                BatchNumber = batchNum,
//                ManufactureDate = model.ManufactureDate,
//                ExpirationDate = model.ExpirationDate,
//                UnitCost = model.UnitCost,
//                IsActive = model.IsActive,
//                CreatedAt = DateTime.UtcNow
//            };
//            store.Batches.Add(newBatch);

//            if (model.Quantity > 0)
//            {
//                int locId = model.LocationId ?? 1;
//                int nextStockId = store.Stocks.Any() ? store.Stocks.Max(s => s.InventoryId) + 1 : 1;
//                store.Stocks.Add(new InventoryStock
//                {
//                    InventoryId = nextStockId,
//                    ProductId = model.ProductId,
//                    BatchId = nextBatchId,
//                    LocationId = locId,
//                    Quantity = model.Quantity,
//                    LastUpdated = DateTime.UtcNow
//                });
//            }

//            await SaveStoreAsync(store);
//            return (true, null, newBatch);
//        }

//        public async Task<(bool Success, string? ErrorMessage)> UpdateBatchAsync(BatchFormViewModel model)
//        {
//            string batchNum = model.BatchNumber.Trim().ToUpper();

//            if (await CanUseDatabaseAsync())
//            {
//                try
//                {
//                    var batch = await _context.ProductBatches.FindAsync(model.BatchId);
//                    if (batch == null) return (false, "Batch not found.");

//                    bool exists = await _context.ProductBatches
//                        .AnyAsync(b => b.ProductId == batch.ProductId && b.BatchNumber.ToUpper() == batchNum && b.BatchId != model.BatchId);
//                    if (exists) return (false, $"Batch Number '{batchNum}' already exists for this product.");

//                    batch.BatchNumber = batchNum;
//                    batch.ManufactureDate = model.ManufactureDate;
//                    batch.ExpirationDate = model.ExpirationDate;
//                    batch.UnitCost = model.UnitCost;
//                    batch.IsActive = model.IsActive;

//                    // Update stock quantity if provided
//                    var stock = await _context.InventoryStocks.FirstOrDefaultAsync(s => s.BatchId == model.BatchId);
//                    if (stock != null)
//                    {
//                        stock.Quantity = model.Quantity;
//                        stock.LastUpdated = DateTime.UtcNow;
//                    }
//                    else if (model.Quantity > 0)
//                    {
//                        int locId = model.LocationId ?? (await GetLocationsAsync()).FirstOrDefault()?.LocationId ?? 1;
//                        _context.InventoryStocks.Add(new InventoryStock
//                        {
//                            ProductId = batch.ProductId,
//                            BatchId = batch.BatchId,
//                            LocationId = locId,
//                            Quantity = model.Quantity,
//                            LastUpdated = DateTime.UtcNow
//                        });
//                    }

//                    await _context.SaveChangesAsync();
//                    return (true, null);
//                }
//                catch
//                {
//                }
//            }

//            var store = await LoadStoreAsync();
//            var bTarget = store.Batches.FirstOrDefault(b => b.BatchId == model.BatchId);
//            if (bTarget == null) return (false, "Batch not found.");

//            bTarget.BatchNumber = batchNum;
//            bTarget.ManufactureDate = model.ManufactureDate;
//            bTarget.ExpirationDate = model.ExpirationDate;
//            bTarget.UnitCost = model.UnitCost;
//            bTarget.IsActive = model.IsActive;

//            var sTarget = store.Stocks.FirstOrDefault(s => s.BatchId == model.BatchId);
//            if (sTarget != null)
//            {
//                sTarget.Quantity = model.Quantity;
//                sTarget.LastUpdated = DateTime.UtcNow;
//            }
//            else if (model.Quantity > 0)
//            {
//                int nextStockId = store.Stocks.Any() ? store.Stocks.Max(s => s.InventoryId) + 1 : 1;
//                store.Stocks.Add(new InventoryStock
//                {
//                    InventoryId = nextStockId,
//                    ProductId = bTarget.ProductId,
//                    BatchId = bTarget.BatchId,
//                    LocationId = model.LocationId ?? 1,
//                    Quantity = model.Quantity,
//                    LastUpdated = DateTime.UtcNow
//                });
//            }

//            await SaveStoreAsync(store);
//            return (true, null);
//        }

//        public async Task<bool> DeleteBatchAsync(int id)
//        {
//            if (await CanUseDatabaseAsync())
//            {
//                try
//                {
//                    var batch = await _context.ProductBatches.FindAsync(id);
//                    if (batch != null)
//                    {
//                        var stocks = _context.InventoryStocks.Where(s => s.BatchId == id);
//                        _context.InventoryStocks.RemoveRange(stocks);
//                        _context.ProductBatches.Remove(batch);
//                        await _context.SaveChangesAsync();
//                        return true;
//                    }
//                }
//                catch
//                {
//                }
//            }

//            var store = await LoadStoreAsync();
//            var b = store.Batches.FirstOrDefault(x => x.BatchId == id);
//            if (b != null)
//            {
//                store.Stocks.RemoveAll(s => s.BatchId == id);
//                store.Batches.Remove(b);
//                await SaveStoreAsync(store);
//                return true;
//            }

//            return false;
//        }

//        public async Task<List<Location>> GetLocationsAsync()
//        {
//            if (await CanUseDatabaseAsync())
//            {
//                try
//                {
//                    var locs = await _context.Locations.Where(l => !l.IsDeleted).ToListAsync();
//                    if (locs.Any()) return locs;
//                }
//                catch
//                {
//                }
//            }

//            var store = await LoadStoreAsync();
//            return store.Locations;
//        }

//        #endregion

//        #region Seeding & Store Resilience

//        public async Task EnsureDefaultDataAsync()
//        {
//            var store = await LoadStoreAsync();
//            bool changed = false;

//            if (!store.Categories.Any())
//            {
//                int catId = 1;
//                store.Categories = new List<Category>
//                {
//                    new() { CategoryId = catId++, CategoryName = "Dairy & Eggs", Description = "Fresh dairy products, milk, cheeses, butter and farm eggs", IsActive = true, CreatedAt = DateTime.UtcNow },
//                    new() { CategoryId = catId++, CategoryName = "Bakery & Bread", Description = "Fresh breads, artisan buns, pastries and bakery essentials", IsActive = true, CreatedAt = DateTime.UtcNow },
//                    new() { CategoryId = catId++, CategoryName = "Beverages & Drinks", Description = "Juices, soft drinks, sparkling waters, coffee and teas", IsActive = true, CreatedAt = DateTime.UtcNow },
//                    new() { CategoryId = catId++, CategoryName = "Snacks & Confectionery", Description = "Chocolates, potato crisps, biscuits and roasted nuts", IsActive = true, CreatedAt = DateTime.UtcNow },
//                    new() { CategoryId = catId++, CategoryName = "Fresh Produce", Description = "Organic fruits, farm-fresh vegetables and seasonal herbs", IsActive = true, CreatedAt = DateTime.UtcNow },
//                    new() { CategoryId = catId++, CategoryName = "Canned & Pantry", Description = "Pastas, sauces, canned beans, grains and cooking oils", IsActive = true, CreatedAt = DateTime.UtcNow }
//                };
//                changed = true;
//            }

//            if (!store.Locations.Any())
//            {
//                int locId = 1;
//                store.Locations = new List<Location>
//                {
//                    new() { LocationId = locId++, LocationName = "Main Store Floor", LocationType = "Store", IsActive = true },
//                    new() { LocationId = locId++, LocationName = "Central Warehouse", LocationType = "Warehouse", IsActive = true }
//                };
//                changed = true;
//            }

//            if (!store.Products.Any())
//            {
//                var today = DateOnly.FromDateTime(DateTime.Today);
//                int pId = 1;
//                int bId = 1;
//                int sId = 1;

//                // Product 1: Whole Milk 1L
//                var p1 = new Product
//                {
//                    ProductId = pId++,
//                    CategoryId = 1,
//                    Sku = "PRD-MILK-001",
//                    Barcode = "890100100001",
//                    ProductName = "Fresh Whole Milk 1L",
//                    UnitOfMeasure = "BOTTLE",
//                    PurchaseCost = 1.20m,
//                    SellingPrice = 1.89m,
//                    MinStockLevel = 20,
//                    IsActive = true,
//                    CreatedAt = DateTime.UtcNow
//                };
//                store.Products.Add(p1);
//                store.Batches.Add(new ProductBatch
//                {
//                    BatchId = bId++,
//                    ProductId = p1.ProductId,
//                    BatchNumber = "BATCH-2026-M01",
//                    ManufactureDate = today.AddDays(-3),
//                    ExpirationDate = today.AddDays(14),
//                    UnitCost = 1.20m,
//                    IsActive = true,
//                    CreatedAt = DateTime.UtcNow
//                });
//                store.Stocks.Add(new InventoryStock
//                {
//                    InventoryId = sId++,
//                    ProductId = p1.ProductId,
//                    BatchId = bId - 1,
//                    LocationId = 1,
//                    Quantity = 60,
//                    LastUpdated = DateTime.UtcNow
//                });

//                // Product 2: Organic Sourdough Bread (Low Stock)
//                var p2 = new Product
//                {
//                    ProductId = pId++,
//                    CategoryId = 2,
//                    Sku = "PRD-BRD-002",
//                    Barcode = "890100100002",
//                    ProductName = "Artisan Sourdough Loaf 500g",
//                    UnitOfMeasure = "PCS",
//                    PurchaseCost = 2.10m,
//                    SellingPrice = 3.99m,
//                    MinStockLevel = 15,
//                    IsActive = true,
//                    CreatedAt = DateTime.UtcNow
//                };
//                store.Products.Add(p2);
//                store.Batches.Add(new ProductBatch
//                {
//                    BatchId = bId++,
//                    ProductId = p2.ProductId,
//                    BatchNumber = "BATCH-2026-B01",
//                    ManufactureDate = today.AddDays(-1),
//                    ExpirationDate = today.AddDays(5),
//                    UnitCost = 2.10m,
//                    IsActive = true,
//                    CreatedAt = DateTime.UtcNow
//                });
//                store.Stocks.Add(new InventoryStock
//                {
//                    InventoryId = sId++,
//                    ProductId = p2.ProductId,
//                    BatchId = bId - 1,
//                    LocationId = 1,
//                    Quantity = 8, // Low stock: 8 <= 15
//                    LastUpdated = DateTime.UtcNow
//                });

//                // Product 3: Sparkling Mineral Water (In Stock)
//                var p3 = new Product
//                {
//                    ProductId = pId++,
//                    CategoryId = 3,
//                    Sku = "PRD-WTR-003",
//                    Barcode = "890100100003",
//                    ProductName = "Sparkling Spring Water 500ml",
//                    UnitOfMeasure = "BOTTLE",
//                    PurchaseCost = 0.65m,
//                    SellingPrice = 1.49m,
//                    MinStockLevel = 30,
//                    IsActive = true,
//                    CreatedAt = DateTime.UtcNow
//                };
//                store.Products.Add(p3);
//                store.Batches.Add(new ProductBatch
//                {
//                    BatchId = bId++,
//                    ProductId = p3.ProductId,
//                    BatchNumber = "BATCH-2026-W01",
//                    ManufactureDate = today.AddDays(-30),
//                    ExpirationDate = today.AddDays(250),
//                    UnitCost = 0.65m,
//                    IsActive = true,
//                    CreatedAt = DateTime.UtcNow
//                });
//                store.Stocks.Add(new InventoryStock
//                {
//                    InventoryId = sId++,
//                    ProductId = p3.ProductId,
//                    BatchId = bId - 1,
//                    LocationId = 1,
//                    Quantity = 120,
//                    LastUpdated = DateTime.UtcNow
//                });

//                // Product 4: Greek Yogurt (Expiring Soon & Expired batches)
//                var p4 = new Product
//                {
//                    ProductId = pId++,
//                    CategoryId = 1,
//                    Sku = "PRD-YGT-004",
//                    Barcode = "890100100004",
//                    ProductName = "Authentic Greek Yogurt 500g",
//                    UnitOfMeasure = "PACK",
//                    PurchaseCost = 1.90m,
//                    SellingPrice = 3.29m,
//                    MinStockLevel = 25,
//                    IsActive = true,
//                    CreatedAt = DateTime.UtcNow
//                };
//                store.Products.Add(p4);
//                // Expired batch
//                store.Batches.Add(new ProductBatch
//                {
//                    BatchId = bId++,
//                    ProductId = p4.ProductId,
//                    BatchNumber = "BATCH-2026-Y01",
//                    ManufactureDate = today.AddDays(-20),
//                    ExpirationDate = today.AddDays(-2), // Expired
//                    UnitCost = 1.90m,
//                    IsActive = true,
//                    CreatedAt = DateTime.UtcNow
//                });
//                store.Stocks.Add(new InventoryStock
//                {
//                    InventoryId = sId++,
//                    ProductId = p4.ProductId,
//                    BatchId = bId - 1,
//                    LocationId = 1,
//                    Quantity = 10,
//                    LastUpdated = DateTime.UtcNow
//                });
//                // Fresh batch expiring soon
//                store.Batches.Add(new ProductBatch
//                {
//                    BatchId = bId++,
//                    ProductId = p4.ProductId,
//                    BatchNumber = "BATCH-2026-Y02",
//                    ManufactureDate = today.AddDays(-5),
//                    ExpirationDate = today.AddDays(18), // Expiring soon
//                    UnitCost = 1.90m,
//                    IsActive = true,
//                    CreatedAt = DateTime.UtcNow
//                });
//                store.Stocks.Add(new InventoryStock
//                {
//                    InventoryId = sId++,
//                    ProductId = p4.ProductId,
//                    BatchId = bId - 1,
//                    LocationId = 1,
//                    Quantity = 35,
//                    LastUpdated = DateTime.UtcNow
//                });

//                // Product 5: Roasted Almonds (Out of Stock)
//                var p5 = new Product
//                {
//                    ProductId = pId++,
//                    CategoryId = 4,
//                    Sku = "PRD-ALM-005",
//                    Barcode = "890100100005",
//                    ProductName = "Premium Roasted Almonds 200g",
//                    UnitOfMeasure = "PACK",
//                    PurchaseCost = 3.50m,
//                    SellingPrice = 5.99m,
//                    MinStockLevel = 10,
//                    IsActive = true,
//                    CreatedAt = DateTime.UtcNow
//                };
//                store.Products.Add(p5);
//                // No batches / 0 stock

//                changed = true;

//                var descMap = await LoadDescriptionMapAsync();
//                descMap[p1.ProductId] = "Pasteurized whole cow milk packed with natural nutrients.";
//                descMap[p2.ProductId] = "Slow-fermented artisan sourdough bread with crispy golden crust.";
//                descMap[p3.ProductId] = "Crisp, refreshing carbonated mineral water sourced from natural springs.";
//                descMap[p4.ProductId] = "Rich, creamy strained Greek yogurt made from fresh whole milk.";
//                descMap[p5.ProductId] = "Lightly salted, oven-roasted California almonds in resealable pouch.";
//                await SaveDescriptionMapAsync(descMap);
//            }

//            if (changed)
//            {
//                await SaveStoreAsync(store);
//            }
//        }

//        private async Task<(bool Success, string? ErrorMessage, Product? Product)> CreateProductInStoreAsync(ProductFormViewModel model)
//        {
//            var store = await LoadStoreAsync();
//            int nextId = store.Products.Any() ? store.Products.Max(p => p.ProductId) + 1 : 1;

//            var product = new Product
//            {
//                ProductId = nextId,
//                Sku = model.Sku.Trim().ToUpper(),
//                Barcode = model.Barcode.Trim(),
//                ProductName = model.ProductName.Trim(),
//                CategoryId = model.CategoryId,
//                UnitOfMeasure = model.UnitOfMeasure.Trim().ToUpper(),
//                PurchaseCost = model.PurchaseCost,
//                SellingPrice = model.SellingPrice,
//                MinStockLevel = model.MinStockLevel,
//                IsActive = model.IsActive,
//                CreatedAt = DateTime.UtcNow,
//                IsDeleted = false
//            };

//            store.Products.Add(product);

//            if (!string.IsNullOrWhiteSpace(model.Description))
//            {
//                var descMap = await LoadDescriptionMapAsync();
//                descMap[product.ProductId] = model.Description.Trim();
//                await SaveDescriptionMapAsync(descMap);
//            }

//            if (model.CreateInitialBatch && !string.IsNullOrWhiteSpace(model.InitialBatchNumber) && (model.InitialQuantity ?? 0) > 0)
//            {
//                int nextBatchId = store.Batches.Any() ? store.Batches.Max(b => b.BatchId) + 1 : 1;
//                store.Batches.Add(new ProductBatch
//                {
//                    BatchId = nextBatchId,
//                    ProductId = product.ProductId,
//                    BatchNumber = model.InitialBatchNumber.Trim().ToUpper(),
//                    ManufactureDate = model.InitialManufactureDate,
//                    ExpirationDate = model.InitialExpirationDate,
//                    UnitCost = model.PurchaseCost,
//                    IsActive = true,
//                    CreatedAt = DateTime.UtcNow
//                });

//                int nextStockId = store.Stocks.Any() ? store.Stocks.Max(s => s.InventoryId) + 1 : 1;
//                store.Stocks.Add(new InventoryStock
//                {
//                    InventoryId = nextStockId,
//                    ProductId = product.ProductId,
//                    BatchId = nextBatchId,
//                    LocationId = model.InitialLocationId ?? 1,
//                    Quantity = model.InitialQuantity ?? 0,
//                    LastUpdated = DateTime.UtcNow
//                });
//            }

//            await SaveStoreAsync(store);
//            return (true, null, product);
//        }

//        private async Task<List<ProductListViewModel>> LoadProductsFromStoreAsync(int? categoryId, string? search)
//        {
//            //var store = await LoadStoreAsync();
//            var descMap = await LoadDescriptionMapAsync();
//            var today = DateOnly.FromDateTime(DateTime.Today);

//            var query = store.Products.Where(p => !p.IsDeleted).AsQueryable();

//            if (categoryId.HasValue && categoryId.Value > 0)
//            {
//                query = query.Where(p => p.CategoryId == categoryId.Value);
//            }

//            if (!string.IsNullOrWhiteSpace(search))
//            {
//                var s = search.Trim().ToLower();
//                query = query.Where(p =>
//                    p.ProductName.ToLower().Contains(s) ||
//                    p.Sku.ToLower().Contains(s) ||
//                    p.Barcode.ToLower().Contains(s));
//            }

//            var list = new List<ProductListViewModel>();

//            foreach (var p in query)
//            {
//                var cat = store.Categories.FirstOrDefault(c => c.CategoryId == p.CategoryId);
//                var batches = store.Batches.Where(b => b.ProductId == p.ProductId && b.IsActive).ToList();
//                decimal totalStock = store.Stocks.Where(s => s.ProductId == p.ProductId).Sum(s => s.Quantity);

//                bool hasExpiring = batches.Any(b => b.ExpirationDate.HasValue && b.ExpirationDate.Value > today && b.ExpirationDate.Value <= today.AddDays(30));
//                bool hasExpired = batches.Any(b => b.ExpirationDate.HasValue && b.ExpirationDate.Value < today);

//                string? desc = descMap.TryGetValue(p.ProductId, out var d) ? d : null;

//                list.Add(new ProductListViewModel
//                {
//                    ProductId = p.ProductId,
//                    Sku = p.Sku,
//                    Barcode = p.Barcode,
//                    ProductName = p.ProductName,
//                    Description = desc,
//                    CategoryId = p.CategoryId,
//                    CategoryName = cat?.CategoryName ?? "General",
//                    UnitOfMeasure = p.UnitOfMeasure,
//                    PurchaseCost = p.PurchaseCost,
//                    SellingPrice = p.SellingPrice,
//                    MinStockLevel = p.MinStockLevel,
//                    TotalStock = totalStock,
//                    ActiveBatchCount = batches.Count,
//                    IsActive = p.IsActive ?? true,
//                    HasExpiringBatches = hasExpiring,
//                    HasExpiredBatches = hasExpired
//                });
//            }

//            return list;
//        }

//        private class ProductStoreData
//        {
//            public List<Product> Products { get; set; } = new();
//            public List<Category> Categories { get; set; } = new();
//            public List<ProductBatch> Batches { get; set; } = new();
//            public List<InventoryStock> Stocks { get; set; } = new();
//            public List<Location> Locations { get; set; } = new();
//        }

//        //private async Task<ProductStoreData> LoadStoreAsync()
//        //{
//        //    await _fileLock.WaitAsync();
//        //    try
//        //    {
//        //        if (File.Exists(_storeFilePath))
//        //        {
//        //            string json = await File.ReadAllTextAsync(_storeFilePath);
//        //            var data = JsonSerializer.Deserialize<ProductStoreData>(json);
//        //            if (data != null) return data;
//        //        }
//        //        return new ProductStoreData();
//        //    }
//        //    finally
//        //    {
//        //        _fileLock.Release();
//        //    }
//        //}

//        //private async Task SaveStoreAsync(ProductStoreData data)
//        //{
//        //    await _fileLock.WaitAsync();
//        //    try
//        //    {
//        //        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
//        //        await File.WriteAllTextAsync(_storeFilePath, json);
//        //    }
//        //    finally
//        //    {
//        //        _fileLock.Release();
//        //    }
//        //}

//        private async Task<Dictionary<int, string>> LoadDescriptionMapAsync()
//        {
//            string descPath = Path.Combine(_env.ContentRootPath, "App_Data", "product_descriptions.json");
//            if (File.Exists(descPath))
//            {
//                try
//                {
//                    string json = await File.ReadAllTextAsync(descPath);
//                    return JsonSerializer.Deserialize<Dictionary<int, string>>(json) ?? new();
//                }
//                catch { }
//            }
//            return new();
//        }

//        private async Task SaveDescriptionAsync(int productId, string description)
//        {
//            var map = await LoadDescriptionMapAsync();
//            map[productId] = description;
//            await SaveDescriptionMapAsync(map);
//        }

//        private async Task SaveDescriptionMapAsync(Dictionary<int, string> map)
//        {
//            string descPath = Path.Combine(_env.ContentRootPath, "App_Data", "product_descriptions.json");
//            string json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
//            await File.WriteAllTextAsync(descPath, json);
//        }

//        #endregion
//    }
//}


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SuperMarket.Models;
using SuperMarket.Models.ViewModels;

namespace SuperMarket.Services
{
    public class ProductService : IProductService
    {
        private readonly DBContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductService(DBContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        #region Products

        public async Task<ProductIndexFilterViewModel> GetProductsAsync(int? categoryId = null, string? search = null, string? stockFilter = null)
        {

            var productsList = new List<ProductListViewModel>();
            var categories = await GetCategoriesAsync();

            var query = _context.Products
                .Where(p => !p.IsDeleted)
                .Include(p => p.Category)
                .Include(p => p.ProductBatches)
                    .ThenInclude(b => b.InventoryStocks)
                .AsQueryable();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(s) ||
                    p.Sku.ToLower().Contains(s) ||
                    p.Barcode.ToLower().Contains(s) ||
                    p.Category.CategoryName.ToLower().Contains(s));
            }

            var dbProducts = await query.ToListAsync();
            var descMap = await LoadDescriptionMapAsync();
            var today = DateOnly.FromDateTime(DateTime.Today);

            foreach (var p in dbProducts)
            {
                var totalStock = p.ProductBatches
                    .SelectMany(b => b.InventoryStocks)
                    .Sum(s => s.Quantity);

                var activeBatches = p.ProductBatches.Where(b => b.IsActive).ToList();
                bool hasExpiring = activeBatches.Any(b => b.ExpirationDate.HasValue && b.ExpirationDate.Value > today && b.ExpirationDate.Value <= today.AddDays(30));
                bool hasExpired = activeBatches.Any(b => b.ExpirationDate.HasValue && b.ExpirationDate.Value < today);

                string? desc = descMap.TryGetValue(p.ProductId, out var d) ? d : p.Description;

                productsList.Add(new ProductListViewModel
                {
                    ProductId = p.ProductId,
                    Sku = p.Sku,
                    Barcode = p.Barcode,
                    ProductName = p.ProductName,
                    Description = desc,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category?.CategoryName ?? "General",
                    UnitOfMeasure = p.UnitOfMeasure,
                    PurchaseCost = p.PurchaseCost,
                    SellingPrice = p.SellingPrice,
                    MinStockLevel = p.MinStockLevel,
                    TotalStock = totalStock,
                    ActiveBatchCount = activeBatches.Count,
                    IsActive = p.IsActive ?? true,
                    HasExpiringBatches = hasExpiring,
                    HasExpiredBatches = hasExpired
                });
            }

            // Apply stock level filter
            if (!string.IsNullOrWhiteSpace(stockFilter) && stockFilter.ToLower() != "all")
            {
                productsList = stockFilter.ToLower() switch
                {
                    "lowstock" => productsList.Where(p => p.TotalStock > 0 && p.TotalStock <= p.MinStockLevel).ToList(),
                    "outofstock" => productsList.Where(p => p.TotalStock <= 0).ToList(),
                    "instock" => productsList.Where(p => p.TotalStock > p.MinStockLevel).ToList(),
                    _ => productsList
                };
            }

            var categoryItems = categories.Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.CategoryName,
                Selected = categoryId.HasValue && categoryId.Value == c.CategoryId
            }).ToList();

            var totalVal = productsList.Sum(p => p.TotalStock * p.PurchaseCost);

            return new ProductIndexFilterViewModel
            {
                Products = productsList.OrderBy(p => p.ProductName).ToList(),
                Categories = categoryItems,
                SelectedCategoryId = categoryId,
                SearchTerm = search,
                StockFilter = stockFilter ?? "all",
                TotalProducts = productsList.Count,
                InStockCount = productsList.Count(p => p.TotalStock > p.MinStockLevel),
                LowStockCount = productsList.Count(p => p.TotalStock > 0 && p.TotalStock <= p.MinStockLevel),
                OutOfStockCount = productsList.Count(p => p.TotalStock <= 0),
                TotalInventoryValue = Math.Round(totalVal, 2)
            };
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {

            var p = await _context.Products
                .Include(x => x.Category)
                .Include(x => x.ProductBatches)
                    .ThenInclude(b => b.InventoryStocks)
                .FirstOrDefaultAsync(x => x.ProductId == id && !x.IsDeleted);

            if (p != null)
            {
                var descMap = await LoadDescriptionMapAsync();
                if (descMap.TryGetValue(p.ProductId, out var desc))
                {
                    p.Description = desc;
                }
                return p;
            }

            return null;
        }

        public async Task<ProductDetailsViewModel?> GetProductDetailsAsync(int id)
        {

            var product = await GetProductByIdAsync(id);
            if (product == null) return null;

            var batches = await GetBatchesAsync(id);
            var stockByLocation = new Dictionary<string, decimal>();

            var stocks = await _context.InventoryStocks
                .Where(s => s.ProductId == id)
                .Include(s => s.Location)
                .ToListAsync();

            foreach (var s in stocks)
            {
                string locName = s.Location?.LocationName ?? "Store Floor";
                if (!stockByLocation.ContainsKey(locName)) stockByLocation[locName] = 0;
                stockByLocation[locName] += s.Quantity;
            }


            if (!stockByLocation.Any())
            {
                stockByLocation["Main Store Floor"] = batches.Sum(b => b.TotalQuantity);
            }

            var totalStock = batches.Sum(b => b.TotalQuantity);

            return new ProductDetailsViewModel
            {
                Product = product,
                CategoryName = product.Category?.CategoryName ?? "General",
                TotalStock = totalStock,
                StockStatus = totalStock <= 0 ? "Out of Stock" : (totalStock <= product.MinStockLevel ? "Low Stock" : "In Stock"),
                Batches = batches.OrderBy(b => b.ExpirationDate).ToList(),
                StockByLocation = stockByLocation
            };
        }

        public async Task<(bool Success, string? ErrorMessage, Product? Product)> CreateProductAsync(ProductFormViewModel model)
        {

            // Validate unique SKU
            if (!await IsSkuUniqueAsync(model.Sku))
            {
                return (false, $"Product Code (SKU) '{model.Sku}' is already registered.", null);
            }

            // Validate unique Barcode
            if (!await IsBarcodeUniqueAsync(model.Barcode))
            {
                return (false, $"Barcode '{model.Barcode}' is already registered to another product.", null);
            }

            var product = new Product
            {
                Sku = model.Sku.Trim().ToUpper(),
                Barcode = model.Barcode.Trim(),
                ProductName = model.ProductName.Trim(),
                CategoryId = model.CategoryId,
                UnitOfMeasure = model.UnitOfMeasure.Trim().ToUpper(),
                PurchaseCost = model.PurchaseCost,
                SellingPrice = model.SellingPrice,
                MinStockLevel = model.MinStockLevel,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(model.Description))
            {
                await SaveDescriptionAsync(product.ProductId, model.Description.Trim());
            }

            // Handle initial batch if requested
            if (model.CreateInitialBatch && !string.IsNullOrWhiteSpace(model.InitialBatchNumber) && (model.InitialQuantity ?? 0) > 0)
            {
                int locId = model.InitialLocationId ?? (await GetLocationsAsync()).FirstOrDefault()?.LocationId ?? 1;

                var batch = new ProductBatch
                {
                    ProductId = product.ProductId,
                    BatchNumber = model.InitialBatchNumber.Trim().ToUpper(),
                    ManufactureDate = model.InitialManufactureDate,
                    ExpirationDate = model.InitialExpirationDate,
                    UnitCost = model.PurchaseCost,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ProductBatches.Add(batch);
                await _context.SaveChangesAsync();

                var stock = new InventoryStock
                {
                    ProductId = product.ProductId,
                    BatchId = batch.BatchId,
                    LocationId = locId,
                    Quantity = model.InitialQuantity ?? 0,
                    LastUpdated = DateTime.UtcNow
                };

                _context.InventoryStocks.Add(stock);
                await _context.SaveChangesAsync();
            }

            return (true, null, product);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateProductAsync(ProductFormViewModel model)
        {

            if (!await IsSkuUniqueAsync(model.Sku, model.ProductId))
            {
                return (false, $"Product Code (SKU) '{model.Sku}' is already registered.");
            }

            if (!await IsBarcodeUniqueAsync(model.Barcode, model.ProductId))
            {
                return (false, $"Barcode '{model.Barcode}' is already registered to another product.");
            }

            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null || product.IsDeleted) return (false, "Product not found.");

            product.Sku = model.Sku.Trim().ToUpper();
            product.Barcode = model.Barcode.Trim();
            product.ProductName = model.ProductName.Trim();
            product.CategoryId = model.CategoryId;
            product.UnitOfMeasure = model.UnitOfMeasure.Trim().ToUpper();
            product.PurchaseCost = model.PurchaseCost;
            product.SellingPrice = model.SellingPrice;
            product.MinStockLevel = model.MinStockLevel;
            product.IsActive = model.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (model.Description != null)
            {
                await SaveDescriptionAsync(product.ProductId, model.Description.Trim());
            }

            return (true, null);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {

            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsDeleted = true;
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> IsSkuUniqueAsync(string sku, int? currentProductId = null)
        {

            if (string.IsNullOrWhiteSpace(sku)) return false;
            string cleanSku = sku.Trim().ToUpper();

            bool exists = await _context.Products
                .AnyAsync(p => p.Sku.ToUpper() == cleanSku && !p.IsDeleted && (!currentProductId.HasValue || p.ProductId != currentProductId.Value));
            return !exists;
        }

        public async Task<bool> IsBarcodeUniqueAsync(string barcode, int? currentProductId = null)
        {

            if (string.IsNullOrWhiteSpace(barcode)) return false;
            string cleanBarcode = barcode.Trim();

            bool exists = await _context.Products
                .AnyAsync(p => p.Barcode == cleanBarcode && !p.IsDeleted && (!currentProductId.HasValue || p.ProductId != currentProductId.Value));
            return !exists;
        }

        public string GenerateUniqueSku()
        {
            var random = new Random();
            return $"PRD-{DateTime.UtcNow:yyMMdd}-{random.Next(100, 999)}";
        }

        public string GenerateUniqueBarcode()
        {
            var random = new Random();
            long prefix = 890000000000L;
            long val = prefix + (long)random.Next(100000, 999999);
            return val.ToString();
        }

        #endregion

        #region Categories

        public async Task<List<CategoryFormViewModel>> GetCategoriesAsync()
        {

            var list = await _context.Categories
                .Where(c => !c.IsDeleted)
                .Select(c => new CategoryFormViewModel
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    ProductCount = c.Products.Count(p => !p.IsDeleted)
                })
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return list;

        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {

            return await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id && !c.IsDeleted);
        }

        public async Task<(bool Success, string? ErrorMessage, Category? Category)> CreateCategoryAsync(CategoryFormViewModel model)
        {

            string catName = model.CategoryName.Trim();

            bool exists = await _context.Categories
                .AnyAsync(c => c.CategoryName.ToLower() == catName.ToLower() && !c.IsDeleted);
            if (exists) return (false, $"Category '{catName}' already exists.", null);

            var cat = new Category
            {
                CategoryName = catName,
                Description = model.Description?.Trim(),
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Categories.Add(cat);
            await _context.SaveChangesAsync();
            return (true, null, cat);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateCategoryAsync(CategoryFormViewModel model)
        {

            string catName = model.CategoryName.Trim();

            bool exists = await _context.Categories
                .AnyAsync(c => c.CategoryName.ToLower() == catName.ToLower() && c.CategoryId != model.CategoryId && !c.IsDeleted);
            if (exists) return (false, $"Another category named '{catName}' already exists.");

            var cat = await _context.Categories.FindAsync(model.CategoryId);
            if (cat == null || cat.IsDeleted) return (false, "Category not found.");

            cat.CategoryName = catName;
            cat.Description = model.Description?.Trim();
            cat.IsActive = model.IsActive;
            cat.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {

            var cat = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.CategoryId == id);
            if (cat != null)
            {
                cat.IsDeleted = true;
                cat.IsActive = false;
                cat.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        #endregion

        #region Batches

        public async Task<List<BatchItemViewModel>> GetBatchesAsync(int? productId = null)
        {

            var list = new List<BatchItemViewModel>();

            var query = _context.ProductBatches
                .Include(b => b.Product)
                .Include(b => b.InventoryStocks)
                .AsQueryable();

            if (productId.HasValue && productId.Value > 0)
            {
                query = query.Where(b => b.ProductId == productId.Value);
            }

            var dbBatches = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
            foreach (var b in dbBatches)
            {
                decimal qty = b.InventoryStocks?.Sum(s => s.Quantity) ?? 0;
                list.Add(new BatchItemViewModel
                {
                    BatchId = b.BatchId,
                    ProductId = b.ProductId,
                    ProductName = b.Product?.ProductName ?? "Product #" + b.ProductId,
                    ProductSku = b.Product?.Sku ?? "",
                    BatchNumber = b.BatchNumber,
                    ManufactureDate = b.ManufactureDate,
                    ExpirationDate = b.ExpirationDate,
                    UnitCost = b.UnitCost,
                    TotalQuantity = qty,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt
                });
            }

            return list;

        }

        public async Task<BatchItemViewModel?> GetBatchByIdAsync(int id)
        {
            var batches = await GetBatchesAsync();
            return batches.FirstOrDefault(b => b.BatchId == id);
        }

        public async Task<(bool Success, string? ErrorMessage, ProductBatch? Batch)> CreateBatchAsync(BatchFormViewModel model)
        {

            string batchNum = model.BatchNumber.Trim().ToUpper();

            bool exists = await _context.ProductBatches
                .AnyAsync(b => b.ProductId == model.ProductId && b.BatchNumber.ToUpper() == batchNum);
            if (exists) return (false, $"Batch Number '{batchNum}' already exists for this product.", null);

            var batch = new ProductBatch
            {
                ProductId = model.ProductId,
                BatchNumber = batchNum,
                ManufactureDate = model.ManufactureDate,
                ExpirationDate = model.ExpirationDate,
                UnitCost = model.UnitCost,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductBatches.Add(batch);
            await _context.SaveChangesAsync();

            if (model.Quantity > 0)
            {
                int locId = model.LocationId ?? (await GetLocationsAsync()).FirstOrDefault()?.LocationId ?? 1;

                var stock = new InventoryStock
                {
                    ProductId = model.ProductId,
                    BatchId = batch.BatchId,
                    LocationId = locId,
                    Quantity = model.Quantity,
                    LastUpdated = DateTime.UtcNow
                };

                _context.InventoryStocks.Add(stock);
                await _context.SaveChangesAsync();
            }

            return (true, null, batch);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateBatchAsync(BatchFormViewModel model)
        {

            string batchNum = model.BatchNumber.Trim().ToUpper();

            var batch = await _context.ProductBatches.FindAsync(model.BatchId);
            if (batch == null) return (false, "Batch not found.");

            bool exists = await _context.ProductBatches
                .AnyAsync(b => b.ProductId == batch.ProductId && b.BatchNumber.ToUpper() == batchNum && b.BatchId != model.BatchId);
            if (exists) return (false, $"Batch Number '{batchNum}' already exists for this product.");

            batch.BatchNumber = batchNum;
            batch.ManufactureDate = model.ManufactureDate;
            batch.ExpirationDate = model.ExpirationDate;
            batch.UnitCost = model.UnitCost;
            batch.IsActive = model.IsActive;

            // Update stock quantity if provided
            var stock = await _context.InventoryStocks.FirstOrDefaultAsync(s => s.BatchId == model.BatchId);
            if (stock != null)
            {
                stock.Quantity = model.Quantity;
                stock.LastUpdated = DateTime.UtcNow;
            }
            else if (model.Quantity > 0)
            {
                int locId = model.LocationId ?? (await GetLocationsAsync()).FirstOrDefault()?.LocationId ?? 1;
                _context.InventoryStocks.Add(new InventoryStock
                {
                    ProductId = batch.ProductId,
                    BatchId = batch.BatchId,
                    LocationId = locId,
                    Quantity = model.Quantity,
                    LastUpdated = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<bool> DeleteBatchAsync(int id)
        {

            var batch = await _context.ProductBatches.FindAsync(id);
            if (batch != null)
            {
                var stocks = _context.InventoryStocks.Where(s => s.BatchId == id);
                _context.InventoryStocks.RemoveRange(stocks);
                _context.ProductBatches.Remove(batch);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<List<Location>> GetLocationsAsync()
        {

            var locs = await _context.Locations.Where(l => !l.IsDeleted).ToListAsync();
            if (locs.Any()) return locs;

            return new List<Location>();
        }

        #endregion

        #region Description Storage

        private async Task<Dictionary<int, string>> LoadDescriptionMapAsync()
        {
            string descPath = Path.Combine(_env.ContentRootPath, "App_Data", "product_descriptions.json");

            if (File.Exists(descPath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(descPath);
                    return JsonSerializer.Deserialize<Dictionary<int, string>>(json) ?? new();
                }
                catch
                {
                    // Ignore malformed description data and return an empty map.
                }
            }

            return new();
        }

        private async Task SaveDescriptionAsync(int productId, string description)
        {
            var map = await LoadDescriptionMapAsync();
            map[productId] = description;
            await SaveDescriptionMapAsync(map);
        }

        private async Task SaveDescriptionMapAsync(Dictionary<int, string> map)
        {
            string descPath = Path.Combine(_env.ContentRootPath, "App_Data", "product_descriptions.json");
            string json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(descPath, json);
        }

        #endregion
        public async Task EnsureDefaultDataAsync()
        {
            // Ensure the database can be accessed.
            await _context.Database.CanConnectAsync();

            // Check whether default categories already exist.
            if (!await _context.Categories.AnyAsync(c => !c.IsDeleted))
            {
                var categories = new List<Category>
        {
            new Category
            {
                CategoryName = "General",
                Description = "General products",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            }
        };

                await _context.Categories.AddRangeAsync(categories);
                await _context.SaveChangesAsync();
            }
        }
    }
}


