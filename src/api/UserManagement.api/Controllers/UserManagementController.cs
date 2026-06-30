using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO.Pagination;
using UserManagement.BAL.Interfaces.Master;

namespace UserManagement.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class UserManagementController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly IApplicationService _applicationService;
        private readonly ILevelService _levelService;
        private readonly IScopeService _scopeService;

        public UserManagementController(IRoleService roleService, IApplicationService applicationService, ILevelService levelService, IScopeService scopeService)
        {
            _roleService = roleService;
            _applicationService = applicationService;
            _levelService = levelService;
            _scopeService = scopeService;
        }

        [HttpPost("Role")]
        public async Task<ServiceResponse<PaginatedResult<RoleGetDTO>>> Role([FromBody] QueryParameters payload)
        {
            ServiceResponse<PaginatedResult<RoleGetDTO>> response = new();
            try
            {
                var result = await _roleService.GetRoleListAsync(payload);

                if (result != null && result.Data.Count > 0)
                {
                    response.Message = "Role List fetched successfully";
                }
                else
                {
                    response.Message = "Data Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = result;
                return response;
            }
            catch (System.Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.Message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }

        [HttpPost("fetchApplication")]
        public async Task<FetchApplicationResponse> FetchApplication([FromBody] QueryParameters payload)
        {
            try
            {
                var result = await _applicationService.GetApplicationListAsync(payload);
                return result;
            }
            catch (System.Exception Ex)
            {
                return new FetchApplicationResponse
                {
                    apiResponseStatus = 3, // Error
                    message = "Application List Not Found",
                    validationResults = Ex.Message,
                    result = new ApplicationPaginatedResult
                    {
                        totalCount = null,
                        pageNumber = null,
                        pageSize = null,
                        data = null
                    }
                };
            }
        }

        [HttpPost("fetchLevel")]
        public async Task<FetchLevelResponse> FetchLevel([FromBody] QueryParameters payload)
        {
            try
            {
                var result = await _levelService.GetLevelListAsync(payload);
                return result;
            }
            catch (System.Exception Ex)
            {
                return new FetchLevelResponse
                {
                    apiResponseStatus = 3, // Error
                    message = "Level List Not Found",
                    validationResults = Ex.Message,
                    result = new LevelPaginatedResult
                    {
                        totalCount = null,
                        pageNumber = null,
                        pageSize = null,
                        data = null
                    }
                };
            }
        }

        [HttpPost("fetchScope")]
        public async Task<FetchScopeResponse> FetchScope([FromBody] QueryParameters payload)
        {
            try
            {
                var result = await _scopeService.GetScopeListAsync(payload);
                return result;
            }
            catch (System.Exception Ex)
            {
                return new FetchScopeResponse
                {
                    apiResponseStatus = 3, // Error
                    message = "Scope List Not Found",
                    validationResults = Ex.Message,
                    result = new ScopePaginatedResult
                    {
                        totalCount = null,
                        pageNumber = null,
                        pageSize = null,
                        data = null
                    }
                };
            }
        }
    }
}
