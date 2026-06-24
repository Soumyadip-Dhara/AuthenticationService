using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.BAL.Interfaces;
using UserManagement.Helper;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO.Pagination;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using UserManagement.BAL.Interfaces.Master;

namespace UserManagement.api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly IClaimService _claimService;

        public UserController(IUserService userService, IRoleService roleService, IClaimService claimService)
        {
            _userService = userService;
            _roleService = roleService;
            _claimService = claimService;
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

        [HttpPost("fetchUserList")]
        public async Task<ServiceResponse<PaginatedResult<UserDetailsDTO>>> fetchUserList([FromBody] QueryParameters payload)
        {
            ServiceResponse<PaginatedResult<UserDetailsDTO>> response = new();
            try
            {
                var result = await _userService.FetchUserList(payload);
                return result;
            }
            catch (System.Exception ex)
            {
                response.Message = ex.Message;
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                return response;
            }
        }

        [HttpPost("/api/usermanagement/upsertBasicUserDetails")]
        public async Task<IActionResult> UpsertBasicUserDetails([FromBody] UpsertBasicUserDetailsRequest payload)
        {
            var response = await _userService.UpsertBasicUserDetails(payload);
            return Ok(response);
        }

        [HttpGet("/api/usermanagement/fetchBasicUserDetails/{userId}")]
        public async Task<IActionResult> FetchBasicUserDetails(long userId)
        {
            var response = await _userService.FetchBasicUserDetails(userId);
            return Ok(response);
        }
    }
}
