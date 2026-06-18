using System.Text.Json.Serialization;

namespace UserManagement.Models.MQueue
{

    public class MasterTreasuryConsumerPayload
    {
        public Guid Id { get; set; }
        public string Table { get; set; } = null!;
        public string Operation { get; set; } = null!;
        public TreasuryConsumeData Data { get; set; }
        public string ChangedBy { get; set; } = null!;
    }
    public class TreasuryConsumeData
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("treasury_name")]
        public string? TreasuryName { get; set; }

        [JsonPropertyName("district_code")]
        public string? DistrictCode { get; set; }

        [JsonPropertyName("treasury_srl_number")]
        public string? TreasurySrlNumber { get; set; }

        [JsonPropertyName("officer_user_id")]
        public long? OfficerUserId { get; set; }

        [JsonPropertyName("officer_name")]
        public string? OfficerName { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("address1")]
        public string? Address1 { get; set; }

        [JsonPropertyName("address2")]
        public string? Address2 { get; set; }

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

        [JsonPropertyName("ref_code")]
        public int? RefCode { get; set; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("is_deleted")]
        public bool? IsDeleted { get; set; }

        [JsonPropertyName("active_flag")]
        public bool? ActiveFlag { get; set; }

        [JsonPropertyName("created_by_userid")]
        public long? CreatedByUseridAlt { get; set; }

        [JsonPropertyName("createdby_user_id")]
        public long? CreatedByUserId { get; set; }

        [JsonPropertyName("updated_by_userid")]
        public long? UpdatedByUseridAlt { get; set; }

        [JsonPropertyName("updatedby_user_id")]
        public long? UpdatedByUserId { get; set; }

        [JsonPropertyName("int_treasury_code")]
        public string? IntTreasuryCode { get; set; }

        [JsonPropertyName("treasury_status")]
        public string? TreasuryStatus { get; set; }

        [JsonPropertyName("int_ddo_id")]
        public int? IntDdoId { get; set; }

        [JsonPropertyName("debt_acct_no")]
        public string? DebtAcctNo { get; set; }

        [JsonPropertyName("nps_registration_no")]
        public string? NpsRegistrationNo { get; set; }

        [JsonPropertyName("pension_flag")]
        public string? PensionFlag { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
        
        [JsonPropertyName("created_by")]
        public string? CreatedBy { get; set; }
        
        [JsonPropertyName("updated_by")]
        public string? UpdatedBy { get; set; }
    }
}

