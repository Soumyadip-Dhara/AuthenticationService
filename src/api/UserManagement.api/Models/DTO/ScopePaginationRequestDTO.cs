namespace UserManagement.Models.DTO
{
    public class ScopePaginationRequestDTO
    {
        public int LevelId { get; set; }
        public int Offset { get; set; } = 0;
        public int Limit { get; set; } = 10;
        public string? Filter { get; set; }
        public string? Search { get; set; }
    }
}
