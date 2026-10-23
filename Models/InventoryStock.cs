using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SuperMarket.Models;

[Table("InventoryStock")]
[Index("LocationId", "ProductId", "Quantity", Name = "IX_InventoryStock_Lookup")]
[Index("BatchId", "LocationId", Name = "UQ_Batch_Location", IsUnique = true)]
public partial class InventoryStock
{
    [Key]
    [Column("InventoryID")]
    public int InventoryId { get; set; }

    [Column("ProductID")]
    public int ProductId { get; set; }

    [Column("BatchID")]
    public int BatchId { get; set; }

    [Column("LocationID")]
    public int LocationId { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public decimal Quantity { get; set; }

    public DateTime? LastUpdated { get; set; }

    [ForeignKey("BatchId")]
    [InverseProperty("InventoryStocks")]
    public virtual ProductBatch Batch { get; set; } = null!;

    [ForeignKey("LocationId")]
    [InverseProperty("InventoryStocks")]
    public virtual Location Location { get; set; } = null!;

    [ForeignKey("ProductId")]
    [InverseProperty("InventoryStocks")]
    public virtual Product Product { get; set; } = null!;
}
