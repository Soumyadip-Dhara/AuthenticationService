using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using UserManagement.BAL.Interfaces;
using UserManagement.DAL.Interfaces;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO.Pagination;
using System.Security.Claims;

namespace UserManagement.BAL.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IClaimService _claimService;

        public UserService(IUserRepository userRepository, IClaimService claimService)
        {
            _userRepository = userRepository;
            _claimService = claimService;
        }

        public async Task<ServiceResponse<PaginatedResult<UserDetailsDTO>>> FetchUserList(QueryParameters payload)
        {
            var userId = _claimService.GetUserId();
            var userRoles = _claimService.GetRoles();
            var role = userRoles.Length > 0 ? userRoles[0] : "User";

            var result = await _userRepository.FetchUserList(payload, role, userId);
            return new ServiceResponse<PaginatedResult<UserDetailsDTO>>
            {
                result = result,
                apiResponseStatus = Enum.APIResponseStatus.Success,
                Message = result.Data.Count > 0 ? "User List Found" : "Data Not Found"
            };
        }
    }
}
