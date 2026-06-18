namespace UserManagement.Models.DTO
{
    public class GroupItemDTO
    {
        public string Label { get; set; }
        public string? Value { get; set; }
        public List<ItemDTO> Items { get; set; }
    }
    public class ItemDTO
    {
        public string Label { get; set; }
        public int ParentId { get; set; }
        public string Value { get; set; }
    }
}
