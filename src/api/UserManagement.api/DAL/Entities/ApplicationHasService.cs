using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.DAL.Entities;

[Table("application_has_services", Schema = "master")]
public partial class ApplicationHasService
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("service_id")]
    public long ServiceId { get; set; }

    [Column("app_id")]
    public long AppId { get; set; }

    [Column("client_secret")]
    public Guid ClientSecret { get; set; }
}
