using System;
using System.Collections.Generic;
using AuthService.IdP.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.IdP.DAL;

public partial class IDPDBContext : DbContext
{
    public IDPDBContext()
    {
    }

    public IDPDBContext(DbContextOptions<IDPDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AppUser> AppUsers { get; set; }

    public virtual DbSet<OpenIddictApplication> OpenIddictApplications { get; set; }

    public virtual DbSet<OpenIddictAuthorization> OpenIddictAuthorizations { get; set; }

    public virtual DbSet<OpenIddictScope> OpenIddictScopes { get; set; }

    public virtual DbSet<OpenIddictToken> OpenIddictTokens { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Name=DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<OpenIddictAuthorization>(entity =>
        {
            entity.HasOne(d => d.Application).WithMany(p => p.OpenIddictAuthorizations).HasConstraintName("FK_OpenIddictAuthorizations_OpenIddictApplications_Application~");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
