namespace UserManagement.Models.DTO
{
    public class EmailRequest
    {
        public IEnumerable<string> To { get; set; } = [];
        public IEnumerable<string>? Cc { get; set; }
        public IEnumerable<string>? Bcc { get; set; }

        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;

        public IEnumerable<IFormFile>? Attachments { get; set; }
    }
    public class ServiceDetails
    {
        public string ServiceName { get; set; }
    }


}
