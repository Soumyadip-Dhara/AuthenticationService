

namespace UserManagement.Models.DTO
{
    public class UserCsvRowDTO
    {
        public int ID { get; set; }
        public string? HrmsId { get; set; }
        public string Name { get; set; } = null!;
        public string? Email { get; set; }
        public string Username { get; set; } = null!;
        public string IsActive { get; set; } = "N";
        public string Designation { get; set; } = null!;
        public string IsAnAdmin { get; set; } = "N";
        public long? MobileNumber { get; set; }
        public string AuthorityCode { get; set; } = null!;
        public string Role { get; set; } = "";
        public string Level { get; set; } = "";
        public string AppName { get; set; } = "";   
        public string? Optional { get; set; }


    }


}




