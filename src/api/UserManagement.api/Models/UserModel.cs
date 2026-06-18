namespace UserManagement.Models
{
    public class UserModel
    {
        public long Id { get; set; }
        public string LoginId { get; set; } = null!;
        public string? HrmsId { get; set; }
        public string Name { get; set; } = null!;
        public byte[] PasswordHash { get; set; } = null!;
        public byte[] PasswordSalt { get; set; } = null!;
        public long? PasswordChangedBy { get; set; }
        public DateTime? PasswordChangedOn { get; set; }
        public int? InvalidLoginCount { get; set; }
        public short? Status { get; set; }
        public string Designation { get; set; } = null!;
        public string MobileNumber { get; set; } = null!;
        public string? Email { get; set; }
        public long? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}