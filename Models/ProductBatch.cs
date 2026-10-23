using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SuperMarket.Models;

[Index("ProductId", "ExpirationDate", Name = "IX_ProductBatches_FEFO")]
public partial class ProductBatch
{
    [Key]
    [Column("BatchID")]
    public int BatchId { get; set; }

    [Column("ProductID")]
    public int ProductId { get; set; }

    [Column("PurchaseOrderID")]
    public int? PurchaseOrderId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string BatchNumber { get; set; } = null!;

    public DateOnly? ManufactureDate { get; set; }

    public DateOnly? ExpirationDate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UnitCost { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool IsActive { get; set; }

    [InverseProperty("Batch")]
    public virtual ICollection<CustomerReturnItem> CustomerReturnItems { get; set; } = new List<CustomerReturnItem>();

    [InverseProperty("Batch")]
    public virtual ICollection<InventoryAdjustment> InventoryAdjustments { get; set; } = new List<InventoryAdjustment>();

    [InverseProperty("Batch")]
    public virtual ICollection<InventoryStock> InventoryStocks { get; set; } = new List<InventoryStock>();

    [InverseProperty("Batch")]
    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    [ForeignKey("ProductId")]
    [InverseProperty("ProductBatches")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("PurchaseOrderId")]
    [InverseProperty("ProductBatches")]
    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    [InverseProperty("Batch")]
    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

    [InverseProperty("Batch")]
    public virtual ICollection<StockTransferItem> StockTransferItems { get; set; } = new List<StockTransferItem>();
}
