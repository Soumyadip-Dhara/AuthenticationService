using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.BAL.Services.Master;

using UserManagement.Helper;
using UserManagement.Models.DTO;

namespace UserManagement.Controllers.Master
{
    [Authorize("Super Admin")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ServiceController : Controller
    {
        private readonly IApplicationService _applicationService;
        private readonly IServiceManagementService _serviceManagementService;
        public ServiceController(IApplicationService applicationService, IServiceManagementService serviceManagementService)
        {
            _applicationService = applicationService;
            _serviceManagementService = serviceManagementService;
        }
        [HttpGet("GetApplicationsForServices")]
        public async Task<APIResponseClass<List<ApplicationForServiceDTO>>> GetApplications()
        {
            APIResponseClass<List<ApplicationForServiceDTO>> response = new();
            try
            {
                List<ApplicationForServiceDTO> applicationResult = new List<ApplicationForServiceDTO>();
                applicationResult = await _applicationService.GetAllApplicationsForService();
                
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Data collected successfully";
                response.result = applicationResult;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }
        [HttpGet("GetSerivesWithPermission")]
        public async Task<APIResponseClass<List<MasterServicesDTO>>> GetServices()
        {
            APIResponseClass<List<MasterServicesDTO>> response = new();
            try
            {
                List<MasterServicesDTO> ServiceResult = new List<MasterServicesDTO>();
                ServiceResult = await _serviceManagementService.GetServicesWithPermissions();

                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Data collected successfully";
                response.result = ServiceResult;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }
        [HttpPost("EnableService")]
        public async Task<APIResponseClass<bool>> EnableService(EnableServiceRequestDTO request)
        {
            var response = new APIResponseClass<bool>();

            try
            {

                var result = await _serviceManagementService.EnableDisableServiceAsync(request);

                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = request.Enabled? "Service enabled successfully": "Service disabled successfully";
                response.result = result;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = ex.Message;
                response.result = false;

            }

            return response;
        }
        [HttpPost("AddService")]
        public async Task<APIResponseClass<bool>> AddService(AddServiceRequestDTO request)
        {
            var response = new APIResponseClass<bool>();

            try
            {
                var result = await _serviceManagementService.AddServiceAsync(request);

                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Service added successfully";
                response.result = result;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = ex.Message;
                response.result = false;
            }

            return response;
        }



    }
}
