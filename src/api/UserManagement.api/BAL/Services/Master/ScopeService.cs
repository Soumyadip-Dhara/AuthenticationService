using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.DAL.Entities;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO.Pagination;

namespace UserManagement.BAL.Services.Master
{
    public class ScopeService : IScopeService
    {
        private readonly IScopeRepository _scopeRepository;
        private readonly IUserHasApplicationRepository _userHasApplicationRepository;
        private readonly IUserRoleHasUserLevelRepository _userRoleHasUserLevelRepository;
        private readonly IUserLevelHasUserScopeRepository _userLevelHasUserScopeRepository;
        private readonly IScopeRelationshipRepository _scopeRelationshipRepository;
        private readonly IApplicationLevelRepository _applicationLevelRepository;
        private readonly IUserMasterRepository _userMasterRepository;
        private readonly IClaimService _claimService;

        public ScopeService(
            IScopeRepository scopeRepository,
            IUserHasApplicationRepository userHasApplicationRepository,
            IUserRoleHasUserLevelRepository userRoleHasUserLevelRepository,
            IUserLevelHasUserScopeRepository userLevelHasUserScopeRepository,
            IScopeRelationshipRepository scopeRelationshipRepository,
            IApplicationLevelRepository applicationLevelRepository,
            IUserMasterRepository userMasterRepository,
            IClaimService claimService)
        {
            _scopeRepository = scopeRepository;
            _userHasApplicationRepository = userHasApplicationRepository;
            _userRoleHasUserLevelRepository = userRoleHasUserLevelRepository;
            _userLevelHasUserScopeRepository = userLevelHasUserScopeRepository;
            _scopeRelationshipRepository = scopeRelationshipRepository;
            _applicationLevelRepository = applicationLevelRepository;
            _userMasterRepository = userMasterRepository;
            _claimService = claimService;
        }

        public async Task<FetchScopeResponse> GetScopeListAsync(QueryParameters payload)
        {
            try
            {
                int appId = 0;
                int levelId = 0;
                short officeCode = 0;
                string search = "";

                if (payload?.Filters != null)
                {
                    var appFilter = payload.Filters.FirstOrDefault(f => f.Field.Equals("ApplicationId", StringComparison.OrdinalIgnoreCase));
                    if (appFilter != null && int.TryParse(appFilter.Value, out int aId))
                    {
                        appId = aId;
                    }

                    var levelFilter = payload.Filters.FirstOrDefault(f => f.Field.Equals("LevelId", StringComparison.OrdinalIgnoreCase));
                    if (levelFilter != null && int.TryParse(levelFilter.Value, out int lId))
                    {
                        levelId = lId;
                    }

                    var officeFilter = payload.Filters.FirstOrDefault(f => f.Field.Equals("OfficeCode", StringComparison.OrdinalIgnoreCase));
                    if (officeFilter != null && short.TryParse(officeFilter.Value, out short oCode))
                    {
                        officeCode = oCode;
                    }

                    var searchFilter = payload.Filters.FirstOrDefault(f => 
                        f.Field.Equals("search", StringComparison.OrdinalIgnoreCase) || 
                        f.Field.Equals("ScopeName", StringComparison.OrdinalIgnoreCase) || 
                        f.Field.Equals("ScopeValue", StringComparison.OrdinalIgnoreCase) || 
                        f.Field.Equals("title", StringComparison.OrdinalIgnoreCase)
                    );
                    if (searchFilter != null)
                    {
                        search = searchFilter.Value;
                    }
                }

                int pageSize = payload?.PageSize > 0 ? payload.PageSize : 10;
                int pageNumber = payload?.PageNumber > 0 ? payload.PageNumber : 1;
                int offset = (pageNumber - 1) * pageSize;
                int limit = pageSize;

                string[] userRoles = _claimService.GetRoles();
                string msg = "Data Not Found";
                var scopeData = new ScopeFetchReturnDTO();

                if (Array.Exists(userRoles, roleName => roleName == "Super Admin") || Array.Exists(userRoles, roleName => roleName == "User Admin"))
                {
                    scopeData = await GetScopeByLevelIdSuperAdminAndUserAdmin(levelId, offset, limit, search);
                }
                else if (officeCode == 1)
                {
                    // own office scopes 
                    scopeData = await GetScopeByLevelIdForOwnOffice(appId, levelId, offset, limit, search);
                }
                else if (officeCode == 2)
                {
                    // Other office scopes 
                    var res = await GetScopeByLevelIdForOtherOffice(appId, levelId, offset, limit, search);
                    scopeData = res.Item2;
                    msg = res.Item1;
                }

                if (scopeData.Scopes.Count != 0)
                {
                    return new FetchScopeResponse
                    {
                        apiResponseStatus = 1, // Success
                        message = "Scope List fetched successfully",
                        validationResults = null,
                        result = new ScopePaginatedResult
                        {
                            totalCount = (int?)scopeData.TotalCount,
                            pageNumber = pageNumber,
                            pageSize = pageSize,
                            data = scopeData.Scopes
                        }
                    };
                }
                else
                {
                    return new FetchScopeResponse
                    {
                        apiResponseStatus = 3, // Error
                        message = msg,
                        validationResults = msg,
                        result = new ScopePaginatedResult
                        {
                            totalCount = 0,
                            pageNumber = pageNumber,
                            pageSize = pageSize,
                            data = new List<ScopeFetchDTO>()
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                return new FetchScopeResponse
                {
                    apiResponseStatus = 3, // Error
                    message = "Failed, please try again.." + ex.Message,
                    validationResults = ex.Message,
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

        public async Task<ScopeFetchReturnDTO> GetScopeByLevelIdSuperAdminAndUserAdmin(int levelId, int offset, int limit, string search)
        {
            var scopeIds = (List<int>)await _scopeRepository.GetSelectedColumnByConditionAsync(
                e => e.ApplicationScopes.Any(s => s.LevelId == levelId && s.IsDeleted == false && s.Status == 1),
                e => e.ScopeId);

            var scopes = new List<ScopeFetchDTO>();
            long totalCount = 0;

            if (string.IsNullOrEmpty(search))
            {
                var scopeRes = await _scopeRepository.GetSelectedColumnByConditionAsyncWithOffsetLimitAndTotalCount(
                        e => scopeIds.Contains(e.ScopeId) && e.IsActive == true,
                        e => new ScopeFetchDTO
                        {
                            ScopeValue = e.Value,
                            ScopeId = e.ApplicationScopes
                                .Where(a => a.ScopeId == e.ScopeId && a.LevelId == levelId)
                                .Select(a => a.AppScopeId).FirstOrDefault(),
                            ScopeName = e.ScopeName
                        },
                        limit,
                        offset);

                scopes = scopeRes.Data.ToList();
                totalCount = scopeRes.TotalCount;
            }
            else
            {
               var scopeRes = await _scopeRepository.GetSelectedColumnByConditionAsyncWithOffsetLimitAndTotalCount(
                    e => scopeIds.Contains(e.ScopeId) && (e.Value.ToLower().Contains(search.ToLower()) || e.ScopeName.ToLower().Contains(search.ToLower())),
                    e => new ScopeFetchDTO
                    {
                        ScopeValue = e.Value,
                        ScopeId = e.ApplicationScopes
                                .Where(a => a.ScopeId == e.ScopeId && a.LevelId == levelId)
                                .Select(a => a.AppScopeId).FirstOrDefault(),
                        ScopeName = e.ScopeName
                    },
                    limit,
                    offset);
                scopes = scopeRes.Data.ToList();
                totalCount = scopeRes.TotalCount;
            }

            return new ScopeFetchReturnDTO { Scopes = scopes, TotalCount = totalCount };
        }

        public async Task<ScopeFetchReturnDTO> GetScopeByLevelIdForOwnOffice(int appId, int levelId, int offset, int limit, string search)
        {
            var userHasApplicationPk = await _userHasApplicationRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.UserId == _claimService.GetUserId() && e.AppId == appId,
                e => e.Id);
            var userRoleHasLevelIdPk = await _userRoleHasUserLevelRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.UserHasAppId == userHasApplicationPk && e.RoleHasLevelId == levelId,
                e => e.Id);
            var scopeIds = (List<long>)await _userLevelHasUserScopeRepository.GetSelectedColumnByConditionAsync(
                e => e.UserRoleHasLevelId == userRoleHasLevelIdPk && e.LevelId == levelId,
                e => e.UserLevelHasScopeId);
            var masterScopesIds = await _scopeRepository.GetSelectedColumnByConditionAsync(
                e => e.ApplicationScopes.Any(x => scopeIds.Contains(x.AppScopeId)),
                e => e.ScopeId
            );
            var scopes = new List<ScopeFetchDTO>();
            long totalCount = 0;

            if (string.IsNullOrEmpty(search))
            {
                var scopeRes = await _scopeRepository.GetSelectedColumnByConditionAsyncWithOffsetLimitAndTotalCount(
                    e => masterScopesIds.Contains(e.ScopeId),
                    e => new ScopeFetchDTO
                    {
                        ScopeValue = e.Value,
                        ScopeId = e.ApplicationScopes
                                    .Where(a => a.ScopeId == e.ScopeId && a.LevelId == levelId)
                                    .Select(a => a.AppScopeId).FirstOrDefault(),
                        ScopeName = e.ScopeName
                    },
                    limit,
                    offset
                );
                scopes = scopeRes.Data.ToList();
                totalCount = scopeRes.TotalCount;
            }
            else
            {
                var scopeRes = await _scopeRepository.GetSelectedColumnByConditionAsyncWithOffsetLimitAndTotalCount(
                    e => scopeIds.Contains((long)e.ScopeId) && (e.Value.ToLower().Contains(search.ToLower()) || e.ScopeName.ToLower().Contains(search.ToLower())),
                    e => new ScopeFetchDTO
                    {
                        ScopeValue = e.Value,
                        ScopeId = e.ApplicationScopes
                                    .Where(a => a.ScopeId == e.ScopeId && a.LevelId == levelId)
                                    .Select(a => a.AppScopeId).FirstOrDefault(),
                        ScopeName = e.ScopeName
                    },
                    limit,
                    offset
                );
                scopes = scopeRes.Data.ToList();
                totalCount = scopeRes.TotalCount;
            }

            return new ScopeFetchReturnDTO { Scopes = scopes, TotalCount = totalCount };
        }

        public async Task<(string, ScopeFetchReturnDTO)> GetScopeByLevelIdForOtherOffice(int appId, int levelId, int offset, int limit, string search)
        {
            var res = "Scopes Found";
            var userHasApplicationPk = await _userHasApplicationRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.UserId == _claimService.GetUserId() && e.AppId == appId,
                e => e.Id);

            var userRoleHasLevelIdPk = await _userRoleHasUserLevelRepository.GetSelectedColumnByConditionAsync(
                e => e.UserHasAppId == userHasApplicationPk,
                e => new { e.Id, e.RoleHasLevelId });
            var userRoleHasLevelIdListPK = userRoleHasLevelIdPk.Select(e => e.Id).ToList();
            var userRoleHasLevelIdList = userRoleHasLevelIdPk.Select(e => e.RoleHasLevelId).ToList();

            var scopeIds = (List<long>)await _userLevelHasUserScopeRepository.GetSelectedColumnByConditionAsync(
                e => userRoleHasLevelIdListPK.Contains(e.UserRoleHasLevelId),
                e => e.UserLevelHasScopeId);
            var selectedScopes = new List<long>();
            var masterScopesIds = new List<int>();

            bool isOtherOfficeAllowed = await _applicationLevelRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.SameLevelOtherOfficeAdminAllowed == true && e.AppLevelId == levelId, 
                e => true
            );

            if (isOtherOfficeAllowed && userRoleHasLevelIdList.Contains(levelId))
            {
                masterScopesIds = (List<int>)await _scopeRepository.GetSelectedColumnByConditionAsync(
                    e => e.ApplicationScopes.Any(s => s.LevelId == levelId), e => e.ScopeId);
            }
            else
            {
                selectedScopes = (List<long>)await _scopeRelationshipRepository.GetSelectedColumnByConditionAsync(
                    e => e.ParentScopeId.HasValue && scopeIds.Contains(e.ParentScopeId.Value) && e.OwnScopeLevelId == levelId, e => e.ScopeId ?? 0);

                masterScopesIds = (List<int>)await _scopeRepository.GetSelectedColumnByConditionAsync(
                    e => e.ApplicationScopes.Any(x => selectedScopes.Contains(x.AppScopeId)) && e.IsActive == true,
                    e => e.ScopeId
                );
            }

            var scopes = new List<ScopeFetchDTO>();
            long totalCount = 0;

            if (string.IsNullOrEmpty(search))
            {
                var scopeRes = await _scopeRepository.GetSelectedColumnByConditionAsyncWithOffsetLimitAndTotalCount(
                    e => masterScopesIds.Contains(e.ScopeId),
                    e => new ScopeFetchDTO
                    {
                        ScopeValue = e.Value,
                        ScopeId = e.ApplicationScopes
                                .Where(a => a.ScopeId == e.ScopeId && a.LevelId == levelId)
                                .Select(a => a.AppScopeId).FirstOrDefault(),
                        ScopeName = e.ScopeName,
                    },
                    limit,
                    offset);
                scopes = scopeRes.Data.ToList();
                totalCount = scopeRes.TotalCount;
            }
            else
            {
                var scopeRes = await _scopeRepository.GetSelectedColumnByConditionAsyncWithOffsetLimitAndTotalCount(
                    e => masterScopesIds.Contains(e.ScopeId)
                         && (e.Value.ToLower().Contains(search.ToLower()) || e.ScopeName.ToLower().Contains(search.ToLower())),
                    e => new ScopeFetchDTO
                    {
                        ScopeValue = e.Value,
                        ScopeId = e.ApplicationScopes
                                .Where(a => a.ScopeId == e.ScopeId && a.LevelId == levelId)
                                .Select(a => a.AppScopeId).FirstOrDefault(),
                        ScopeName = e.ScopeName,
                    },
                    limit,
                    offset);

                if (scopeRes.Data.Count == 0)
                {
                    res = await _userMasterRepository.GetAdminsForScope(search, levelId, appId);
                }
                scopes = scopeRes.Data.ToList();
                totalCount = scopeRes.TotalCount;
            }

            return (res, new ScopeFetchReturnDTO { Scopes = scopes, TotalCount = totalCount });
        }
    }
}
