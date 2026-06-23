namespace UserManagement.Models.DTO
{
    public class Filter
    {
        public string? Value { get; set; }
        public string MatchMode { get; set; } = string.Empty;
    }

    public class FilterData
    {
        public int First { get; set; }
        public int Rows { get; set; }
        public Dictionary<string, Filter> Filters { get; set; } = new Dictionary<string, Filter>();
        public string? ScopeValue { get; set; } = "";
        public string GlobalFilter { get; set; } = string.Empty;
    }
}
