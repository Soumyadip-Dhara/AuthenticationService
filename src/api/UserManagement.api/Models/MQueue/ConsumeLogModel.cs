using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace UserManagement.Models.MQueue
{
    public class NewConsumeLogModel
    {
        public Guid? MessageId { get; set; }
        public string QueueName { get; set; } = null!;
        public string? ExchangeName { get; set; }
        public string? RoutingKey { get; set; }
        public string MessageBody { get; set; }
        public string? Status { get; set; }
        public string? ActionStatus { get; set; }
        public string? FailedMessage { get; set; }
        public string? FailedType { get; set; }
        public DateTime FailedAt { get; set; } = DateTime.Now;
        public DateTime ConsumedAt { get; set; } = DateTime.Now;
        public bool IsRedelivered { get; set; } = false;

        public void Reset()
        {
            MessageId = null;
            ExchangeName = null;
            RoutingKey = null;
            MessageBody = null;
            Status = null;
            ActionStatus = null;
            FailedMessage = null;
            FailedType = null;
            ConsumedAt = DateTime.Now;
            FailedAt = DateTime.Now;
            // QueueName is intentionally not reset
        }
    }
    public class abcmodel
    {
        public string a { get; set; }
    }

    //public class UserRegistrationModel
    //{
    //    public Guid queueUniqueId { get; set; } = Guid.NewGuid();

    //    public string user_full_name { get; set; }
    //    public string userid { get; set; }
    //    public string hrms_id { get; set; }
    //    public string role_code { get; set; }
    //    public object permission { get; set; }
    //    public string agency_id { get; set; }
    //    public ExtraParameter extra_parameter { get; set; }
    //    public string designation { get; set; }
    //    public string mobile { get; set; }
    //    public string email { get; set; }
    //    public DateTime valid_from { get; set; }
    //    public DateTime? valid_until { get; set; }
    //    public DateTime created_at { get; set; }
    //    public DateTime updated_at { get; set; }
    //    public string created_by { get; set; }
    //    public string updated_by { get; set; }
    //    public bool is_active { get; set; }
    //    public bool is_disabled { get; set; }
    //}
    //public class ExtraParameter
    //{
    //    public string sls_code { get; set; }
    //    public object optional { get; set; }
    //}


        public class UserRegistrationModel
    {
            public Guid queueUniqueId { get; set; } 

            public string user_full_name { get; set; }
            public string userid { get; set; }
            public string hrms_id { get; set; }

            public List<RoleDetails> privileges { get; set; }

            public string designation { get; set; }
            public string mobile { get; set; }
            public string email { get; set; }

            public DateTime valid_from { get; set; }
            public DateTime? valid_until { get; set; }

            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }

            public string created_by { get; set; }
            public string updated_by { get; set; }

            public bool is_active { get; set; }
            public bool is_disabled { get; set; }
        }

        public class RoleDetails
        {
            public string role_code { get; set; }

            public string agency_id { get; set; }

            public Guid role_uid { get; set; }

            public string sls_code { get; set; }

            public object permission { get; set; }

            public ExtraParameter extraParameter { get; set; }
            public bool is_active { get; set; }
            public bool is_deleted { get; set; }
    }

        public class ExtraParameter
        {
            public string parentagencycode { get; set; }
            public string ddo_code { get; set; }
            public string treas_code { get; set; }
            public string districtcode { get; set; }
            public object optional { get; set; }
        }
    //public class UserRoleMappingDbModel
    //{
    //    public string user_full_name { get; set; }
    //    public string userid { get; set; }
    //    public string hrms_id { get; set; }

    //    public string designation { get; set; }
    //    public string mobile { get; set; }
    //    public string email { get; set; }
    //    public bool is_disabled { get; set; }

    //    public DateTime created_at { get; set; }
    //    public DateTime updated_at { get; set; }

    //    public string created_by { get; set; }
    //    public string updated_by { get; set; }

    //    public bool is_active { get; set; }
    //}

    public class SlsAgencyModel
    {
        public string SlsCode { get; set; }
        public string AgencyCode { get; set; }
        public string AgencyName { get; set; }
        public string ParentAgencyCode { get; set; }
        public bool IsActive { get; set; }
        public string CorrelationId { get; set; }
    }

    public class SnapshotRequestModel 
    { 
        public string? UserId { get; set; }
        public string  QueueUniqueId { get; set; }
        public string Respond_to { get; set; }
        public short Request_type { get; set; }
        public DateTime Requested_at { get; set; }
        public string CorrelationId { get; set; }
    }

   

    


}
