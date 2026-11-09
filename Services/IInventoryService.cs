using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SuperMarket.Models;
using SuperMarket.Models.ViewModels;

namespace SuperMarket.Services
{
    public interface IInventoryService
    {
        // Locations
        Task<List<Location>> GetLocationsAsync();
        Task<Location?> GetLocationByIdAsync(int id);
        Task<(bool Success, string? Error, Location? Location)> CreateLocationAsync(LocationFormViewModel model);
        Task<(bool Success, string? Error)> UpdateLocationAsync(LocationFormViewModel model);
        Task<bool> DeleteLocationAsync(int id);

        // Stock overview
        Task<StockDashboardViewModel> GetStockDashboardAsync();
        Task<List<LocationStockViewModel>> GetStockByLocationAsync(int? locationId = null, int? productId = null, string? stockStatus = null);
        Task<List<LocationSummaryViewModel>> GetLocationSummariesAsync();
        Task<decimal> GetTotalInventoryValueAsync();

        // Transactions (read-only, immutable)
        Task<InventoryTransactionFilterViewModel> GetTransactionsAsync(string? type = null, int? productId = null, int? locationId = null, DateTime? from = null, DateTime? to = null, string? search = null);
        Task<InventoryTransactionListViewModel?> GetTransactionByIdAsync(int id);
        Task RecordTransactionAsync(int productId, int batchId, int locationId, int userId, string transactionType, decimal quantityDelta, int? referenceId = null, string? notes = null);

        // Adjustments
        Task<List<StockAdjustmentListViewModel>> GetAdjustmentsAsync(string? status = null);
        Task<InventoryAdjustment?> GetAdjustmentByIdAsync(int id);
        Task<(bool Success, string? Error, InventoryAdjustment? Adjustment)> CreateAdjustmentAsync(StockAdjustmentFormViewModel model, int userId);
        Task<(bool Success, string? Error)> ApproveAdjustmentAsync(int adjustmentId, int approverUserId);
        Task<(bool Success, string? Error)> RejectAdjustmentAsync(int adjustmentId, int approverUserId);

        // Transfers
        Task<List<StockTransferListViewModel>> GetTransfersAsync(string? status = null);
        Task<StockTransferDetailsViewModel?> GetTransferDetailsAsync(int id);
        Task<(bool Success, string? Error, StockTransfer? Transfer)> CreateTransferAsync(StockTransferFormViewModel model, int requestedByUserId);
        Task<(bool Success, string? Error)> ApproveTransferAsync(int transferId, int approverUserId);
        Task<(bool Success, string? Error)> DispatchTransferAsync(int transferId, int userId);
        Task<(bool Success, string? Error)> ReceiveTransferAsync(int transferId, int userId);
        Task<(bool Success, string? Error)> CancelTransferAsync(int transferId, int userId);

        // Returns
        Task<List<CustomerReturnListViewModel>> GetReturnsAsync();
        Task<CustomerReturn?> GetReturnByIdAsync(int id);
        Task<(bool Success, string? Error, CustomerReturn? Return)> CreateReturnAsync(CustomerReturnFormViewModel model, int userId);

        // Valuation
        Task<StockValuationViewModel> GetStockValuationAsync();

        // Helpers
        Task<List<ProductBatch>> GetBatchesForProductAsync(int productId);
        Task<List<ProductListViewModel>> GetActiveProductsForDropdownAsync();
        Task EnsureDefaultLocationsAsync();
    }
}

