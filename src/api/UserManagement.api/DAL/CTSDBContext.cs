using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using UserManagement.DAL.Entities;

namespace UserManagement.DAL;

public partial class CTSDBContext : DbContext
{
    public CTSDBContext()
    {
    }

    public CTSDBContext(DbContextOptions<CTSDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<UserSessionActivityAuditV> UserSessionActivityAuditView { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Name=ConnectionStrings:CommonLogDBConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSessionActivityAuditV>(entity =>
        {
            entity.ToView("user_session_activity_audit_v", "cts_log");
        });
        modelBuilder.HasSequence("cheque_master_id_seq", "cts_cheque");
        modelBuilder.HasSequence("discount_details_discount_id_seq", "cts_stamp");
        modelBuilder.HasSequence("rbi_cn_files_id_seq", "ag");
        modelBuilder.HasSequence("rbi_dn_files_id_seq", "ag");
        modelBuilder.HasSequence("rbi_rn_files_id_seq", "ag");
        modelBuilder.HasSequence("stamp_category_stamp_category_id_seq", "cts_stamp");
        modelBuilder.HasSequence("stamp_combination_stamp_combination_id_seq", "cts_stamp");
        modelBuilder.HasSequence("stamp_damage_data_stamp_damage_data_id_seq", "cts_stamp");
        modelBuilder.HasSequence("stamp_indent_data_stamp_indent_data_id_seq", "cts_stamp");
        modelBuilder.HasSequence("stamp_indent_id_seq", "cts_stamp");
        modelBuilder.HasSequence("stamp_inventory_stamp_inventory_id_seq", "cts_stamp");
        modelBuilder.HasSequence("stamp_label_master_label_id_seq", "cts_stamp");
        modelBuilder.HasSequence("stamp_master_transaction_stamp_master_transaction_id_seq", "cts_stamp");
        modelBuilder.HasSequence("stamp_type_denomination_id_seq", "cts_stamp");
        modelBuilder.HasSequence("stamp_valuables_stamp_valuables_id_seq", "cts_stamp");
        modelBuilder.HasSequence("stamp_vendor_request_id_seq", "cts_stamp");
        modelBuilder.HasSequence("stamp_vendor_requisition_id_seq", "cts_stamp");
        modelBuilder.HasSequence("stamp_vendor_vendor_code_seq", "cts_stamp");
        modelBuilder.HasSequence("stamp_wallet_id_seq", "cts_stamp");
        modelBuilder.HasSequence("treasury_stamp_credit_ledger_treasury_stamp_credit_ledger_i_seq", "cts_stamp");
        modelBuilder.HasSequence("treasury_stamp_debit_ledger_treasury_stamp_debit_ledger_id_seq", "cts_stamp");
        modelBuilder.HasSequence("vendor_requisition_approve_vendor_requisition_approve_id_seq", "cts_stamp");
        modelBuilder.HasSequence("vendor_requisition_challan_ge_vendor_requisition_challan_ge_seq", "cts_stamp");
        modelBuilder.HasSequence("vendor_stamp_ledger_vendor_stamp_ledger_id_seq", "cts_stamp");
        modelBuilder.HasSequence("vendor_stamp_requisition_data_vendor_stamp_requisition_data_seq", "cts_stamp");
        modelBuilder.HasSequence("vendor_stamp_requisition_vendor_stamp_requisition_id_seq", "cts_stamp");
        modelBuilder.HasSequence("vendor_type_vendor_type_id_seq", "cts_stamp");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
