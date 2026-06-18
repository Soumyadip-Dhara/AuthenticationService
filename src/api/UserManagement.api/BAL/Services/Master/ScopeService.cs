using System.Text.Json;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.Models.DTO;
using UserManagement.Utils.Interfaces;
namespace UserManagement.BAL.Services.Master
{
    public class ScopeService : IScopeService
    {
        private readonly IScopeRepository _scopeRepository;
        private readonly IUserLevelHasUserScopeRepository _userLevelHasUserScopeRepository;
        private readonly IUserHasApplicationRepository _userHasApplicationRepository;
        private readonly IUserRoleHasUserLevelRepository _userRoleHasUserLevelRepository;
        private readonly IScopeRelationshipRepository _scopeRelationshipRepository;
        private readonly IClaimService _claimService;
        private readonly ILevelRelationshipRepository _levelRelationshipRepository;
        private readonly ILevelRepository _levelRepository;
        private readonly IRabbitMQPublisherService _rabbitMQPublisherService;
        private readonly IUserMasterRepository _userMasterRepository;

        public ScopeService(IScopeRepository scopeRepository, IUserLevelHasUserScopeRepository userLevelHasUserScopeRepository, IUserHasApplicationRepository userHasApplicationRepository, IUserRoleHasUserLevelRepository userRoleHasUserLevelRepository, IScopeRelationshipRepository scopeRelationshipRepository, IClaimService claimService, ILevelRelationshipRepository levelRelationshipRepository, ILevelRepository levelRepository, IRabbitMQPublisherService rabbitMQPublisherService, IUserMasterRepository userMasterRepository)
        {
            _scopeRepository = scopeRepository;
            _userLevelHasUserScopeRepository = userLevelHasUserScopeRepository;
            _userHasApplicationRepository = userHasApplicationRepository;
            _userRoleHasUserLevelRepository = userRoleHasUserLevelRepository;
            _scopeRelationshipRepository = scopeRelationshipRepository;
            _claimService = claimService;
            _levelRelationshipRepository = levelRelationshipRepository;
            _levelRepository = levelRepository;
            _rabbitMQPublisherService = rabbitMQPublisherService;
            _userMasterRepository = userMasterRepository;
        }


        public async Task<(bool, string)> InsertScopeData(List<ScopeDataInsertDTO> scopes)
        {
            if (scopes.Count > 0)
            {
                var res = await _scopeRepository.InsertScopeValue(scopes);

                await _rabbitMQPublisherService.PublishMessage($"{res.Item4}-GETSCOPES", JsonSerializer.Serialize(res.Item3));

                return (res.Item1, res.Item2);
            }
            return (false, "No Data Provided");
        }
        public async Task<bool> CreateNewScope(ScopeCreateDTO scope)
        {
            if (scope != null)
            {
                return _scopeRepository.CreateNewScope(scope);
            }
            return false;
        }
        public async Task<List<ScopeStructureDTO>> GetScopesByLevelId(int levelId)
        {
            return _scopeRepository.GetScopesByLevelId(levelId);
        }
        public async Task<ScopeReturnDTO> GetScopesByLevelIdForViewOnly(int levelId, int first, int rows, string globalSearch)
        {
            var search = globalSearch?.ToLower() ?? string.Empty;
            
            var res = await _scopeRepository.GetSelectedColumnByConditionAsyncWithOffsetLimitAndTotalCount(
                s => !s.IsDeleted
                    //&& s.IsActive  // currentlly fetch all scope from master
                    && s.ApplicationScopes.Any(a => a.LevelId == levelId && !a.IsDeleted) 
                    && (string.IsNullOrEmpty(search) 
                        || s.ScopeName.ToLower().Contains(search) 
                        || s.Value.ToLower().Contains(search)),
                s => new ScopesDTO 
                {
                    Id = s.ScopeId,
                    Name = s.ScopeName,
                    Value = s.Value,
                    IsActive= s.IsActive,
                    Status = s.ApplicationScopes
                        .Where(a => a.LevelId == levelId && !a.IsDeleted)
                        .Select(a => a.Status)
                        .FirstOrDefault(),
                    IsGlobal = s.IsGlobal,
                    accessedByScopes = s.ApplicationScopes
                        .Where(a => a.LevelId == levelId && !a.IsDeleted) // matches s.level_id filter & avoids deleted
                        .SelectMany(a => a.ScopeRelationships   // ⚠️ use ScopeRelationships (NOT ParentScopeRelationships)
                            .Where(sr => sr.OwnScopeLevelId == a.LevelId && !sr.IsDeleted && sr.IsActive)) 
                        .Select(sr => sr.ParentAppScope.Scope.ScopeName!)
                        .Distinct()
                        .ToList()


                },
                rows,
                first
            );
           
            return new ScopeReturnDTO {
                Scopes = (List<ScopesDTO>)res.Data,
                TotalCount = res.TotalCount
            };
        }
        //public async Task<List<ScopeFetchDTO>> GetScopesOfAuthenticatedUserByLevelId(int levelId, string? search)
        //{
        //    var dataForSearch = (List<ScopeDataForSearchDTO>)await _userLevelHasUserScopeRepository
        //        .GetSelectedColumnByConditionAsync(
        //            e => e.UserRoleHasLevelId == levelId,
        //            e => new ScopeDataForSearchDTO
        //            {
        //                Id = e.UserLevelHasScopeId,
        //                LevelId = e.LevelId
        //            });

        //    var data = await _scopeRepository.GetScopesOfAuthenticatedUserByLevelId(dataForSearch, search);

        //    return data;
        //}
        public async Task<List<ScopeFetchDTO>> GetScopesOfAuthenticatedUserByLevelId(int levelId, string? filter)
        {
            var dataForSearch = (List<ScopeDataForSearchDTO>)await _userLevelHasUserScopeRepository.GetSelectedColumnByConditionAsync(e => e.UserRoleHasLevelId == levelId, e => new ScopeDataForSearchDTO
            {
                Id = e.UserLevelHasScopeId,
                LevelId = e.LevelId
            });
            var data = await _scopeRepository.GetScopesOfAuthenticatedUserByLevelId(dataForSearch, filter);
            return data;
        }
        public async Task<ScopeFetchReturnDTO> GetScopeByLevelIdSuperAdminAndUserAdmin(int levelId, int offset, int limit, string search)
        {
            var scopeIds = (List<int>)await _scopeRepository.GetSelectedColumnByConditionAsync(
                e => e.ApplicationScopes.Any(s => s.LevelId == levelId && s.IsDeleted == false && s.Status == 1),
                e => e.ScopeId);

            var scopes = new List<ScopeFetchDTO>();
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
                scopes = (List<ScopeFetchDTO>)scopeRes.Data;
            }

            return new ScopeFetchReturnDTO { Scopes = scopes };
        }
        public async Task<List<ScopeFetchDTO>> GetScopeByLevelIdForApplicationSuperAdmin(int appId, int levelId)
        {
            // Filter by scope_id from application_scope for this level, exclude global (admin-created) scopes
            var scopeIds = (List<int>)await _scopeRepository.GetSelectedColumnByConditionAsync(
                e => e.ApplicationScopes.Any(s => s.LevelId == levelId && s.AppId == appId),
                e => e.ScopeId);

            var scopes = (List<ScopeFetchDTO>)await _scopeRepository.GetSelectedColumnByConditionAsync(
                e => scopeIds.Contains(e.ScopeId) && e.IsGlobal == false,
                e => new ScopeFetchDTO
                {
                    ScopeValue = e.Value,
                    ScopeId  = e.ApplicationScopes
                                .Where(a => a.ScopeId == e.ScopeId)
                                .Select(a => a.AppScopeId).FirstOrDefault(),
                    ScopeName = e.ScopeName
                });

            return scopes;
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
            if (string.IsNullOrEmpty(search))
            {

                var scopeRes = await _scopeRepository.GetSelectedColumnByConditionAsyncWithOffsetLimitAndTotalCount(
                e => masterScopesIds.Contains(e.ScopeId),
                e => new ScopeFetchDTO
                {
                    ScopeValue = e.Value,
                    ScopeId  = e.ApplicationScopes
                                .Where(a => a.ScopeId == e.ScopeId && a.LevelId == levelId)
                                .Select(a => a.AppScopeId).FirstOrDefault(),
                    ScopeName = e.ScopeName
                },
                limit,
                offset
                );
                scopes = (List<ScopeFetchDTO>)scopeRes.Data;
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
                scopes = (List<ScopeFetchDTO>)scopeRes.Data;
            }

            return new ScopeFetchReturnDTO { Scopes = scopes };
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
            if (await _levelRepository.GetSingleSelectedColumnByConditionAsync(e => e.SameLevelOtherOfficeAdminAllowed == true && e.AppLevelId == levelId, e => true) && userRoleHasLevelIdList.Contains(levelId))
            {
                masterScopesIds = (List<int>)await _scopeRepository.GetSelectedColumnByConditionAsync(
                    e => e.ApplicationScopes.Any(s => s.LevelId == levelId), e => e.ScopeId);
            }
            else
            {
                selectedScopes = (List<long>)await _scopeRelationshipRepository.GetSelectedColumnByConditionAsync(
                    e => scopeIds.Contains(e.ParentScopeId) && e.OwnScopeLevelId == levelId, e => (long)e.ScopeId);

                masterScopesIds = (List<int>)await _scopeRepository.GetSelectedColumnByConditionAsync(
                    e => e.ApplicationScopes.Any(x => selectedScopes.Contains(x.AppScopeId)) && e.IsActive == true,
                    e => e.ScopeId
                );
            }


            var scopes = new List<ScopeFetchDTO>();
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
                scopes = (List<ScopeFetchDTO>)scopeRes.Data;
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
                    var admins = _userMasterRepository.GetAdminsForScope(search, levelId, appId);
                    res = admins.Result.ToString();
                }
                scopes = (List<ScopeFetchDTO>)scopeRes.Data;
            }

            return (res, new ScopeFetchReturnDTO { Scopes = scopes });
        }

        public async Task<(string, bool)> UpadateScope(ScopeUpdateDTO scopeUpdateDTO)
        {
            var scope = await _scopeRepository.GetSingleAysnc(e => e.ScopeId == scopeUpdateDTO.ScopeId);
            if (scope == null)
            {
                return ("Item Does Not Exist!!!", false);
            }

            // 1. Fetch current AppScopeId early to fix ID mismatch bugs and optimize lookups
            var currentAppScopeId = await _scopeRepository.GetSingleSelectedColumnByConditionAsync(
                s => s.ScopeId == scopeUpdateDTO.ScopeId,
                s => s.ApplicationScopes
                        .Where(a => a.LevelId == scopeUpdateDTO.LevelId && a.AppId == scopeUpdateDTO.AppId)
                        .Select(a => a.AppScopeId)
                        .FirstOrDefault()
            );

            if (currentAppScopeId == 0)
            {
                return ("Application Scope Mapping Not Found!!!", false);
            }

            // 2. Fetch parent relationships using the correct AppScopeId
            var parentScopes = await _scopeRelationshipRepository.GetAllByConditionAsync(e => e.ScopeId == currentAppScopeId && e.ParentScopeId != currentAppScopeId && e.ParentScopeLevelId == scopeUpdateDTO.ParentLevelId);

            // 3. Fetch self-relationship using the correct AppScopeId
            var ownScope = await _scopeRelationshipRepository.GetSingleAysnc(e => e.ScopeId == currentAppScopeId && e.ScopeId == e.ParentScopeId);
            var isOwn = false;
            if (ownScope != null)
            {
                ownScope.IsActive = scopeUpdateDTO.IsActive; // Ensure status is propagated to self-relationship
                if (!_scopeRelationshipRepository.Update(ownScope))
                {
                    isOwn = true;
                }
            }

            scope.ScopeName = scopeUpdateDTO.Name;
            scope.Value = scopeUpdateDTO.Code;
            scope.IsActive = scopeUpdateDTO.IsActive;
            var flag = false;
            if (_scopeRepository.Update(scope) && !isOwn)
            {
                // Update Application Scope Status
                await _scopeRepository.UpdateApplicationScopeStatus(scopeUpdateDTO.AppId, (int)scopeUpdateDTO.ScopeId, scopeUpdateDTO.Status);

                // 4. Propagate IsActive status to all child relationships
                var childrenRelationships = await _scopeRelationshipRepository.GetAllByConditionAsync(
                    e => e.ParentScopeId == currentAppScopeId && e.ParentScopeLevelId == scopeUpdateDTO.LevelId
                );
                foreach (var rel in childrenRelationships)
                {
                    if (rel.ScopeId != currentAppScopeId) // Avoid redundant update of self-relationship
                    {
                        rel.IsActive = scope.IsActive;
                        _scopeRelationshipRepository.Update(rel);
                    }
                }

                if (_scopeRelationshipRepository.DeleteRange(parentScopes))
                {
                    foreach (var parentInfo in scopeUpdateDTO.accessedByScopes)
                    {
                        var parentAppScope = await _scopeRepository.GetSingleSelectedColumnByConditionAsync(
                                        s => s.ScopeId == parentInfo.ScopeId,
                                        s => s.ApplicationScopes
                                            .Where(a => a.LevelId == parentInfo.ParentScopeLevelId && a.AppId == scopeUpdateDTO.AppId)
                                            .Select(a => a.AppScopeId)
                                            .FirstOrDefault());

                        var scopeRelationship = new ScopeRelationship
                        {
                            ScopeId = currentAppScopeId,
                            ParentScopeId = (int)parentAppScope,
                            OwnScopeLevelId = scopeUpdateDTO.LevelId,
                            ParentScopeLevelId = parentInfo.ParentScopeLevelId,
                            IsActive = scope.IsActive
                        };

                        if (!_scopeRelationshipRepository.Add(scopeRelationship))

                        {
                            flag = true;
                        }
                    }
                    if (flag == false)
                    {
                        _scopeRelationshipRepository.SaveChangesManaged();
                        _scopeRepository.SaveChangesManaged();
                        return ("Update Successful", true);
                    }
                    else
                    {
                        return ("Updation failed", false);
                    }
                }
                else
                {
                    return ("Updation failed", false);
                }
            }
            return ("Updation failed", false);
        }


        public async Task<(string, bool)> DeleteScope(int Id)
        {
            var res = await _scopeRepository.GetSingleAysnc(p => p.ScopeId == Id);
            if (res != null)
            {
                // app_scope_id corresponding to this ScopeMaster
                int appScopeId = await _scopeRepository.GetAppScopeId(Id);

                // Use appScopeId to find the relationships perfectly! (Including where it acts as a ParentScope)
                var accesedScopes = await _scopeRelationshipRepository.GetAllByConditionAsync(e => e.ScopeId == appScopeId || e.ParentScopeId == appScopeId);
                
                // 1. Soft Delete Relationships first
                foreach (var rel in accesedScopes)
                {
                    rel.IsDeleted = true;
                    rel.IsActive = false;
                    _scopeRelationshipRepository.Update(rel);
                }

                // Get current user ID if possible to update UpdatedBy
                long currentUserId = _claimService.GetUserId();

                // 2. Soft Delete ApplicationScope entries related to this scope
                await _scopeRepository.DeleteApplicationScopesByScopeId(Id, currentUserId);

                // 3. Soft Delete ScopeMaster
                res.IsDeleted = true;
                res.IsActive = false;
                res.UpdatedAt = DateTime.Now;
                res.UpdatedBy = currentUserId;

                if (_scopeRepository.Update(res))
                {
                    _scopeRelationshipRepository.SaveChangesManaged();
                    _scopeRepository.SaveChangesManaged();
                    return ("Scope Deletion Succesful", true);
                }
                
                return ("Scope Deletion Unsuccesful", false);
            }
            else
            {
                return (("Item Does Not Exists!!!", false));
            }
        }
        public Task<(bool, string)> ImportScopesFromCSV(CSVScopeDataDTO scopes)
        {
            return _scopeRepository.ImportScopesFromCSV(scopes, _claimService.GetUserId());
        }
        public async Task<GetAllAndSelectedScopeDTO> GetAllAndSelectedScope(int ScopeId, int ParentLevelId)
        {
            var SelectedScopes = await _scopeRelationshipRepository.GetSelectedColumnByConditionAsync(e => e.ScopeId == ScopeId && e.ParentScopeId != ScopeId && e.ParentScopeLevelId == ParentLevelId,
                e => new ScopeGetDTO
                {
                    ScopeId = e.ParentScopeId,
                    ScopeName = e.ParentAppScope.Scope.ScopeName,
                    ScopeCode = e.ParentAppScope.Scope.Value,
                    ParentScopeLevelId = e.ParentScopeLevelId,
                });

            var AllScopes = await _scopeRepository.GetSelectedColumnByConditionAsync(
                e => e.ApplicationScopes.Any(s => s.LevelId == ParentLevelId) && e.ScopeId != ScopeId,
                e => new ScopeGetDTO
                {
                    ScopeId = e.ScopeId,
                    ScopeName = e.ScopeName,
                    ScopeCode = e.Value,
                    ParentScopeLevelId = ParentLevelId                     
                });
            return new GetAllAndSelectedScopeDTO
            {
                SelectedScopes = SelectedScopes.ToList(),
                AllScopes = AllScopes.ToList()
            };
        }

        public async Task<ScopeReturnDTO> GetScopeByLevelIdWithPaginationAsync(int levelId, int offset, int limit, string filter, string search)
        {
            return await _scopeRepository.GetScopeByLevelIdWithPaginationAsync(levelId, offset, limit, filter ?? string.Empty, search ?? string.Empty);
        }
    }
}
