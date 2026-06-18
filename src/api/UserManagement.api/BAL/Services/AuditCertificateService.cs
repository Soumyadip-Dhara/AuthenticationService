using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using UserManagement.BAL.Interfaces;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Services
{
    public class AuditCertificateService : IAuditCertificateService
    {
        private readonly IAuditCertificateRepository _repository;
        private readonly IWebHostEnvironment _env;
        private readonly IClaimService _claimService;
        private readonly IConfiguration _configuration;
        private readonly ICertificateEmailService _certificateEmailService;

        public AuditCertificateService(IAuditCertificateRepository repository, IWebHostEnvironment env, IClaimService claimService, IConfiguration configuration, ICertificateEmailService certificateEmailService)

        {
            _repository = repository;
            _env = env;
            _claimService = claimService;
            _configuration = configuration;
            _certificateEmailService = certificateEmailService;
        }

        public async Task<int> UploadCertificateAsync(CertificateUploadDto dto)
        {
            // 1. Upload document to Document Vault FIRST
            var documentId = await UploadToDocumentStorageAsync(dto.CertificatePdf);

            if (documentId == Guid.Empty)
                throw new ApplicationException("Document upload failed. Aborting DB insert.");

            // 2. Persist metadata only
            var certificate = new SecurityAuditCertificateDetail
            {
                SerialNo = dto.SerialNo,
                IssueDate = dto.IssueDate,
                ExpiryDate = dto.ExpiryDate,
                Application = dto.Application,
                Emails = dto.Emails,
                IsActive = true,
                DocumentId = documentId, // <-- IMPORTANT
                CreatedBy = _claimService.GetUserId(),
                CreatedAt = DateTime.Now
            };

            _repository.Add(certificate);
            _repository.SaveChangesManaged();

            // 3. Email 
            await _certificateEmailService.SendCertificateEmailAsync(
                dto.Emails,
                dto.SerialNo,
                false,
                dto.CertificatePdf
            );

            return 1;
        }


        public async Task<int> UpdateCertificateAsync(long id, CertificateUpdateDto dto)
        {
            return await _repository.UpdateCertificateAsync(id, dto);
        }

        public async Task<SecurityAuditCertificateDetail?> GetCertificateByIdAsync(long id)
        {
            return await _repository.GetCertificateByIdAsync(id);
        }
        public async Task<byte[]> DownloadCertificate(long id)
        {
             var res = await _repository.GetCertificateByIdAsync(id);
             var doc = await DownloadDocumentFromStorageAsync(res.DocumentId);
            return doc;
        }

        public async Task<IEnumerable<SecurityAuditCertificateDto>> GetAllCertificatesAsync()
        {
            return await _repository.GetAllCertificatesAsync();
        }

        public async Task<LatestActiveUMSecurityAuditCertificateDto> GetLatestActiveCertificateAsync(int applicationId)
        {
            var cert = await _repository.GetLatestActiveCertificateAsync(applicationId);
            if (cert == null || cert.DocumentId == null )
                return null;

           
            // Fetch document from Document Storage
            byte[] fileBytes = await DownloadDocumentFromStorageAsync(cert.DocumentId.Value);
            return new LatestActiveUMSecurityAuditCertificateDto
            {
                PdfBase64 = Convert.ToBase64String(fileBytes)
            };
        }
        public async Task<IEnumerable<SecurityAuditCertificateDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllWithApplicationAsync();

            return entities.Select(e => new SecurityAuditCertificateDto
            {
                Id = e.Id,
                SerialNo = e.SerialNo,
                IssueDate = e.IssueDate,
                ExpiryDate = e.ExpiryDate,
                Emails = e.Emails,
                IsActive = e.IsActive,
                ApplicationTitle = e.ApplicationNavigation.Title
            });
        }

        public async Task<IEnumerable<ApplicationDto>> GetApplicationsAsync()
        {
            return await _repository.GetApplicationsAsync();
        }



        //public async Task SendCertificateEmailAsync(
        //string[] recipientEmail,
        //string serialNo,
        //bool isExpiry,
        //IFormFile? attachedFile)
        //    {
        //        try
        //        {
        //            using var httpClient = new HttpClient();

        //            string subject = isExpiry ? "Certificate Expired" : "Certificate Issued";

        //            string body = isExpiry
        //                ? @"<html>
        //                 <body>
        //                <p>Dear Sir/Madam,</p>
        //                <p>Your certificate (Serial No: <b>" + serialNo + @"</b>) has expired.</p>
        //                <p>Please renew your certificate to continue using the services.</p>
        //                <p>Best Regards,<br/>IFMS Team</p>
        //                </body>
        //                </html>"
        //                                    : @"<html>
        //                <body>
        //                <p>Dear Sir/Madam,</p>
        //                <p>Your certificate (Serial No: <b>" + serialNo + @"</b>) has been issued successfully.</p>
        //                <p>Please find the attached PDF.</p>
        //                <p>Best Regards,<br/>IFMS Team</p>
        //                </body>
        //                </html>";

        //            var form = new MultipartFormDataContent();


        //            form.Add(new StringContent(string.Join(",", recipientEmail)), "To");
        //            form.Add(new StringContent(subject), "Subject");
        //            form.Add(new StringContent(body), "Body");


        //            if (attachedFile != null && attachedFile.Length > 0)
        //            {
        //                var fileContent = new StreamContent(attachedFile.OpenReadStream());
        //                fileContent.Headers.ContentType =
        //                    new System.Net.Http.Headers.MediaTypeHeaderValue(attachedFile.ContentType);

        //                form.Add(
        //                    fileContent,
        //                    "UploadedFiles",              
        //                    attachedFile.FileName      
        //                );
        //            }

        //            var response = await httpClient.PostAsync(
        //                _configuration["NotificationService:NotificationEmailApi"],
        //                form
        //            );

        //            var responseBody = await response.Content.ReadAsStringAsync();

        //            if (!response.IsSuccessStatusCode)
        //            {
        //                Console.WriteLine("Email failed:");
        //                Console.WriteLine(response.StatusCode);
        //                Console.WriteLine(responseBody);
        //            }
        //            else
        //            {
        //                Console.WriteLine("Email sent successfully");
        //                Console.WriteLine(responseBody);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine("Email error: " + ex);
        //        }
        //    }


        private async Task<Guid> UploadToDocumentStorageAsync(IFormFile file)
        {
            using var client = new HttpClient
            {
                BaseAddress = new Uri("http://10.176.100.17:5000/")
            };

            client.DefaultRequestHeaders.Add("app_id", _configuration["DocumentStorage:AppId"]);
            client.DefaultRequestHeaders.Add("client_secret", _configuration["DocumentStorage:ClientSecret"]);

            using var form = new MultipartFormDataContent();

            using var fileStream = file.OpenReadStream();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

            form.Add(fileContent, "File", file.FileName);
            form.Add(new StringContent(_claimService.GetUserId().ToString()), "CreatedBy");

            var response = await client.PostAsync("api/Documents/upload", form);

            if (!response.IsSuccessStatusCode)
                throw new ApplicationException("Document Storage API call failed.");

            var responseBody = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonConvert.DeserializeObject<DocumentApiResponse<DocumentUploadResponse>>(responseBody);

            if (apiResponse.ApiResponseStatus != 1 || apiResponse.Result == null)
                throw new ApplicationException(apiResponse.Message ?? "Invalid document upload response.");

            return apiResponse.Result.DocumentId;
        }

        public async Task<byte[]> DownloadDocumentFromStorageAsync(Guid documentId)
        {
            using var client = new HttpClient
            {
                BaseAddress = new Uri(_configuration["DocumentStorage:BaseUrl"])
            };

            client.DefaultRequestHeaders.Add("app_id", _configuration["DocumentStorage:AppId"]);
            client.DefaultRequestHeaders.Add("client_secret", _configuration["DocumentStorage:ClientSecret"]);

            var response = await client.GetAsync($"api/Documents/{documentId}/download");

            if (!response.IsSuccessStatusCode)
                throw new ApplicationException(
                    $"Failed to download document {documentId} from Document Storage.");

            return await response.Content.ReadAsByteArrayAsync();
        }






    }
}
