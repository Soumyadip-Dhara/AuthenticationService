using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UserManagement.BAL.Interfaces;
using UserManagement.DAL;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
using UserManagement.Models.DTO;

namespace UserManagement.DAL.Repositories
{
    public class AuditCertificateRepository  : Repository<SecurityAuditCertificateDetail, UserManagementDBContext>, IAuditCertificateRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly UserManagementDBContext _dbContext;
        private readonly IClaimService _claimService;

        public AuditCertificateRepository(IConfiguration configuration, UserManagementDBContext userManagementDBContext, IClaimService claimService) : base(userManagementDBContext)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("UserManagementDBConnection");
            _dbContext = userManagementDBContext;
            _claimService = claimService;
        }

        public async Task<int> InsertCertificateAsync(CertificateUploadDto dto, string filePath)
        {
            var query = @"
                INSERT INTO public.security_audit_certificate_details
                (serial_no, issue_date, expiry_date, certificate_path, emails, is_active, created_by, created_at, application)
                VALUES (@SerialNo, @IssueDate, @ExpiryDate, @CertificatePath, @Emails, true, @CreatedBy, now(), @Application);
            ";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteAsync(query, new
            {
                dto.SerialNo,
                dto.IssueDate,
                dto.ExpiryDate,
                CertificatePath = filePath,
                dto.Emails,
                dto.CreatedBy,
                dto.Application
            });
        }

        public async Task<int> UpdateCertificateAsync(long id, CertificateUpdateDto dto)
        {
            var updatedBy = _claimService.GetUserId();
            var query = @"
                UPDATE public.security_audit_certificate_details
                SET is_active = @IsActive,
                    updated_by = @UpdatedBy,
                    updated_at = now()
                WHERE id = @Id;
            ";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteAsync(query, new
            {
                Id = id,
                dto.IsActive,
                UpdatedBy = updatedBy
            });
        }

        public async Task<SecurityAuditCertificateDetail?> GetCertificateByIdAsync(long id)
        {
            var query = @"SELECT id, document_id AS DocumentId 
                      FROM public.security_audit_certificate_details
                      WHERE id = @Id;";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<SecurityAuditCertificateDetail>(query, new { Id = id });

        }


        public async Task<IEnumerable<SecurityAuditCertificateDto>> GetAllCertificatesAsync()
        {
            var query = @"
                SELECT c.id, c.serial_no AS SerialNo, c.issue_date AS IssueDate, c.expiry_date AS ExpiryDate,
                       c.emails, c.is_active AS IsActive, a.title AS ApplicationTitle, c.certificate_path AS CertificatePath
                FROM public.security_audit_certificate_details c
                JOIN master.applications a ON c.application = a.id
                ORDER BY c.created_at DESC;
            ";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<SecurityAuditCertificateDto>(query);
        }

        public async Task<SecurityAuditCertificateDto?> GetLatestActiveCertificateAsync(int applicationId)
        {
            var query = @"
                SELECT c.id, c.serial_no AS SerialNo, c.issue_date AS IssueDate, c.expiry_date AS ExpiryDate,
                       c.emails, c.is_active AS IsActive, a.title AS ApplicationTitle, c.document_id AS DocumentId
                FROM public.security_audit_certificate_details c
                JOIN master.applications a ON c.application = a.id
                WHERE c.application = @ApplicationId AND c.is_active = true
                ORDER BY c.issue_date DESC
                LIMIT 1;
            ";

            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<SecurityAuditCertificateDto>(query, new { ApplicationId = applicationId });
        }

        public async Task<IEnumerable<SecurityAuditCertificateDetail>> GetAllWithApplicationAsync()
        {
            return await _dbContext.SecurityAuditCertificateDetails
                .Include(c => c.ApplicationNavigation)   // eager load related Application
                .Select(c => new SecurityAuditCertificateDetail
                {
                    Id = c.Id,
                    SerialNo = c.SerialNo,
                    IssueDate = c.IssueDate,
                    ExpiryDate = c.ExpiryDate,
                    Emails = c.Emails,
                    IsActive = c.IsActive,
                    //CertificatePath = c.CertificatePath,   // now storing file path
                    ApplicationNavigation = c.ApplicationNavigation
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<ApplicationDto>> GetApplicationsAsync()
        {
            return await _dbContext.Applications
                .Where(a => a.Id != 5 && (a.IsActive ?? false))                // exclude id=5 & inactive apps
                .Select(a => new ApplicationDto
                {
                    Id = a.Id,
                    Title = a.Title
                })
                .ToListAsync();
        }

    }
}
