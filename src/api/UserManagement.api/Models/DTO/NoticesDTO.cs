namespace UserManagement.Models.DTO
{
    public class NoticeFilterDTO
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public DateTime? FromDate { get; set; } = GetFinancialYearStart();
        public DateTime? ToDate { get; set; } = DateTime.Now;
        public bool? IsActive { get; set; }
        private static DateTime GetFinancialYearStart()
        {
            var today = DateTime.Now;
            int year = today.Month >= 4 ? today.Year : today.Year - 1;

            return new DateTime(year, 4, 1, 0, 0, 0);
        }

    }


    public class NoticeDTO
    {
        public long Id { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool? IsActive { get; set; }
    }

    public class CreateNoticeDTO
    {
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime? ExpiresAt { get; set; }
    }

    public class UpdateNoticeDTO
    {
        public long Id { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }
    }



}
