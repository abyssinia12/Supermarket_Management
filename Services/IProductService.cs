using System.Collections.Generic;
using System.Threading.Tasks;
using SuperMarket.Models;
using SuperMarket.Models.ViewModels;

namespace SuperMarket.Services
{
    public interface IProductService
    {
        // Products
        Task<ProductIndexFilterViewModel> GetProductsAsync(int? categoryId = null, string? search = null, string? stockFilter = null);
        Task<Product?> GetProductByIdAsync(int id);
        Task<ProductDetailsViewModel?> GetProductDetailsAsync(int id);
        Task<(bool Success, string? ErrorMessage, Product? Product)> CreateProductAsync(ProductFormViewModel model);
        Task<(bool Success, string? ErrorMessage)> UpdateProductAsync(ProductFormViewModel model);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> IsSkuUniqueAsync(string sku, int? currentProductId = null);
        Task<bool> IsBarcodeUniqueAsync(string barcode, int? currentProductId = null);
        string GenerateUniqueSku();
        string GenerateUniqueBarcode();

        // Categories
        Task<List<CategoryFormViewModel>> GetCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, Category? Category)> CreateCategoryAsync(CategoryFormViewModel model);
        Task<(bool Success, string? ErrorMessage)> UpdateCategoryAsync(CategoryFormViewModel model);
        Task<bool> DeleteCategoryAsync(int id);

        // Batches & Stock
        Task<List<BatchItemViewModel>> GetBatchesAsync(int? productId = null);
        Task<BatchItemViewModel?> GetBatchByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, ProductBatch? Batch)> CreateBatchAsync(BatchFormViewModel model);
        Task<(bool Success, string? ErrorMessage)> UpdateBatchAsync(BatchFormViewModel model);
        Task<bool> DeleteBatchAsync(int id);
        Task<List<Location>> GetLocationsAsync();
        Task EnsureDefaultDataAsync();
    }
}

