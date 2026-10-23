using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SuperMarket.Models;

public partial class SaleItem
{
    [Key]
    [Column("SaleItemID")]
    public int SaleItemId { get; set; }

    [Column("SaleID")]
    public int SaleId { get; set; }

    [Column("ProductID")]
    public int ProductId { get; set; }

    [Column("BatchID")]
    public int BatchId { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Discount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal LineTotal { get; set; }

    [ForeignKey("BatchId")]
    [InverseProperty("SaleItems")]
    public virtual ProductBatch Batch { get; set; } = null!;

    [ForeignKey("ProductId")]
    [InverseProperty("SaleItems")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("SaleId")]
    [InverseProperty("SaleItems")]
    public virtual Sale Sale { get; set; } = null!;
}
