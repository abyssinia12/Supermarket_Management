using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SuperMarket.Models.ViewModels
{
    public class EditRoleViewModel
    {
        [Required]
        public int RoleId { get; set; }

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
