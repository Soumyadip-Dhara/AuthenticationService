namespace UserManagement.Models
{
    public class RoleModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public int ApplicationId { get; set; }
        public int ScopeTypeId { get; set; }
        public long? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}