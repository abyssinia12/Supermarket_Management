using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SuperMarket.Models;

[Index("Barcode", Name = "IX_Products_Barcode")]
[Index("Sku", Name = "IX_Products_SKU")]
[Index("Barcode", Name = "UQ__Products__177800D32B54D6C8", IsUnique = true)]
[Index("Sku", Name = "UQ__Products__CA1ECF0D54A30673", IsUnique = true)]
public partial class Product
{
    [Key]
    [Column("ProductID")]
    public int ProductId { get; set; }

    [Column("CategoryID")]
    public int CategoryId { get; set; }

    [Column("SKU")]
    [StringLength(50)]
    [Unicode(false)]
    public string Sku { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string Barcode { get; set; } = null!;

    [StringLength(150)]
    [Unicode(false)]
    public string ProductName { get; set; } = null!;

    [StringLength(20)]
    [Unicode(false)]
    public string UnitOfMeasure { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal PurchaseCost { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SellingPrice { get; set; }

    public int MinStockLevel { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("CategoryId")]
    [InverseProperty("Products")]
    public virtual Category Category { get; set; } = null!;

    [InverseProperty("Product")]
    public virtual ICollection<CustomerReturnItem> CustomerReturnItems { get; set; } = new List<CustomerReturnItem>();

    [InverseProperty("Product")]
    public virtual ICollection<InventoryAdjustment> InventoryAdjustments { get; set; } = new List<InventoryAdjustment>();

    [InverseProperty("Product")]
    public virtual ICollection<InventoryStock> InventoryStocks { get; set; } = new List<InventoryStock>();

    [InverseProperty("Product")]
    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    [InverseProperty("Product")]
    public virtual ICollection<ProductBatch> ProductBatches { get; set; } = new List<ProductBatch>();

    [InverseProperty("Product")]
    public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();

    [InverseProperty("Product")]
    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

    [InverseProperty("Product")]
    public virtual ICollection<StockTransferItem> StockTransferItems { get; set; } = new List<StockTransferItem>();
}
