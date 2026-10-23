using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SuperMarket.Models;

public partial class StockTransfer
{
    [Key]
    [Column("TransferID")]
    public int TransferId { get; set; }

    [Column("FromLocationID")]
    public int FromLocationId { get; set; }

    [Column("ToLocationID")]
    public int ToLocationId { get; set; }

    [Column("RequestedByUserID")]
    public int RequestedByUserId { get; set; }

    [Column("ApprovedByUserID")]
    public int? ApprovedByUserId { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    public DateTime? RequestedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public bool IsDeleted { get; set; }

    [ForeignKey("ApprovedByUserId")]
    [InverseProperty("StockTransferApprovedByUsers")]
    public virtual User? ApprovedByUser { get; set; }

    [ForeignKey("FromLocationId")]
    [InverseProperty("StockTransferFromLocations")]
    public virtual Location FromLocation { get; set; } = null!;

    [ForeignKey("RequestedByUserId")]
    [InverseProperty("StockTransferRequestedByUsers")]
    public virtual User RequestedByUser { get; set; } = null!;

    [InverseProperty("Transfer")]
    public virtual ICollection<StockTransferItem> StockTransferItems { get; set; } = new List<StockTransferItem>();

    [ForeignKey("ToLocationId")]
    [InverseProperty("StockTransferToLocations")]
    public virtual Location ToLocation { get; set; } = null!;
}
