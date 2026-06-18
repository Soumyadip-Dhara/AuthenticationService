using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("security_audit_certificate_details")]
public partial class SecurityAuditCertificateDetail
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("serial_no")]
    [StringLength(50)]
    public string SerialNo { get; set; } = null!;

    [Column("issue_date", TypeName = "timestamp without time zone")]
    public DateTime IssueDate { get; set; }

    [Column("expiry_date", TypeName = "timestamp without time zone")]
    public DateTime ExpiryDate { get; set; }

    [Column("emails", TypeName = "character varying[]")]
    public string[]? Emails { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    [Column("created_by")]
    public long? CreatedBy { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_by")]
    public long? UpdatedBy { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [Column("application")]
    public int Application { get; set; }

    [Column("document_id")]
    public Guid DocumentId { get; set; }

    [ForeignKey("Application")]
    [InverseProperty("SecurityAuditCertificateDetails")]
    public virtual Application ApplicationNavigation { get; set; } = null!;


   
}
