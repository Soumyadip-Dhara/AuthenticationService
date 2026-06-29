using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Entities;
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


    public virtual DbSet<Application> Applications { get; set; }

    public virtual DbSet<ApplicationHasGlobalLevel> ApplicationHasGlobalLevels { get; set; }

    public virtual DbSet<ApplicationHasService> ApplicationHasServices { get; set; }

    public virtual DbSet<ApplicationLevel> ApplicationLevels { get; set; }

    public virtual DbSet<ApplicationScope> ApplicationScopes { get; set; }

    public virtual DbSet<LevelHasAllowedRole> LevelHasAllowedRoles { get; set; }

    public virtual DbSet<LevelMaster> LevelMasters { get; set; }

    public virtual DbSet<LevelRelationship> LevelRelationships { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RoleHasPermission> RoleHasPermissions { get; set; }

    public virtual DbSet<RoleRelationship> RoleRelationships { get; set; }

    public virtual DbSet<ScopeMaster> ScopeMasters { get; set; }

    public virtual DbSet<ScopeRelationship> ScopeRelationships { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<UserApplicationHasUserRole> UserApplicationHasUserRoles { get; set; }

    public virtual DbSet<UserHasApplication> UserHasApplications { get; set; }

    public virtual DbSet<UserHasModuleManagement> UserHasModuleManagements { get; set; }

    public virtual DbSet<UserHasUserManagement> UserHasUserManagements { get; set; }

    public virtual DbSet<UserLevelHasUserScope> UserLevelHasUserScopes { get; set; }

    public virtual DbSet<UserMaster> UserMasters { get; set; }

    public virtual DbSet<UserRoleHasOwnApp> UserRoleHasOwnApps { get; set; }

    public virtual DbSet<UserRoleHasUserLevel> UserRoleHasUserLevels { get; set; }

    public virtual DbSet<UserRoleHasUserPermission> UserRoleHasUserPermissions { get; set; }

    public virtual DbSet<UserRoleScopeAppContext> UserRoleScopeAppContexts { get; set; }



    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Name=UserManagementDBConnection");
        }
    }

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


        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("applications_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsConsumingData).HasDefaultValue(true);
            entity.Property(e => e.IsMultiAdminDisallowed).HasDefaultValue(true);
            entity.Property(e => e.IsUnderMaintenance).HasDefaultValue(false);
            entity.Property(e => e.IsUseUserManagement).HasDefaultValue(true);
            entity.Property(e => e.MaintenanceMsg).HasDefaultValueSql("'Application Under Maintenance.'::text");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Applications).HasConstraintName("applications_created_by_fkey");
        });

        modelBuilder.Entity<ApplicationHasGlobalLevel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("application_has_global_levels_pkey");
        });

        modelBuilder.Entity<ApplicationHasService>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("application_has_services_pkey");

            entity.HasOne(d => d.Service).WithMany(p => p.ApplicationHasServices)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("application_has_services_service_id_fkey");
        });

        modelBuilder.Entity<ApplicationLevel>(entity =>
        {
            entity.HasKey(e => e.AppLevelId).HasName("application_level_pkey");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasOne(d => d.App).WithMany(p => p.ApplicationLevels).HasConstraintName("application_level_app_id_fkey");

            entity.HasOne(d => d.Level).WithMany(p => p.ApplicationLevels).HasConstraintName("application_level_level_id_fkey");
        });

        modelBuilder.Entity<ApplicationScope>(entity =>
        {
            entity.HasKey(e => e.AppScopeId).HasName("application_scope_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsAdminCreated).HasDefaultValue(false);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.Status).HasDefaultValue((short)2);

            entity.HasOne(d => d.App).WithMany(p => p.ApplicationScopes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("application_scope_app_id_fkey");

            entity.HasOne(d => d.Level).WithMany(p => p.ApplicationScopes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("application_scope_level_id_fkey");

            entity.HasOne(d => d.Scope).WithMany(p => p.ApplicationScopes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("application_scope_scope_id_fkey");
        });

        modelBuilder.Entity<LevelHasAllowedRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("level_has_allowed_roles_pkey");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.IsParentOrAdminRole).HasDefaultValue(false);

            entity.HasOne(d => d.Level).WithMany(p => p.LevelHasAllowedRoles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("level_has_allowed_roles_level_id_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.LevelHasAllowedRoles).HasConstraintName("level_has_allowed_roles_role_id_fkey");
        });

        modelBuilder.Entity<LevelMaster>(entity =>
        {
            entity.HasKey(e => e.LevelId).HasName("level_master_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.IsGlobal).HasDefaultValue(false);

            entity.HasOne(d => d.ParentLevel).WithMany(p => p.InverseParentLevel).HasConstraintName("level_master_parent_level_id_fkey");
        });

        modelBuilder.Entity<LevelRelationship>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("level_relationships_pkey");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasOne(d => d.AccessLevel).WithMany(p => p.LevelRelationshipAccessLevels)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("level_relationships_access_level_id_fkey");

            entity.HasOne(d => d.Level).WithMany(p => p.LevelRelationshipLevels)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("level_relationships_level_id_fkey");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("permissions_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Application).WithMany(p => p.Permissions).HasConstraintName("permission_application_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Permissions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("permissions_created_by_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsOperational).HasDefaultValue(true);

            entity.HasOne(d => d.Application).WithMany(p => p.Roles).HasConstraintName("roles_application_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Roles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("roles_created_by_fkey");
        });

        modelBuilder.Entity<RoleHasPermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("role_has_permission_pkey");

            entity.HasOne(d => d.Permission).WithMany(p => p.RoleHasPermissions).HasConstraintName("role_has_permission_permission_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.RoleHasPermissions).HasConstraintName("role_has_permission_role_fkey");
        });

        modelBuilder.Entity<RoleRelationship>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("role_relationships_pkey");

            entity.HasOne(d => d.AccessRole).WithMany(p => p.RoleRelationshipAccessRoles).HasConstraintName("role_relationships_access_id_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.RoleRelationshipRoles).HasConstraintName("role_relationships_role_id_key");
        });

        modelBuilder.Entity<ScopeMaster>(entity =>
        {
            entity.HasKey(e => e.ScopeId).HasName("scope_master_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.IsGlobal).HasDefaultValue(false);
        });

        modelBuilder.Entity<ScopeRelationship>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("scope_relationships_pkey");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasOne(d => d.ParentScope).WithMany(p => p.ScopeRelationshipParentScopes).HasConstraintName("scope_relationships_own_scope_level_id_fkey");

            entity.HasOne(d => d.Scope).WithMany(p => p.ScopeRelationshipScopes).HasConstraintName("scope_relationships_scope_id_fkey");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("services_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<UserApplicationHasUserRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("application_has_user_role_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"user\".application_has_user_role_id_seq'::regclass)");

            entity.HasOne(d => d.App).WithMany(p => p.UserApplicationHasUserRoles).HasConstraintName("user_application_has_user_role_app_id_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.UserApplicationHasUserRoles).HasConstraintName("application_has_user_role_role_id_fkey");

            entity.HasOne(d => d.UserHasApp).WithMany(p => p.UserApplicationHasUserRoles).HasConstraintName("application_has_user_role_user_has_app_id_fkey");
        });

        modelBuilder.Entity<UserHasApplication>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_has_application_pkey");

            entity.HasOne(d => d.App).WithMany(p => p.UserHasApplications).HasConstraintName("user_has_application_app_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserHasApplications).HasConstraintName("user_has_application_user_id_fkey");
        });

        modelBuilder.Entity<UserHasModuleManagement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_has_module_management_pkey");

            entity.HasOne(d => d.AssignedApp).WithMany(p => p.UserHasModuleManagements).HasConstraintName("user_has_module_management_assigned_app_id_fkey");

            entity.HasOne(d => d.AssignedMmRole).WithMany(p => p.UserHasModuleManagements).HasConstraintName("user_has_module_management_assigned_mm_role_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserHasModuleManagements).HasConstraintName("user_has_module_management_user_id_fkey");
        });

        modelBuilder.Entity<UserHasUserManagement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_has_user_management_pkey");

            entity.HasOne(d => d.AssignedApp).WithMany(p => p.UserHasUserManagements).HasConstraintName("user_has_user_management_assigned_app_id_fkey");

            entity.HasOne(d => d.AssignedUmRole).WithMany(p => p.UserHasUserManagements).HasConstraintName("user_has_user_management_assigned_um_role_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserHasUserManagements).HasConstraintName("user_has_user_management_user_id_fkey");
        });

        modelBuilder.Entity<UserLevelHasUserScope>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_level_has_user_scope_pkey");

            entity.HasOne(d => d.Level).WithMany(p => p.UserLevelHasUserScopes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_level_has_user_scope_level_id_fkey");

            entity.HasOne(d => d.UserRoleHasLevel).WithMany(p => p.UserLevelHasUserScopes).HasConstraintName("user_level_has_user_scope_user_role_has_level_id_fkey");
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

        modelBuilder.Entity<UserRoleHasOwnApp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_role_has_own_app_id_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"user\".user_role_has_own_app_id_id_seq'::regclass)");

            entity.HasOne(d => d.OwnApp).WithMany(p => p.UserRoleHasOwnApps)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("user_role_has_own_app_own_app_id_fkey");

            entity.HasOne(d => d.OwnAppRole).WithMany(p => p.UserRoleHasOwnApps).HasConstraintName("user_role_has_own_app_id_own_app_role_id_fkey");

            entity.HasOne(d => d.UserAppHasRole).WithMany(p => p.UserRoleHasOwnApps).HasConstraintName("user_role_has_own_app_id_user_app_has_role_id_fkey");
        });

        modelBuilder.Entity<UserRoleHasUserLevel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_role_has_user_level_pkey");

            entity.HasOne(d => d.ApplicationHasRole).WithMany(p => p.UserRoleHasUserLevels).HasConstraintName("user_role_has_user_level_application_has_role_id_fkey");

            entity.HasOne(d => d.RoleHasLevel).WithMany(p => p.UserRoleHasUserLevels)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_role_has_user_level_role_has_level_id_fkey");

            entity.HasOne(d => d.UserHasApp).WithMany(p => p.UserRoleHasUserLevels)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("user_role_has_user_level_user_has_app_id_fkey");
        });

        modelBuilder.Entity<UserRoleHasUserPermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_role_has_permission_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('\"user\".user_role_has_permission_id_seq'::regclass)");

            entity.HasOne(d => d.ApplicationHasRole).WithMany(p => p.UserRoleHasUserPermissions).HasConstraintName("user_role_has_permission_application_has_role_id_fkey");

            entity.HasOne(d => d.RoleHasPermission).WithMany(p => p.UserRoleHasUserPermissions).HasConstraintName("user_role_has_user_permission_role_has_permission_id_fkey");
        });

        modelBuilder.Entity<UserRoleScopeAppContext>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_role_scope_app_context_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Application).WithMany(p => p.UserRoleScopeAppContexts).HasConstraintName("fk_user_scope_context_app");

            entity.HasOne(d => d.UserLevelHasScope).WithMany(p => p.UserRoleScopeAppContexts).HasConstraintName("fk_user_scope_context");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
