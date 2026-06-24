using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.BAL.Interfaces;
using UserManagement.Helper;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO.Pagination;
using System.Threading.Tasks;

namespace UserManagement.api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
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
    }
}
