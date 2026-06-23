using Microsoft.AspNetCore.Mvc;
using System.Runtime.ConstrainedExecution;
using UserManagement.BAL.Services;
using UserManagement.Helper;
using UserManagement.Models.DTO;

namespace UserManagement.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuditCertificateController : Controller
    {
        private readonly IAuditCertificateService _auditCertificateService;
        public AuditCertificateController(IAuditCertificateService auditCertificateService)
        {
            _auditCertificateService = auditCertificateService;
        }

        [HttpPost("InsertCertificate")]
        public async Task<APIResponseClass<int>> InsertCertificate([FromForm] CertificateUploadDto dto)
        {
            APIResponseClass<int> response = new();
            try
            {
                int result = await _auditCertificateService.UploadCertificateAsync(dto);

                if (result > 0)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "Certificate inserted successfully.";

                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Failed to insert certificate.";
                }

                response.result = result;
                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = $"Exception: {ex.Message}";
                return response;
            }
        }

        [HttpPost("UpdateCertificate/{id}")]
        public async Task<APIResponseClass<int>> UpdateCertificate(long id, [FromBody] CertificateUpdateDto dto)
        {
            APIResponseClass<int> response = new();
            try
            {
                int result = await _auditCertificateService.UpdateCertificateAsync(id, dto);

                if (result > 0)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = $"Certificate with id={id} updated successfully.";
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = $"Failed to update certificate with id={id}.";
                }

                response.result = result;
                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = $"Exception: {ex.Message}";
                return response;
            }
        }

        [HttpGet("GetAll")]
        public async Task<APIResponseClass<IEnumerable<SecurityAuditCertificateDto>>> GetAll()
        {
            var response = new APIResponseClass<IEnumerable<SecurityAuditCertificateDto>>();

            try
            {
                var result = await _auditCertificateService.GetAllAsync();

                if (result != null && result.Any())
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "Certificates retrieved successfully.";
                    response.result = result;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "No certificates found.";
                    response.result = Enumerable.Empty<SecurityAuditCertificateDto>();
                }
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = $"Exception: {ex.Message}";
                response.result = Enumerable.Empty<SecurityAuditCertificateDto>();
            }

            return response;
        }

        [HttpGet("GetLatestActiveAuditCertificate")]
        public async Task<APIResponseClass<LatestActiveUMSecurityAuditCertificateDto>> GetLatest([FromQuery] int applicationId = 1)
        {
            var response = new APIResponseClass<LatestActiveUMSecurityAuditCertificateDto>();

            try
            {
                var result = await _auditCertificateService.GetLatestActiveCertificateAsync(applicationId);

                if (result != null)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "Latest active certificate retrieved successfully.";
                    response.result = result;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = $"No active certificate found for {applicationId}";
                }
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = $"Exception: {ex.Message}";
            }

            return response;
        }

        [HttpGet("GetAllApplications")]
        public async Task<APIResponseClass<IEnumerable<ApplicationDto>>> GetAllApplications()
        {
            var response = new APIResponseClass<IEnumerable<ApplicationDto>>();

            try
            {
                var result = await _auditCertificateService.GetApplicationsAsync();

                if (result != null && result.Any())
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "Applications retrieved successfully.";
                    response.result = result;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "No applications found.";
                    response.result = Enumerable.Empty<ApplicationDto>();
                }
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = $"Exception: {ex.Message}";
                response.result = Enumerable.Empty<ApplicationDto>();
            }

            return response;
        }

        //[HttpGet("ViewCertificate/{id}")]
        //public async Task<APIResponseClass<byte[]>> ViewCertificate(long id)
        //{
        //    var response = new APIResponseClass<byte[]>();

        //    try
        //    {
        //        var certificate = await _auditCertificateService.GetCertificateByIdAsync(id);

        //        if (certificate == null || string.IsNullOrEmpty(certificate.CertificatePath))
        //        {
        //            response.apiResponseStatus = Enum.APIResponseStatus.Error;
        //            response.message = "Certificate not found.";
        //            response.result = Array.Empty<byte>();
        //            return response;
        //        }

        //        if (!System.IO.File.Exists(certificate.CertificatePath))
        //        {
        //            response.apiResponseStatus = Enum.APIResponseStatus.Error;
        //            response.message = "File not found on server.";
        //            response.result = Array.Empty<byte>();
        //            return response;
        //        }

        //        var fileBytes = await System.IO.File.ReadAllBytesAsync(certificate.CertificatePath);

        //        response.apiResponseStatus = Enum.APIResponseStatus.Success;
        //        response.message = "Certificate retrieved successfully.";
        //        response.result = fileBytes;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.apiResponseStatus = Enum.APIResponseStatus.Error;
        //        response.message = $"Exception: {ex.Message}";
        //        response.result = Array.Empty<byte>();
        //    }

        //    return response;
        //}

        [HttpGet("DownloadCertificate/{id}")]
        public async Task<APIResponseClass<FileDto>> DownloadCertificate(long id)
        {
            var response = new APIResponseClass<FileDto>();

            try
            {
                var certificate = await _auditCertificateService.DownloadCertificate(id);

                if (certificate == null)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Certificate not found.";
                    response.result = null!;
                    return response;
                }

                //if (!System.IO.File.Exists(certificate.CertificatePath))
                //{
                //    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                //    response.message = "File not found on server.";
                //    response.result = null!;
                //    return response;
                //}

                //var fileBytes = await System.IO.File.ReadAllBytesAsync(certificate.CertificatePath);
                // Fetch document from Document Storage
                //byte[] fileBytes = await _auditCertificateService.DownloadDocumentFromStorageAsync(certificate.DocumentId);
                //var fileName = Path.GetFileName(certificate.CertificatePath);

                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Certificate downloaded successfully.";
                response.result = new FileDto
                {
                    FileName = "abc",
                    FileContent = certificate,
                    ContentType = "application/pdf"
                };
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = $"Exception: {ex.Message}";
                response.result = null!;
            }

            return response;
        }



    }
}
