using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SuperMarket.Models;

public partial class PurchaseOrderItem
{
    [Key]
    [Column("POItemID")]
    public int PoitemId { get; set; }

    [Column("PurchaseOrderID")]
    public int PurchaseOrderId { get; set; }

    [Column("ProductID")]
    public int ProductId { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public decimal OrderedQuantity { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public decimal ReceivedQuantity { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UnitPrice { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("PurchaseOrderItems")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("PurchaseOrderId")]
    [InverseProperty("PurchaseOrderItems")]
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
}
