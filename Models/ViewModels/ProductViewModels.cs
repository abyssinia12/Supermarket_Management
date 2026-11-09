using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SuperMarket.Models.ViewModels
{
    public class ProductListViewModel
    {
        public int ProductId { get; set; }

        [Display(Name = "Product Code (SKU)")]
        public string Sku { get; set; } = string.Empty;

        [Display(Name = "Barcode")]
        public string Barcode { get; set; } = string.Empty;

        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        public int CategoryId { get; set; }

        [Display(Name = "Category")]
        public string CategoryName { get; set; } = string.Empty;

        [Display(Name = "UOM")]
        public string UnitOfMeasure { get; set; } = "PCS";

        [Display(Name = "Cost Price")]
        [DataType(DataType.Currency)]
        public decimal PurchaseCost { get; set; }

        [Display(Name = "Selling Price")]
        [DataType(DataType.Currency)]
        public decimal SellingPrice { get; set; }

        [Display(Name = "Min Stock")]
        public int MinStockLevel { get; set; }

        [Display(Name = "Stock Level")]
        public decimal TotalStock { get; set; }

        [Display(Name = "Active Batches")]
        public int ActiveBatchCount { get; set; }

        public string StockStatus => TotalStock switch
        {
            <= 0 => "Out of Stock",
            var s when s <= MinStockLevel => "Low Stock",
            _ => "In Stock"
        };

        public bool IsActive { get; set; } = true;

        public bool HasExpiringBatches { get; set; }
        public bool HasExpiredBatches { get; set; }
    }

    public class ProductIndexFilterViewModel
    {
        public List<ProductListViewModel> Products { get; set; } = new();
        public List<SelectListItem> Categories { get; set; } = new();
        public int? SelectedCategoryId { get; set; }
        public string? SearchTerm { get; set; }
        public string? StockFilter { get; set; } // "all", "lowstock", "outofstock", "instock"
        public int TotalProducts { get; set; }
        public int InStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public decimal TotalInventoryValue { get; set; }
    }

    public class ProductFormViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Unique Product Code (SKU) is required.")]
        [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters.")]
        [Display(Name = "Product Code (SKU)")]
        public string Sku { get; set; } = string.Empty;

        [Required(ErrorMessage = "Barcode is required.")]
        [StringLength(100, ErrorMessage = "Barcode cannot exceed 100 characters.")]
        [Display(Name = "Barcode")]
        public string Barcode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product Name is required.")]
        [StringLength(150, ErrorMessage = "Product Name cannot exceed 150 characters.")]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        [Display(Name = "Product Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please select a category.")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Unit of Measure is required.")]
        [StringLength(20)]
        [Display(Name = "Unit of Measure")]
        public string UnitOfMeasure { get; set; } = "PCS";

        [Required(ErrorMessage = "Cost Price is required.")]
        [Range(0, 9999999.99, ErrorMessage = "Purchase Cost must be a positive number.")]
        [Display(Name = "Purchase Cost")]
        public decimal PurchaseCost { get; set; }

        [Required(ErrorMessage = "Selling Price is required.")]
        [Range(0, 9999999.99, ErrorMessage = "Selling Price must be a positive number.")]
        [Display(Name = "Selling Price")]
        public decimal SellingPrice { get; set; }

        [Required(ErrorMessage = "Minimum stock level is required.")]
        [Range(0, 100000, ErrorMessage = "Minimum Stock Level must be 0 or greater.")]
        [Display(Name = "Minimum Stock Alert Level")]
        public int MinStockLevel { get; set; } = 10;

        [Display(Name = "Product is Active")]
        public bool IsActive { get; set; } = true;

        // Optional initial batch on creation
        [Display(Name = "Record Initial Batch & Opening Stock")]
        public bool CreateInitialBatch { get; set; } = false;

        [Display(Name = "Initial Batch Number")]
        [StringLength(50)]
        public string? InitialBatchNumber { get; set; }

        [Display(Name = "Manufacture Date")]
        [DataType(DataType.Date)]
        public DateOnly? InitialManufactureDate { get; set; }

        [Display(Name = "Expiration Date")]
        [DataType(DataType.Date)]
        public DateOnly? InitialExpirationDate { get; set; }

        [Display(Name = "Opening Stock Quantity")]
        [Range(0, 999999, ErrorMessage = "Opening Quantity must be greater than or equal to 0.")]
        public decimal? InitialQuantity { get; set; }

        [Display(Name = "Storage Location")]
        public int? InitialLocationId { get; set; }

        // Dropdown collections
        public List<SelectListItem> AvailableCategories { get; set; } = new();
        public List<SelectListItem> AvailableLocations { get; set; } = new();
        public List<string> AvailableUnits { get; set; } = new()
        {
            "PCS", "KG", "GRAM", "PACK", "BOX", "BOTTLE", "CAN", "LITRE", "ML", "DOZEN", "BAG"
        };
    }

    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } = null!;
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalStock { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public decimal ProfitMargin => Product.SellingPrice > 0
            ? Math.Round(((Product.SellingPrice - Product.PurchaseCost) / Product.SellingPrice) * 100, 2)
            : 0;

        public List<BatchItemViewModel> Batches { get; set; } = new();
        public Dictionary<string, decimal> StockByLocation { get; set; } = new();
    }

    public class BatchItemViewModel
    {
        public int BatchId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public DateOnly? ManufactureDate { get; set; }
        public DateOnly? ExpirationDate { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalQuantity { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }

        public string ExpiryStatus
        {
            get
            {
                if (!ExpirationDate.HasValue) return "No Expiry";
                var today = DateOnly.FromDateTime(DateTime.Today);
                if (ExpirationDate.Value < today) return "Expired";
                if (ExpirationDate.Value <= today.AddDays(30)) return "Expiring Soon";
                return "Good";
            }
        }

        public int DaysUntilExpiry
        {
            get
            {
                if (!ExpirationDate.HasValue) return int.MaxValue;
                var today = DateOnly.FromDateTime(DateTime.Today);
                return ExpirationDate.Value.DayNumber - today.DayNumber;
            }
        }
    }

    public class BatchFormViewModel
    {
        public int BatchId { get; set; }

        [Required(ErrorMessage = "Please select a product.")]
        [Display(Name = "Product")]
        public int ProductId { get; set; }

        public string? ProductName { get; set; }
        public string? ProductSku { get; set; }

        [Required(ErrorMessage = "Batch number is required.")]
        [StringLength(50, ErrorMessage = "Batch Number cannot exceed 50 characters.")]
        [Display(Name = "Batch Number")]
        public string BatchNumber { get; set; } = string.Empty;

        [Display(Name = "Manufacture Date")]
        [DataType(DataType.Date)]
        public DateOnly? ManufactureDate { get; set; }

        [Display(Name = "Expiration Date")]
        [DataType(DataType.Date)]
        public DateOnly? ExpirationDate { get; set; }

        [Required(ErrorMessage = "Unit cost is required.")]
        [Range(0, 9999999.99, ErrorMessage = "Unit Cost must be positive.")]
        [Display(Name = "Batch Unit Cost")]
        public decimal UnitCost { get; set; }

        [Display(Name = "Batch Quantity")]
        [Range(0, 999999, ErrorMessage = "Quantity must be 0 or greater.")]
        public decimal Quantity { get; set; } = 0;

        [Display(Name = "Storage Location")]
        public int? LocationId { get; set; }

        [Display(Name = "Batch is Active")]
        public bool IsActive { get; set; } = true;

        public List<SelectListItem> AvailableProducts { get; set; } = new();
        public List<SelectListItem> AvailableLocations { get; set; } = new();
    }

    public class CategoryFormViewModel
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Category Name is required.")]
        [StringLength(100, ErrorMessage = "Category Name cannot exceed 100 characters.")]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Category is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Total Products")]
        public int ProductCount { get; set; }
    }
}

