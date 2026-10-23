using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SuperMarket.Models;

[Index("LocationName", Name = "UQ__Location__F946BB84AA7806B6", IsUnique = true)]
public partial class Location
{
    [Key]
    [Column("LocationID")]
    public int LocationId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string LocationName { get; set; } = null!;

    [StringLength(20)]
    [Unicode(false)]
    public string LocationType { get; set; } = null!;

    public bool? IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("Location")]
    public virtual ICollection<InventoryAdjustment> InventoryAdjustments { get; set; } = new List<InventoryAdjustment>();

    [InverseProperty("Location")]
    public virtual ICollection<InventoryStock> InventoryStocks { get; set; } = new List<InventoryStock>();

    [InverseProperty("Location")]
    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    [InverseProperty("Location")]
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();

    [InverseProperty("FromLocation")]
    public virtual ICollection<StockTransfer> StockTransferFromLocations { get; set; } = new List<StockTransfer>();

    [InverseProperty("ToLocation")]
    public virtual ICollection<StockTransfer> StockTransferToLocations { get; set; } = new List<StockTransfer>();
}
