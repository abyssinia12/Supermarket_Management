using System;

namespace SuperMarket.Models.ViewModels
{
    public class RoleListViewModel
    {
        public int RoleId { get; set; }

        public string RoleName { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }

        // Displays total active users assigned to this role
        public int UserCount { get; set; }
    }
}