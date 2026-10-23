using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SuperMarket.Models;

[Index("TableName", "RecordId", Name = "IX_AuditLogs_Table_Record")]
public partial class AuditLog
{
    [Key]
    [Column("AuditLogID")]
    public int AuditLogId { get; set; }

    [Column("UserID")]
    public int? UserId { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string TableName { get; set; } = null!;

    [StringLength(20)]
    [Unicode(false)]
    public string OperationType { get; set; } = null!;

    [Column("RecordID")]
    public int RecordId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    [Column("IPAddress")]
    [StringLength(45)]
    [Unicode(false)]
    public string? Ipaddress { get; set; }

    public DateTime? Timestamp { get; set; }
}
