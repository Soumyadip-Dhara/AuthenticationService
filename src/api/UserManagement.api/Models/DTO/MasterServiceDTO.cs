namespace UserManagement.Models.DTO
{
    public class MasterServicesDTO
    {
        public long Id { get; set; }
        public string ServiceName { get; set; }
        public List<ServicePermissionDTO> Permissions { get; set; } = new();
    }

    public class ServicePermissionDTO
    {
        public long AppId { get; set; }
        public bool Enabled { get; set; }
    }
    internal class ServicePermissionFlatRow
    {
        public long Id { get; set; }
        public string ServiceName { get; set; }
        public long AppId { get; set; }
        public bool Enabled { get; set; }
    }
    public class EnableServiceRequestDTO
    {
        public long ServiceId { get; set; }
        public long AppId { get; set; }
        public bool Enabled { get; set; }
    }
    public class AddServiceRequestDTO
    {
        public string ServiceName { get; set; } = string.Empty;
    }


}
