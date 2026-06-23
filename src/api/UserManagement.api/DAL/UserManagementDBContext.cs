using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
//using UserManagement.Dal.Entities;
using UserManagement.DAL.Entities;
using UserManagement.Models.MQueue;
using UserManagement.Models.DTO;

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
    public virtual DbSet<Service> Services { get; set; }
    public virtual DbSet<UserRoleScopeAppContext> UserRoleScopeAppContexts { get; set; }

    public virtual DbSet<ApplicationHasService> ApplicationHasServices { get; set; }

    public virtual DbSet<UserActivityLog> UserActivityLogs { get; set; }

    public virtual DbSet<Application> Applications { get; set; }

    public virtual DbSet<Application1> Application1s { get; set; }

    public virtual DbSet<Application5> Application5s { get; set; }

    public virtual DbSet<ApplicationHasGlobalLevel> ApplicationHasGlobalLevels { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }
    
    public virtual DbSet<Club5> Club5s { get; set; }

    public virtual DbSet<Country75> Country75s { get; set; }

    public virtual DbSet<Ddo46> Ddo46s { get; set; }

    public virtual DbSet<Department76> Department76s { get; set; }

    [Obsolete("Use ApplicationLevels and LevelMasters instead")]
    public virtual DbSet<Level> Levels { get; set; }

    public virtual DbSet<LevelHasAllowedRole> LevelHasAllowedRoles { get; set; }

    public virtual DbSet<LevelRelationship> LevelRelationships { get; set; }



    public virtual DbSet<Module5> Module5s { get; set; }

    public virtual DbSet<Municipality47> Municipality47s { get; set; }

    public virtual DbSet<PasswordChangeLog> PasswordChangeLogs { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RoleHasPermission> RoleHasPermissions { get; set; }

    public virtual DbSet<RoleRelationship> RoleRelationships { get; set; }

    public virtual DbSet<Schemes47> Schemes47s { get; set; }

    public virtual DbSet<ScopeRelationship> ScopeRelationships { get; set; }

    public virtual DbSet<State47> State47s { get; set; }

    public virtual DbSet<State5> State5s { get; set; }

    public virtual DbSet<State75> State75s { get; set; }

    public virtual DbSet<TempHrm> TempHrms { get; set; }

    public virtual DbSet<TestLevel47> TestLevel47s { get; set; }

    public virtual DbSet<Treasury45> Treasury45s { get; set; }

    public virtual DbSet<Treasury46> Treasury46s { get; set; }

    public virtual DbSet<Treasury5> Treasury5s { get; set; }

    public virtual DbSet<Treasury75> Treasury75s { get; set; }

    public virtual DbSet<UserApplicationHasUserRole> UserApplicationHasUserRoles { get; set; }

    public virtual DbSet<UserHasApplication> UserHasApplications { get; set; }

    public virtual DbSet<UserHasModuleManagement> UserHasModuleManagements { get; set; }

    public virtual DbSet<UserHasUserManagement> UserHasUserManagements { get; set; }

    public virtual DbSet<UserLevelHasUserScope> UserLevelHasUserScopes { get; set; }

    public virtual DbSet<UserManagement1> UserManagement1s { get; set; }

    public virtual DbSet<UserMaster> UserMasters { get; set; }

    public virtual DbSet<UserRoleHasOwnApp> UserRoleHasOwnApps { get; set; }

    public virtual DbSet<UserRoleHasUserLevel> UserRoleHasUserLevels { get; set; }

    public virtual DbSet<UserRoleHasUserPermission> UserRoleHasUserPermissions { get; set; }

    public virtual DbSet<Wbifms1> Wbifms1s { get; set; }

    public virtual DbSet<LevelMaster> LevelMasters { get; set; }
    public virtual DbSet<ApplicationLevel> ApplicationLevels { get; set; }
    public virtual DbSet<ScopeMaster> ScopeMasters { get; set; }
    public virtual DbSet<ApplicationScope> ApplicationScopes { get; set; }

    public virtual DbSet<Otp> Otps { get; set; }

    public virtual DbSet<Notice> Notices { get; set; }

    public virtual DbSet<SecurityAuditCertificateDetail> SecurityAuditCertificateDetails { get; set; }
    public virtual DbSet<OtpLog> OtpLogs { get; set; }

    public virtual DbSet<ConsumeFailedLog> ConsumeFailedLogs { get; set; }
    public virtual DbSet<ConsumeLog> ConsumeLogs { get; set; }
    public virtual DbSet<MessageQueueFailedLog> MessageQueueFailedLogs { get; set; }
    public virtual DbSet<MessageQueueLog> MessageQueueLogs { get; set; }
    public virtual DbSet<MessageQueue> MessageQueues { get; set; }
    public virtual DbSet<ConsumedAcknowledgementLog> ConsumedAcknowledgementLogs { get; set; }
    public virtual DbSet<PublishedAcknowledgementLog> PublishedAcknowledgementLogs { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Name=UserManagementDBConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<LevelMaster>(entity =>
        {
            entity.HasKey(e => e.LevelId).HasName("level_master_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsActive).HasDefaultValueSql("true");
            entity.Property(e => e.IsDeleted).HasDefaultValueSql("false");
            entity.Property(e => e.IsGlobal).HasDefaultValueSql("false");

            entity.HasOne(d => d.ParentLevel).WithMany(p => p.InverseParentLevel).HasConstraintName("level_master_parent_level_id_fkey");
        });
        modelBuilder.Entity<ApplicationLevel>(entity =>
        {
            entity.HasIndex(e => new { e.AppId, e.LevelId }).IsUnique();
        });

        modelBuilder.Entity<ApplicationScope>(entity =>
        {
            entity.HasIndex(e => new { e.AppId, e.LevelId, e.ScopeId }).IsUnique();
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("services_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<ApplicationHasService>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("application_has_services_pkey");
        });

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("applications_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValueSql("true");
            entity.Property(e => e.MaintenanceMsg).HasDefaultValueSql("'Application Under Maintenance.'::text");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Applications).HasConstraintName("applications_created_by_fkey");
        });

        modelBuilder.Entity<Application1>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.LevelId }).HasName("APPLICATION_1_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        });
        modelBuilder.Entity<UserActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_activity_log_pkey");
            entity.HasOne(d => d.User).WithMany(p => p.UserActivityLogs)
                            .OnDelete(DeleteBehavior.ClientSetNull)
                            .HasConstraintName("user_activity_log_user_id_fkey");
            entity.Property(e => e.ActivityTime).HasDefaultValueSql("now()");
            entity.Property(e => e.PrivateIp).HasDefaultValueSql("''::character varying");
        });
        modelBuilder.Entity<Application5>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.LevelId }).HasName("APPLICATION_5_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<ApplicationHasGlobalLevel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("application_has_global_levels_pkey");
        });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.ChangeTimestamp }).HasName("audit_log_pkey");
            });

            modelBuilder.Entity<Club5>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.LevelId }).HasName("Club_5_pkey");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<Country75>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.LevelId }).HasName("Country_75_pkey");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<Ddo46>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.LevelId }).HasName("DDO_46_pkey");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<Department76>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.LevelId }).HasName("Department_76_pkey");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<Level>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("level_pkey");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.level_id_seq'::regclass)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

                entity.HasOne(d => d.Application).WithMany(p => p.Levels).HasConstraintName("level_application_fkey");

                entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Levels)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("levels_created_by_fkey");
            });

            modelBuilder.Entity<LevelHasAllowedRole>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("level_has_allowed_roles_pkey");

                entity.HasOne(d => d.Level).WithMany(p => p.LevelHasAllowedRoles).HasConstraintName("level_has_allowed_roles_level_id_fkey");

                entity.HasOne(d => d.Role).WithMany(p => p.LevelHasAllowedRoles).HasConstraintName("level_has_allowed_roles_role_id_fkey");
            });

            modelBuilder.Entity<LevelRelationship>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("level_relationships_pkey");

                entity.HasOne(d => d.AccessLevel).WithMany(p => p.LevelRelationshipAccessLevels).HasConstraintName("level_relationships_access_id_fkey");

                entity.HasOne(d => d.Level).WithMany(p => p.LevelRelationshipLevels).HasConstraintName("level_relationships_level_id_fkey");
            });



            modelBuilder.Entity<Module5>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.LevelId }).HasName("Module_5_pkey");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<Municipality47>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.LevelId }).HasName("Municipality_47_pkey");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<PasswordChangeLog>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("password_change_log_pkey");

                entity.Property(e => e.PasswordChangeLog1).HasDefaultValueSql("now()");

                entity.HasOne(d => d.User).WithMany(p => p.PasswordChangeLogs)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("password_change_log_user_id_fkey");
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

            modelBuilder.Entity<Schemes47>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.LevelId }).HasName("SCHEME_47_pkey");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<ScopeRelationship>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("scope_relationships_pkey");

                entity.HasOne(d => d.OwnScopeLevel).WithMany(p => p.ScopeRelationshipOwnScopeLevels)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("scope_relationships_own_scope_level_id_fkey");

                entity.HasOne(d => d.ParentScopeLevel).WithMany(p => p.ScopeRelationshipParentScopeLevels)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("scope_relationships_parent_scope_level_id_fkey");
            });

            modelBuilder.Entity<State47>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.LevelId }).HasName("State_47_pkey");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<State5>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.LevelId }).HasName("STATE_5_pkey");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<State75>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.LevelId }).HasName("state_75_pkey");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<TempHrm>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("temp_hrms_pkey");

                entity.Property(e => e.Mobile).IsFixedLength();
            });

            modelBuilder.Entity<TestLevel47>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.LevelId }).HasName("TestLevel_47_pkey");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<Treasury45>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.LevelId }).HasName("Treasury_45_pkey");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<Treasury46>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.LevelId }).HasName("Treasury_46_pkey");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<Treasury5>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.LevelId }).HasName("Treasury_5_pkey");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<Treasury75>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.LevelId }).HasName("Treasury_75_pkey");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
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

                // FK now points to application_level (app_level_id) instead of master.levels
                entity.HasOne(d => d.Level).WithMany(p => p.UserLevelHasUserScopes).HasConstraintName("user_level_has_user_scope_level_id_fkey");

                entity.HasOne(d => d.UserRoleHasLevel).WithMany(p => p.UserLevelHasUserScopes).HasConstraintName("user_level_has_user_scope_user_role_has_level_id_fkey");
            });

            modelBuilder.Entity<UserManagement1>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.LevelId }).HasName("User Management_1_pkey");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            });


            modelBuilder.Entity<UserMaster>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("user_master_pkey");

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
                entity.Property(e => e.DueFirstLogin).HasDefaultValueSql("true");
                entity.Property(e => e.EffectiveFrom).HasDefaultValueSql("CURRENT_DATE");
                entity.Property(e => e.ExpiresOn).HasDefaultValueSql("(CURRENT_DATE + '1 year'::interval)");
                entity.Property(e => e.IsActive).HasDefaultValueSql("true");

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

                // FK now points to application_level (app_level_id) instead of master.levels
                entity.HasOne(d => d.RoleHasLevel).WithMany(p => p.UserRoleHasUserLevels).HasConstraintName("user_role_has_user_level_role_has_level_id_fkey");

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

            modelBuilder.Entity<Wbifms1>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.LevelId }).HasName("WBIFMS_1_pkey");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('master.scopes_id_seq'::regclass)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            });
            modelBuilder.Entity<SecurityAuditCertificateDetail>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("security_audit_certificate_details_pkey");

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
                entity.Property(e => e.IsActive).HasDefaultValueSql("true");

                entity.HasOne(d => d.ApplicationNavigation).WithMany(p => p.SecurityAuditCertificateDetails)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("security_audit_certificate_details_application_fkey");
            });

            modelBuilder.Entity<Notice>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("notice_pkey");

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
                entity.Property(e => e.IsActive).HasDefaultValueSql("true");
            });
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

            modelBuilder.Entity<UserRoleScopeAppContext>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("user_role_scope_app_context_pkey");

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<OtpLog>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("otp_log_pkey");
            });
            modelBuilder.HasSequence("audit_log_id_seq", "log");

            OnModelCreatingPartial(modelBuilder);

            modelBuilder.HasSequence("audit_log_id_seq", "log");

        modelBuilder.Entity<UserProfileQueryModel>().HasNoKey(); //check 

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
