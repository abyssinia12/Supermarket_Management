using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SuperMarket.Models.ViewModels
{
    // Per-location stock row
    public class LocationStockViewModel
    {
        public int InventoryId { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; } = "";
        public string LocationType { get; set; } = "";
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ProductSku { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public int? BatchId { get; set; }
        public string? BatchNumber { get; set; }
        public DateOnly? ExpirationDate { get; set; }
        public decimal Quantity { get; set; }
        public int MinStockLevel { get; set; }
        public int ReorderPoint { get; set; }
        public int MaxStockLevel { get; set; }
        public string StockStatus { get; set; } = "Normal"; // OutOfStock, Low, Normal, Overstocked
        public decimal UnitCost { get; set; }
        public decimal InventoryValue { get; set; }
        public bool IsExpiringSoon { get; set; }
        public bool IsExpired { get; set; }
    }

    // Summary card per location
    public class LocationSummaryViewModel
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = "";
        public string LocationType { get; set; } = "";
        public bool IsActive { get; set; }
        public int TotalProducts { get; set; }
        public decimal TotalUnits { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int ExpiringBatchCount { get; set; }
    }

    // Dashboard
    public class StockDashboardViewModel
    {
        public List<LocationSummaryViewModel> Locations { get; set; } = new();
        public decimal TotalInventoryValue { get; set; }
        public int TotalLowStockAlerts { get; set; }
        public int TotalExpiringBatches { get; set; }
        public int TotalActiveLocations { get; set; }
        public List<LocationStockViewModel> LowStockAlerts { get; set; } = new();
        public List<InventoryTransactionListViewModel> RecentTransactions { get; set; } = new();
        public int PendingAdjustments { get; set; }
        public int PendingTransfers { get; set; }
    }

    // Single transaction row (read-only)
    public class InventoryTransactionListViewModel
    {
        public int TransactionId { get; set; }
        public string ProductName { get; set; } = "";
        public string ProductSku { get; set; } = "";
        public string? BatchNumber { get; set; }
        public string LocationName { get; set; } = "";
        public string UserName { get; set; } = "";
        public string TransactionType { get; set; } = "";
        public decimal QuantityDelta { get; set; }
        public int? ReferenceId { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? Notes { get; set; }
    }

    // List + filter params
    public class InventoryTransactionFilterViewModel
    {
        public List<InventoryTransactionListViewModel> Transactions { get; set; } = new();
        public string? TransactionType { get; set; }
        public int? ProductId { get; set; }
        public int? LocationId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? SearchTerm { get; set; }
        public List<SelectListItem> AvailableTypes { get; set; } = new();
        public List<SelectListItem> AvailableProducts { get; set; } = new();
        public List<SelectListItem> AvailableLocations { get; set; } = new();
    }

    // Adjustment form
    public class StockAdjustmentFormViewModel
    {
        [Required] public int ProductId { get; set; }
        [Required] public int BatchId { get; set; }
        [Required] public int LocationId { get; set; }
        [Required] public string AdjustmentType { get; set; } = "";
        [Required] public decimal Quantity { get; set; }
        [Required][StringLength(255)] public string Reason { get; set; } = "";
        public string? Notes { get; set; }
        public List<SelectListItem> AvailableProducts { get; set; } = new();
        public List<SelectListItem> AvailableBatches { get; set; } = new();
        public List<SelectListItem> AvailableLocations { get; set; } = new();
    }

    // Adjustment list row
    public class StockAdjustmentListViewModel
    {
        public int AdjustmentId { get; set; }
        public string ProductName { get; set; } = "";
        public string? BatchNumber { get; set; }
        public string LocationName { get; set; } = "";
        public string AdjustmentType { get; set; } = "";
        public decimal Quantity { get; set; }
        public string Reason { get; set; } = "";
        public string Status { get; set; } = "";
        public string ReportedByUser { get; set; } = "";
        public string? ApprovedByUser { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    // A single item in a transfer form
    public class TransferItemLineViewModel
    {
        public int ProductId { get; set; }
        public int BatchId { get; set; }
        public decimal Quantity { get; set; }
        public string? ProductName { get; set; }
        public string? BatchNumber { get; set; }
    }

    // Transfer form
    public class StockTransferFormViewModel
    {
        [Required] public int FromLocationId { get; set; }
        [Required] public int ToLocationId { get; set; }
        public string? Notes { get; set; }
        public List<TransferItemLineViewModel> Items { get; set; } = new();
        public List<SelectListItem> AvailableLocations { get; set; } = new();
        public List<SelectListItem> AvailableProducts { get; set; } = new();
        public List<SelectListItem> AvailableBatches { get; set; } = new();
    }

    // Transfer list row
    public class StockTransferListViewModel
    {
        public int TransferId { get; set; }
        public string FromLocationName { get; set; } = "";
        public string ToLocationName { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime? RequestedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string RequestedByUser { get; set; } = "";
        public string? ApprovedByUser { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalUnits { get; set; }
        public string? Notes { get; set; }
    }

    // Transfer item detail
    public class StockTransferItemDetailViewModel
    {
        public int TransferItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ProductSku { get; set; } = "";
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = "";
        public DateOnly? ExpirationDate { get; set; }
        public decimal Quantity { get; set; }
    }

    // Transfer details (full)
    public class StockTransferDetailsViewModel
    {
        public int TransferId { get; set; }
        public string FromLocationName { get; set; } = "";
        public string ToLocationName { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime? RequestedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string RequestedByUser { get; set; } = "";
        public string? ApprovedByUser { get; set; }
        public string? Notes { get; set; }
        public List<StockTransferItemDetailViewModel> Items { get; set; } = new();
    }

    // A single item in a return form
    public class ReturnItemLineViewModel
    {
        public int ProductId { get; set; }
        public int BatchId { get; set; }
        public decimal Quantity { get; set; }
        public string RestockCondition { get; set; } = "Resalable";
        public string? ProductName { get; set; }
        public string? BatchNumber { get; set; }
    }

    // Return form
    public class CustomerReturnFormViewModel
    {
        public int? SaleId { get; set; }
        [Required] public DateTime ReturnDate { get; set; } = DateTime.Today;
        [Required][StringLength(255)] public string Reason { get; set; } = "";
        public decimal RefundAmount { get; set; }
        [Required] public int LocationId { get; set; }
        public string? Notes { get; set; }
        public List<ReturnItemLineViewModel> Items { get; set; } = new();
        public List<SelectListItem> AvailableLocations { get; set; } = new();
        public List<SelectListItem> AvailableProducts { get; set; } = new();
        public List<SelectListItem> AvailableBatches { get; set; } = new();
    }

    // Return list row
    public class CustomerReturnListViewModel
    {
        public int ReturnId { get; set; }
        public DateTime? ReturnDate { get; set; }
        public int? SaleId { get; set; }
        public string Reason { get; set; } = "";
        public decimal RefundAmount { get; set; }
        public string ProcessedByUser { get; set; } = "";
        public int ItemCount { get; set; }
        public decimal TotalQuantity { get; set; }
    }

    // Valuation line
    public class StockValuationLineViewModel
    {
        public string ProductName { get; set; } = "";
        public string Sku { get; set; } = "";
        public string? BatchNumber { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
    }

    // Valuation per location
    public class StockValuationLocationViewModel
    {
        public string LocationName { get; set; } = "";
        public string LocationType { get; set; } = "";
        public List<StockValuationLineViewModel> Lines { get; set; } = new();
        public decimal Subtotal { get; set; }
    }

    // Full valuation report
    public class StockValuationViewModel
    {
        public List<StockValuationLocationViewModel> Locations { get; set; } = new();
        public decimal GrandTotal { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public decimal EstimatedRetailValue { get; set; }
        public decimal EstimatedMargin { get; set; }
    }

    // Location form
    public class LocationFormViewModel
    {
        public int LocationId { get; set; }
        [Required][StringLength(50)] public string LocationName { get; set; } = "";
        [Required] public string LocationType { get; set; } = "Store";
        public bool IsActive { get; set; } = true;
    }
}

