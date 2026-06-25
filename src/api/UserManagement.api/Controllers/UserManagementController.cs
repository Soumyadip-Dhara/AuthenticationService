using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO.Pagination;
using UserManagement.BAL.Interfaces.Master;

namespace UserManagement.api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class UserManagementController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public UserManagementController(IRoleService roleService)
        {
            _roleService = roleService;
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
    }
}
