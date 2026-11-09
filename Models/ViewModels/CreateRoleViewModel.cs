using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SuperMarket.Models.ViewModels
{
    public class MenuSelectionItemViewModel
    {
        public int MenuId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string MenuCategory { get; set; } = string.Empty;
        public bool IsMenu { get; set; }
        public bool IsTraceable { get; set; }
        public bool IsActive { get; set; }
        public bool IsSelected { get; set; }
    }

    public class CreateRoleViewModel
    {
        [Required(ErrorMessage = "Role title is required.")]
        [StringLength(50, ErrorMessage = "Role title cannot exceed 50 characters.")]
        [Display(Name = "Role Title")]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Assigned Menus")]
        public List<int> SelectedMenuIds { get; set; } = new();

        public List<MenuSelectionItemViewModel> AvailableMenus { get; set; } = new();
    }
}
