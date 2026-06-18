using Microsoft.AspNetCore.Mvc;
using Npgsql;
using UserManagement.BAL.Interfaces;
using UserManagement.Filters;
using UserManagement.Helper;
using UserManagement.Models.DTO;

namespace UserManagement.Controllers
{
    //[Authorize("Super Admin, IFMS USER")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class MigrationController : Controller
    {
        private readonly IMigrationService _migrationService;
        public MigrationController(IMigrationService migrationService)
        {
            _migrationService = migrationService;
        }

        [HttpPost("Upload")]
        public async Task<APIResponseClass<string>> UploadMigrationCsv(IFormFile file, string entityType)
        {
            APIResponseClass<string> response = new();
            var jobId = Guid.NewGuid();

            await _migrationService.UploadCsvAsync(file, entityType, jobId);
            response.apiResponseStatus = Enum.APIResponseStatus.Success;
            response.message = "CSV Uploaded Successfully";
            response.result = jobId.ToString();
            return response ;
        }


    }
}

