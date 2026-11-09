using System.Collections.Generic;
using System.Threading.Tasks;
using SuperMarket.Models;
using SuperMarket.Models.ViewModels;

namespace SuperMarket.Services
{
    public interface IMenuService
    {
        Task<List<Menu>> GetAllAsync(string? category = null, string? search = null, bool? isMenuOnly = null);
        Task<Menu?> GetByIdAsync(int id);
        Task<Menu> CreateAsync(Menu menu);
        Task<bool> UpdateAsync(Menu menu);
        Task<bool> DeleteAsync(int id);
        Task<bool> TogglePropertyAsync(int id, string propertyName);
        Task<List<string>> GetCategoriesAsync();
        Task<List<string>> GetAvailableControllersAsync();
        Task<List<string>> GetAvailableActionsAsync();
        Task<int> SyncControllersAsync();
        Task ResetSeedDataAsync();
        Task<MenuIndexViewModel> GetIndexViewModelAsync(string? category, string? search, bool? isMenuOnly);

        // Role-Menu assignment methods
        Task<List<Menu>> GetMenusForRoleAsync(int roleId);
        Task<List<int>> GetMenuIdsForRoleAsync(int roleId);
        Task SetRoleMenusAsync(int roleId, IEnumerable<int> menuIds);
        Task<Dictionary<int, int>> GetRoleMenuCountsAsync();
        Task<Dictionary<int, List<string>>> GetRoleMenuTitlesAsync();
    }
}
