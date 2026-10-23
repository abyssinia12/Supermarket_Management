using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SuperMarket.Models;

[Index("InvoiceNumber", Name = "UQ__Sales__D776E98111A065FC", IsUnique = true)]
public partial class Sale
{
    [Key]
    [Column("SaleID")]
    public int SaleId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string InvoiceNumber { get; set; } = null!;

    [Column("CashierUserID")]
    public int CashierUserId { get; set; }

    [Column("LocationID")]
    public int LocationId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalAmount { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string PaymentMethod { get; set; } = null!;

    [StringLength(20)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    public DateTime? SaleTimestamp { get; set; }

    public bool IsDeleted { get; set; }

    [ForeignKey("CashierUserId")]
    [InverseProperty("Sales")]
    public virtual User CashierUser { get; set; } = null!;

    [InverseProperty("Sale")]
    public virtual ICollection<CustomerReturn> CustomerReturns { get; set; } = new List<CustomerReturn>();

    [ForeignKey("LocationId")]
    [InverseProperty("Sales")]
    public virtual Location Location { get; set; } = null!;

    [InverseProperty("Sale")]
    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
