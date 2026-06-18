using System;
using System.Text.Json.Serialization;

namespace UserManagement.Models.MQueue
{
    public class MasterDdoConsumerPayload
    {
        public Guid Id { get; set; }
        public string Table { get; set; } = null!;
        public string Operation { get; set; } = null!;
        public DdoConsumeData Data { get; set; } = null!;
        public string ChangedBy { get; set; } = null!;
    }

    public class DdoConsumeData
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("treasury_code")]
        public string? TreasuryCode { get; set; }

        [JsonPropertyName("ddo_code")]
        public string? DdoCode { get; set; }

        [JsonPropertyName("ddo_type")]
        public string? DdoType { get; set; }

        [JsonPropertyName("valid_upto")]
        public DateTime? ValidUpto { get; set; }

        [JsonPropertyName("designation")]
        public string? Designation { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("phone_no1")]
        public string? PhoneNo1 { get; set; }

        [JsonPropertyName("phone_no2")]
        public string? PhoneNo2 { get; set; }

        [JsonPropertyName("fax")]
        public string? Fax { get; set; }

        [JsonPropertyName("e_mail")]
        public string? Email { get; set; }

        [JsonPropertyName("pin")]
        public string? Pin { get; set; }

        [JsonPropertyName("active_flag")]
        public bool? ActiveFlag { get; set; }

        [JsonPropertyName("created_by_userid")]
        public long? CreatedByUserId { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updated_by_userid")]
        public long? UpdatedByUserId { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("ddo_tan_no")]
        public string? DdoTanNo { get; set; }

        [JsonPropertyName("int_dept_id")]
        public long? IntDeptId { get; set; }

        [JsonPropertyName("office_name")]
        public string? OfficeName { get; set; }

        [JsonPropertyName("station")]
        public string? Station { get; set; }

        [JsonPropertyName("controlling_officer")]
        public string? ControllingOfficer { get; set; }

        [JsonPropertyName("enrolement_no")]
        public string? EnrolementNo { get; set; }

        [JsonPropertyName("nps_registration_no")]
        public string? NpsRegistrationNo { get; set; }

        [JsonPropertyName("int_dept_id_hrms")]
        public string? IntDeptIdHrms { get; set; }

        [JsonPropertyName("gstin")]
        public string? Gstin { get; set; }

        [JsonPropertyName("parent_treasury_code")]
        public string? ParentTreasuryCode { get; set; }

        [JsonPropertyName("ref_code")]
        public int? RefCode { get; set; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("is_deleted")]
        public bool? IsDeleted { get; set; }
    }
}
