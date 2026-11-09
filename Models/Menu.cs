using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SuperMarket.Models
{
    [Table("Menus")]
    public class Menu
    {
        [Key]
        [Column("MenuId")]
        public int MenuId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Controller name is required")]
        [StringLength(100)]
        [Display(Name = "Controller")]
        public string Controller { get; set; } = string.Empty;

        [Required(ErrorMessage = "Action name is required")]
        [StringLength(100)]
        [Display(Name = "Action")]
        public string Action { get; set; } = "Index";

        [Display(Name = "IsMenu")]
        public bool IsMenu { get; set; } = false;

        [Display(Name = "IsTraceable")]
        public bool IsTraceable { get; set; } = false;

        [Display(Name = "IsActive")]
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Menu Category is required")]
        [StringLength(100)]
        [Display(Name = "Menu Category")]
        public string MenuCategory { get; set; } = "General";

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; } = 0;

        [StringLength(50)]
        [Display(Name = "Icon")]
        public string? Icon { get; set; }

        [StringLength(255)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
    }
}

