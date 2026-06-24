using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO.Pagination;
using System.Linq;
using System;

namespace UserManagement.BAL.Services.Master
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IUserApplicationHasUserRoleRepository _userApplicationHasUserRoleRepository;
        private readonly ILevelHasAllowedRoleRepository _levelHasAllowedRoleRepository;
        private readonly IClaimService _claimService;

        public RoleService(
            IRoleRepository roleRepository,
            IUserApplicationHasUserRoleRepository userApplicationHasUserRoleRepository,
            ILevelHasAllowedRoleRepository levelHasAllowedRoleRepository,
            IClaimService claimService)
        {
            _roleRepository = roleRepository;
            _userApplicationHasUserRoleRepository = userApplicationHasUserRoleRepository;
            _levelHasAllowedRoleRepository = levelHasAllowedRoleRepository;
            _claimService = claimService;
        }

        public async Task<PaginatedResult<RoleGetDTO>> GetRoleListAsync(QueryParameters payload)
        {
            int applicationId = 0;
            short officeCode = 0;
            int levelId = 0;

            if (payload?.Filters != null)
            {
                var appFilter = payload.Filters.FirstOrDefault(f => f.Field.Equals("ApplicationId", StringComparison.OrdinalIgnoreCase));
                if (appFilter != null && int.TryParse(appFilter.Value, out int appId)) applicationId = appId;

                var levelFilter = payload.Filters.FirstOrDefault(f => f.Field.Equals("LevelId", StringComparison.OrdinalIgnoreCase));
                if (levelFilter != null && int.TryParse(levelFilter.Value, out int lId)) levelId = lId;

                var officeFilter = payload.Filters.FirstOrDefault(f => f.Field.Equals("OfficeCode", StringComparison.OrdinalIgnoreCase));
                if (officeFilter != null && short.TryParse(officeFilter.Value, out short oId)) officeCode = oId;
            }

            List<RoleGetDTO> RoleResult = new List<RoleGetDTO>();
            string[] userRoles = _claimService.GetRoles();
            if (Array.Exists(userRoles, roleName => roleName == "Super Admin"))
            {
                RoleResult = await GetRolesByApplicationIdForSuperAdminAndUserAdmin(applicationId);
            }
            else if (Array.Exists(userRoles, roleName => roleName.ToLower().Contains("user admin")))
            {
                RoleResult = await GetRolesByApplicationIdForSuperAdminAndUserAdmin(applicationId);
            }
            else if (officeCode == 1 || officeCode == 2)
            {
                RoleResult = await GetRolesByApplicationIdRoleIdForUM(applicationId, officeCode, levelId);
            }
            else
            {
                throw new Exception("Bad Request. Wrong office id provided.");
            }

            int totalCount = RoleResult.Count;
            int pageSize = payload?.PageSize > 0 ? payload.PageSize : 10;
            int pageNumber = payload?.PageNumber > 0 ? payload.PageNumber : 1;

            var paginatedData = RoleResult
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var paginatedResult = new PaginatedResult<RoleGetDTO>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Data = paginatedData
            };

            return paginatedResult;
        }

        public async Task<List<RoleGetDTO>> GetRolesByApplicationIdForSuperAdminAndUserAdmin(int applicationId)
        {
            List<RoleGetDTO> result = (List<RoleGetDTO>)await _roleRepository.GetSelectedColumnByConditionAsync(
                entity => entity.ApplicationId == applicationId,
                entity => new RoleGetDTO
                {
                    Id = entity.Id,
                    Title = entity.Title
                });
            return result;
        }

        public async Task<List<RoleGetDTO>> GetRolesByApplicationIdRoleIdForUM(int applicationId, short isOwnOffice, int levelId)
        {
            var ownRoleIds = (List<int>)await _userApplicationHasUserRoleRepository.GetSelectedColumnByConditionAsync(
                    e => e.UserHasApp.AppId == applicationId && e.UserHasApp.UserId == _claimService.GetUserId(),
                    e => e.RoleId
                );

            var data = new List<RoleGetDTO>();
            if (isOwnOffice == 1)
            {
                var levelHasAllowedRoles = (List<int>)await _levelHasAllowedRoleRepository.GetSelectedColumnByConditionAsync(
                    e => e.LevelId == levelId && !ownRoleIds.Contains(e.RoleId),
                    e => e.RoleId);
                data = (List<RoleGetDTO>)await _roleRepository.GetSelectedColumnByConditionAsync(
                    e => levelHasAllowedRoles.Contains(e.Id), 
                    e => new RoleGetDTO
                    {
                        Id = e.Id,
                        Title = e.Title,
                        ApplicationName = e.Application != null ? e.Application.Title : string.Empty
                    });
            }
            else
            {
                var levelHasAllowedRoles = (List<int>)await _levelHasAllowedRoleRepository.GetSelectedColumnByConditionAsync(
                    e => e.LevelId == levelId && e.IsParentOrAdminRole == true,
                    e => e.RoleId);
                data = (List<RoleGetDTO>)await _roleRepository.GetSelectedColumnByConditionAsync(
                    e => levelHasAllowedRoles.Contains(e.Id), 
                    e => new RoleGetDTO
                    {
                        Id = e.Id,
                        Title = e.Title,
                        ApplicationName = e.Application != null ? e.Application.Title : string.Empty
                    });
            }

            return data;
        }
    }
}
