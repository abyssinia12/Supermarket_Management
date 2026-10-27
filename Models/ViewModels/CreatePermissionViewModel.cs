using System.ComponentModel.DataAnnotations;

namespace SuperMarket.Models.ViewModels
{
    public class CreatePermissionViewModel
    {
        [Required(ErrorMessage = "Permission Code is required.")]
        [StringLength(100, ErrorMessage = "Permission Code cannot exceed 100 characters.")]
        [Display(Name = "Permission Code")]
        public string PermissionCode { get; set; } = null!;

        [Required(ErrorMessage = "Please select a Module Name.")]
        [Display(Name = "Module Name")]
        public string ModuleName { get; set; } = null!;

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }
    }
}