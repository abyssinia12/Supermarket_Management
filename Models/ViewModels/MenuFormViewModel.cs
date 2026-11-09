using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SuperMarket.Models.ViewModels
{
    public class MenuFormViewModel
    {
        public int MenuId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Controller name is required")]
        [StringLength(100, ErrorMessage = "Controller name cannot exceed 100 characters")]
        [Display(Name = "Controller")]
        public string Controller { get; set; } = string.Empty;

        [Required(ErrorMessage = "Action name is required")]
        [StringLength(100, ErrorMessage = "Action name cannot exceed 100 characters")]
        [Display(Name = "Action")]
        public string Action { get; set; } = "Index";

        [Display(Name = "IsMenu")]
        public bool IsMenu { get; set; } = false;

        [Display(Name = "IsTraceable")]
        public bool IsTraceable { get; set; } = false;

        [Display(Name = "IsActive")]
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Menu Category is required")]
        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
        [Display(Name = "Menu Category")]
        public string MenuCategory { get; set; } = "General";

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; } = 0;

        [StringLength(50)]
        [Display(Name = "Icon Class")]
        public string? Icon { get; set; }

        [StringLength(255)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // Dropdown helpers
        public List<string> AvailableControllers { get; set; } = new();
        public List<string> AvailableActions { get; set; } = new();
        public List<string> AvailableCategories { get; set; } = new();
    }
}

