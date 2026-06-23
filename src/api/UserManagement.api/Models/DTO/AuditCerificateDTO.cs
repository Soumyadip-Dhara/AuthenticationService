using System.ComponentModel.DataAnnotations;

namespace UserManagement.Models.DTO
{
    public class CertificateUploadDto
    {
        [Required]
        public string SerialNo { get; set; }

        [Required]
        public DateTime IssueDate { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public IFormFile CertificatePdf { get; set; }

        public string[] Emails { get; set; }
        public int Application { get; set; }
        public long CreatedBy { get; set; }
    }

    public class CertificateUpdateDto
    {
        public bool? IsActive { get; set; }
        public long UpdatedBy { get; set; }
    }

    public class SecurityAuditCertificateDto
    {
        public long Id { get; set; }
        public string SerialNo { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string[]? Emails { get; set; }
        public bool? IsActive { get; set; }
        public string ApplicationTitle { get; set; }   // from Application table
        public Guid? DocumentId { get; set; }    // file path instead of bytea
    }

    public class LatestActiveUMSecurityAuditCertificateDto
    {
        public string PdfBase64 { get; set; } = string.Empty;
    }

    public class ApplicationDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }

    public class EmailRequestDto
    {
        public string[] to { get; set; }
        public string subject { get; set; }
        public string body { get; set; }
        //public List<AttachmentDto> Attachments { get; set; } = new();
    }

    public class AttachmentDto
    {
        public string FileName { get; set; }
        public string FileContentBase64 { get; set; }
    }

    public class FileDto
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] FileContent { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
    }
    public class DocumentUploadResponse
    {
        public Guid DocumentId { get; set; }
        public string Hash { get; set; }
        public string FileName { get; set; }
        public string Status { get; set; }
    }

    public class DocumentApiResponse<T>
    {
        public T Result { get; set; }
        public int ApiResponseStatus { get; set; }
        public string Message { get; set; }
    }


}





