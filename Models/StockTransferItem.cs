using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SuperMarket.Models;

public partial class StockTransferItem
{
    [Key]
    [Column("TransferItemID")]
    public int TransferItemId { get; set; }

    [Column("TransferID")]
    public int TransferId { get; set; }

    [Column("ProductID")]
    public int ProductId { get; set; }

    [Column("BatchID")]
    public int BatchId { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public decimal Quantity { get; set; }

    [ForeignKey("BatchId")]
    [InverseProperty("StockTransferItems")]
    public virtual ProductBatch Batch { get; set; } = null!;

    [ForeignKey("ProductId")]
    [InverseProperty("StockTransferItems")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("TransferId")]
    [InverseProperty("StockTransferItems")]
    public virtual StockTransfer Transfer { get; set; } = null!;
}
