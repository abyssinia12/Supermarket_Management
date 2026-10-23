using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SuperMarket.Models;

public partial class InventoryAdjustment
{
    [Key]
    [Column("AdjustmentID")]
    public int AdjustmentId { get; set; }

    [Column("ProductID")]
    public int ProductId { get; set; }

    [Column("BatchID")]
    public int BatchId { get; set; }

    [Column("LocationID")]
    public int LocationId { get; set; }

    [Column("ReportedByUserID")]
    public int ReportedByUserId { get; set; }

    [Column("ApprovedByUserID")]
    public int? ApprovedByUserId { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public decimal Quantity { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string AdjustmentType { get; set; } = null!;

    [StringLength(255)]
    [Unicode(false)]
    public string Reason { get; set; } = null!;

    [StringLength(20)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("ApprovedByUserId")]
    [InverseProperty("InventoryAdjustmentApprovedByUsers")]
    public virtual User? ApprovedByUser { get; set; }

    [ForeignKey("BatchId")]
    [InverseProperty("InventoryAdjustments")]
    public virtual ProductBatch Batch { get; set; } = null!;

    [ForeignKey("LocationId")]
    [InverseProperty("InventoryAdjustments")]
    public virtual Location Location { get; set; } = null!;

    [ForeignKey("ProductId")]
    [InverseProperty("InventoryAdjustments")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("ReportedByUserId")]
    [InverseProperty("InventoryAdjustmentReportedByUsers")]
    public virtual User ReportedByUser { get; set; } = null!;
}
