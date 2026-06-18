using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("temp_hrms")]
public partial class TempHrm
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("hrms_id")]
    [StringLength(50)]
    public string? HrmsId { get; set; }

    [Column("name", TypeName = "character varying")]
    public string? Name { get; set; }

    [Column("designation", TypeName = "character varying")]
    public string? Designation { get; set; }

    [Column("mobile")]
    [StringLength(10)]
    public string? Mobile { get; set; }

    [Column("email", TypeName = "character varying")]
    public string? Email { get; set; }
}
