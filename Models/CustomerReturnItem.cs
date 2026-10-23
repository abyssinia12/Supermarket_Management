using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SuperMarket.Models;

public partial class CustomerReturnItem
{
    [Key]
    [Column("ReturnItemID")]
    public int ReturnItemId { get; set; }

    [Column("ReturnID")]
    public int ReturnId { get; set; }

    [Column("ProductID")]
    public int ProductId { get; set; }

    [Column("BatchID")]
    public int BatchId { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public decimal Quantity { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string RestockCondition { get; set; } = null!;

    [ForeignKey("BatchId")]
    [InverseProperty("CustomerReturnItems")]
    public virtual ProductBatch Batch { get; set; } = null!;

    [ForeignKey("ProductId")]
    [InverseProperty("CustomerReturnItems")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("ReturnId")]
    [InverseProperty("CustomerReturnItems")]
    public virtual CustomerReturn Return { get; set; } = null!;
}
