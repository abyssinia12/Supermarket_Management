using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SuperMarket.Models;

[Index("Username", Name = "UQ__Users__536C85E477915851", IsUnique = true)]
[Index("Email", Name = "UQ__Users__A9D1053409B251F7", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("UserID")]
    public int UserId { get; set; }

    [Column("RoleID")]
    public int RoleId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string Username { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string Email { get; set; } = null!;

    [StringLength(255)]
    [Unicode(false)]
    public string PasswordHash { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string FullName { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("ProcessedByUser")]
    public virtual ICollection<CustomerReturn> CustomerReturns { get; set; } = new List<CustomerReturn>();

    [InverseProperty("ApprovedByUser")]
    public virtual ICollection<InventoryAdjustment> InventoryAdjustmentApprovedByUsers { get; set; } = new List<InventoryAdjustment>();

    [InverseProperty("ReportedByUser")]
    public virtual ICollection<InventoryAdjustment> InventoryAdjustmentReportedByUsers { get; set; } = new List<InventoryAdjustment>();

    [InverseProperty("User")]
    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();

    [ForeignKey("RoleId")]
    [InverseProperty("Users")]
    public virtual Role Role { get; set; } = null!;

    [InverseProperty("CashierUser")]
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();

    [InverseProperty("ApprovedByUser")]
    public virtual ICollection<StockTransfer> StockTransferApprovedByUsers { get; set; } = new List<StockTransfer>();

    [InverseProperty("RequestedByUser")]
    public virtual ICollection<StockTransfer> StockTransferRequestedByUsers { get; set; } = new List<StockTransfer>();
}
