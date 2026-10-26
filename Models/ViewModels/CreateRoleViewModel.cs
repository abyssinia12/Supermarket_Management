using System.ComponentModel.DataAnnotations;

namespace SuperMarket.Models.ViewModels
{
    public class CreateRoleViewModel
    {
        [Required(ErrorMessage = "Role Name is required.")]
        [StringLength(50, ErrorMessage = "Role Name cannot exceed 50 characters.")]
        [Display(Name = "Role Name")]
        public string RoleName { get; set; } = null!;

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
    }
}