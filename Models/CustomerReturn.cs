using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SuperMarket.Models;

public partial class CustomerReturn
{
    [Key]
    [Column("ReturnID")]
    public int ReturnId { get; set; }

    [Column("SaleID")]
    public int SaleId { get; set; }

    [Column("ProcessedByUserID")]
    public int ProcessedByUserId { get; set; }

    public DateTime? ReturnDate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RefundAmount { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string Reason { get; set; } = null!;

    [InverseProperty("Return")]
    public virtual ICollection<CustomerReturnItem> CustomerReturnItems { get; set; } = new List<CustomerReturnItem>();

    [ForeignKey("ProcessedByUserId")]
    [InverseProperty("CustomerReturns")]
    public virtual User ProcessedByUser { get; set; } = null!;

    [ForeignKey("SaleId")]
    [InverseProperty("CustomerReturns")]
    public virtual Sale Sale { get; set; } = null!;
}
