using System.Collections.Generic;

namespace SuperMarket.Models.ViewModels
{
    public class RolePermissionsViewModel
    {
        public int RoleId { get; set; }

        public string RoleName { get; set; } = null!;

        // Checkbox items for the UI
        public List<PermissionSelectionViewModel> Permissions { get; set; } = new List<PermissionSelectionViewModel>();
    }

    public class PermissionSelectionViewModel
    {
        public int PermissionId { get; set; }

        // Mapped to PermissionCode from your Permission.cs model
        public string PermissionCode { get; set; } = null!;

        // Added ModuleName to group or display context cleanly
        public string ModuleName { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsSelected { get; set; }
    }
}