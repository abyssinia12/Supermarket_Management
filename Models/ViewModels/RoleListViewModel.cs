using System;
using System.Collections.Generic;

namespace SuperMarket.Models.ViewModels
{
    public class RoleListViewModel
    {
        public int RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }

        // Displays total active users assigned to this role
        public int UserCount { get; set; }

        // Displays total menus assigned to this role
        public int MenuCount { get; set; }

        // Preview of assigned menus for badges
        public List<string> AssignedMenuTitles { get; set; } = new();
    }
}
