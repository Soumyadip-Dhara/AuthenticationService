using UserManagement.DAL.Entities;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Services
{
    public interface IAuditCertificateService
    {
        Task<int> UploadCertificateAsync(CertificateUploadDto dto);
        Task<int> UpdateCertificateAsync(long id, CertificateUpdateDto dto);
        //Task<SecurityAuditCertificateDto?> GetCertificateByIdAsync(long id);
        Task<SecurityAuditCertificateDetail> GetCertificateByIdAsync(long id);
        Task<IEnumerable<SecurityAuditCertificateDto>> GetAllCertificatesAsync();
        Task<LatestActiveUMSecurityAuditCertificateDto> GetLatestActiveCertificateAsync(int applicationId);
        public Task<IEnumerable<SecurityAuditCertificateDto>> GetAllAsync();
        //public Task SendCertificateEmailAsync(string[] recipientEmail, string serialNo, bool isExpiry, IFormFile? attachedFile);
        public Task<IEnumerable<ApplicationDto>> GetApplicationsAsync();
        Task<byte[]> DownloadDocumentFromStorageAsync(Guid documentId);
        Task<byte[]> DownloadCertificate(long id);
    }
}
