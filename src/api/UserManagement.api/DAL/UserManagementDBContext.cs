using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using UserManagement.api.DAL.Entities;
using UserManagement.DAL.Entities;

namespace UserManagement.DAL;

public partial class UserManagementDBContext : DbContext
{
    public UserManagementDBContext()
    {
    }

    public UserManagementDBContext(DbContextOptions<UserManagementDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ConsumeFailedLog> ConsumeFailedLogs { get; set; }
    public virtual DbSet<ConsumeLog> ConsumeLogs { get; set; }
    public virtual DbSet<MessageQueueFailedLog> MessageQueueFailedLogs { get; set; }
    public virtual DbSet<MessageQueueLog> MessageQueueLogs { get; set; }
    public virtual DbSet<MessageQueue> MessageQueues { get; set; }
    public virtual DbSet<ConsumedAcknowledgementLog> ConsumedAcknowledgementLogs { get; set; }
    public virtual DbSet<PublishedAcknowledgementLog> PublishedAcknowledgementLogs { get; set; }
    public virtual DbSet<UserMaster> UserMasters { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Name=UserManagementDBConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConsumedAcknowledgementLog>(entity =>
        {
            entity.HasKey(e => e.UniqueId).HasName("consumed_acknowledgement_logs_pkey");

            entity.Property(e => e.UniqueId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.ErrorType).IsFixedLength();
            entity.Property(e => e.Status).IsFixedLength();
        });
        modelBuilder.Entity<ConsumeFailedLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("consume_failed_logs_pkey");

            entity.Property(e => e.ActionStatus)
                .HasDefaultValueSql("'PENDING'::bpchar")
                .IsFixedLength();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.FailedType).IsFixedLength();
            entity.Property(e => e.IsRedelivered).HasDefaultValue(false);
        });
        modelBuilder.Entity<ConsumeLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("consume_logs_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.ErrorType).IsFixedLength();
            entity.Property(e => e.Status).IsFixedLength();
        });
        modelBuilder.Entity<MessageQueueFailedLog>(entity =>
        {
            entity.HasKey(e => e.UniqueId).HasName("message_queue_failed_logs_pkey");

            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            entity.Property(e => e.UniqueId).ValueGeneratedNever();
            entity.Property(e => e.ActionStatus)
                .HasDefaultValueSql("'PENDING'::bpchar")
                .IsFixedLength();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasConversion(dateTimeConverter);
            entity.Property(e => e.FailedAt).HasConversion(dateTimeConverter);
            entity.Property(e => e.ResolvedAt).HasConversion(dateTimeConverter);
            entity.Property(e => e.UpdatedAt).HasConversion(dateTimeConverter);
            entity.Property(e => e.FailedType).IsFixedLength();
        });
        modelBuilder.Entity<MessageQueueLog>(entity =>
        {
            entity.HasKey(e => e.UniqueId).HasName("message_queue_logs_pkey");

            entity.Property(e => e.UniqueId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        });
        modelBuilder.Entity<MessageQueue>(entity =>
        {
            entity.HasKey(e => e.UniqueId).HasName("message_queues_pkey");

            entity.Property(e => e.UniqueId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<PublishedAcknowledgementLog>(entity =>
        {
            entity.HasKey(e => e.UniqueId).HasName("published_acknowledgement_log_pkey");

            entity.Property(e => e.UniqueId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        });
        modelBuilder.Entity<UserMaster>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_master_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.DueFirstLogin).HasDefaultValue(true);
            entity.Property(e => e.EffectiveFrom).HasDefaultValueSql("CURRENT_DATE");
            entity.Property(e => e.ExpiresOn).HasDefaultValueSql("(CURRENT_DATE + '1 year'::interval)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsAnAdmin).HasDefaultValue(false);
            entity.Property(e => e.IsBlocked).HasDefaultValue(false);
            entity.Property(e => e.IsOnlyUsermanagement).HasDefaultValue(false);
            entity.Property(e => e.OldId).HasDefaultValue(0L);
            entity.Property(e => e.SignerId).HasDefaultValueSql("''::character varying");
            entity.Property(e => e.TotpEnabled).HasDefaultValue(false);
            entity.Property(e => e.UnsuccessfulLoginAttempt).HasDefaultValue((short)0);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.InverseCreatedByNavigation)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_master_created_by_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
