using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SuperMarket.Models;

[Index("ProductId", "LocationId", "Timestamp", Name = "IX_InventoryTransactions_Filter")]
public partial class InventoryTransaction
{
    [Key]
    [Column("TransactionID")]
    public int TransactionId { get; set; }

    [Column("ProductID")]
    public int ProductId { get; set; }

    [Column("BatchID")]
    public int BatchId { get; set; }

    [Column("LocationID")]
    public int LocationId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string TransactionType { get; set; } = null!;

    [Column(TypeName = "decimal(18, 3)")]
    public decimal QuantityDelta { get; set; }

    [Column("ReferenceID")]
    public int? ReferenceId { get; set; }

    public DateTime? Timestamp { get; set; }

    [ForeignKey("BatchId")]
    [InverseProperty("InventoryTransactions")]
    public virtual ProductBatch Batch { get; set; } = null!;

    [ForeignKey("LocationId")]
    [InverseProperty("InventoryTransactions")]
    public virtual Location Location { get; set; } = null!;

    [ForeignKey("ProductId")]
    [InverseProperty("InventoryTransactions")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("InventoryTransactions")]
    public virtual User User { get; set; } = null!;
}
