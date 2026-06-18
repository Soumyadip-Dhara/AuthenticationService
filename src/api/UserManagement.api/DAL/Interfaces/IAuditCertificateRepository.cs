//using UserManagement.DAL.Entities;
//using UserManagement.Models.DTO;

//namespace UserManagement.DAL.Interfaces
//{
//    public interface IAuditCertificateRepository : IRepository<SecurityAuditCertificateDetail>
//    {
//        public Task<int> InsertCertificateAsync(
//        string serialNo,
//        DateTime issueDate,
//        DateTime expiryDate,
//        byte[] pdfBytes,
//        string[] emails,
//        int application);
//        public Task<int> UpdateCertificateAsync(
//        long id,
//        string serialNo,
//        DateTime issueDate,
//        DateTime expiryDate,
//        byte[] pdfBytes,
//        string[] emails);

//        public Task<SecurityAuditCertificateDetail> GetByIdAsync(long id);
//        public Task UpdateAsync(SecurityAuditCertificateDetail entity);
//        public Task<IEnumerable<SecurityAuditCertificateDetail>> GetAllWithApplicationAsync();
//        public Task<byte[]?> GetLatestActiveCertificateByAppNameAsync(string applicationName);
//        public Task<IEnumerable<ApplicationDto>> GetApplicationsAsync();
//    }
//}


using UserManagement.DAL.Entities;
using UserManagement.Models.DTO;

namespace UserManagement.DAL.Interfaces
{
    public interface IAuditCertificateRepository :  IRepository<SecurityAuditCertificateDetail>
    {
        Task<int> InsertCertificateAsync(CertificateUploadDto dto, string filePath);
        Task<int> UpdateCertificateAsync(long id, CertificateUpdateDto dto);
        //Task<SecurityAuditCertificateDto?> GetCertificateByIdAsync(long id);
        Task<SecurityAuditCertificateDetail?> GetCertificateByIdAsync(long id);
        Task<IEnumerable<SecurityAuditCertificateDto>> GetAllCertificatesAsync();
        Task<SecurityAuditCertificateDto?> GetLatestActiveCertificateAsync(int applicationId);
        public Task<IEnumerable<SecurityAuditCertificateDetail>> GetAllWithApplicationAsync();
        public Task<IEnumerable<ApplicationDto>> GetApplicationsAsync();
    }
}
