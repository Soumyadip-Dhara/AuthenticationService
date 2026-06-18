using AutoMapper;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL.Interfaces;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.DAL.Repositories.Master;
using UserManagement.DAL.Repositories;
using UserManagement.Models.DTO;
using System.Collections.Generic;
using UserManagement.DAL.Entities;
using UserManagement.Utils.Interfaces;
using UserManagement.Utils;

namespace UserManagement.BAL.Services.Master
{
    public class LevelService : ILevelService
    {
        private readonly IApplicationHasLevelRepository _applicationHasLevelRepository;
        private readonly IClaimService _claimService;
        private readonly ILevelRelationshipRepository _levelRelationshipRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleHasOwnAppRepository _userRoleHasOwnAppRepository;
        private readonly IUserLevelHasUserScopeRepository _userLevelHasUserScopeRepository;
        private readonly IScopeRepository _scopeRepository;
        private readonly ILevelRepository _levelRepository;
        private readonly ILevelMasterRepository _levelMasterRepository;
        private readonly IMapper _mapper;
        private readonly IScopeService _scopeService;
        private readonly IUserApplicationHasUserRoleRepository _userApplicationHasUserRoleRepository;
        private readonly IUserRoleHasUserLevelRepository _userRoleHasUserLevelRepository;
        private readonly ILevelHasAllowedRoleRepository _levelHasAllowedRoleRepository;
        private readonly IRabbitMQPublisherService _rabbitMQPublisherService;
        private readonly IApplicationRepository _applicationRepository;

        public LevelService(ILevelRepository LevelRepository, ILevelMasterRepository levelMasterRepository, IApplicationHasLevelRepository applicationHasLevelRepository, IScopeService scopeService, IMapper mapper, IClaimService claimService, IUserApplicationHasUserRoleRepository userApplicationHasUserRoleRepository, IUserRoleHasUserLevelRepository userRoleHasUserLevelRepository, ILevelRelationshipRepository levelRelationshipRepository, IRoleRepository roleRepository, IUserRoleHasOwnAppRepository userRoleHasOwnAppRepository, IUserLevelHasUserScopeRepository userLevelHasUserScopeRepository, IScopeRepository scopeRepository, ILevelHasAllowedRoleRepository levelHasAllowedRoleRepository, IRabbitMQPublisherService rabbitMQPublisherService, IApplicationRepository applicationRepository)
        {
            _levelRepository = LevelRepository;
            _applicationHasLevelRepository = applicationHasLevelRepository;
            _mapper = mapper;
            _claimService = claimService;
            _scopeService = scopeService;
            _userApplicationHasUserRoleRepository = userApplicationHasUserRoleRepository;
            _userRoleHasUserLevelRepository = userRoleHasUserLevelRepository;
            _levelRelationshipRepository = levelRelationshipRepository;
            _roleRepository = roleRepository;
            _userRoleHasOwnAppRepository = userRoleHasOwnAppRepository;
            _userLevelHasUserScopeRepository = userLevelHasUserScopeRepository;
            _scopeRepository = scopeRepository;
            _levelHasAllowedRoleRepository = levelHasAllowedRoleRepository;
            _rabbitMQPublisherService = rabbitMQPublisherService;
            _applicationRepository = applicationRepository;
            _levelMasterRepository = levelMasterRepository;
        }

        public async Task<List<LevelGetDTO>> GetLevelByApplication(int applicationId)
        {
            List<LevelGetDTO> result = (List<LevelGetDTO>)await _levelRepository.GetSelectedColumnByConditionAsync(
                entity => entity.AppId == applicationId && entity.IsDeleted != true && entity.IsActive != false,
                entity => new LevelGetDTO
                {
                    Id = entity.AppLevelId,
                    Title = entity.Level != null ? entity.Level.LevelName : string.Empty,
                    ApplicationName = entity.App != null ? entity.App.Title : string.Empty,
                    Rank = entity.Rank ?? 0,
                    AccisibleLevels = entity.LevelRelationshipAccessLevels
                        .Where(r => r.AccessLevelId == entity.AppLevelId && r.IsDeleted != true && r.IsActive != false)
                        .Select(r => r.Level.Level != null ? r.Level.Level.LevelName : string.Empty)
                        .ToList(),
                    AllowedRoles = entity.LevelHasAllowedRoles
                        .Where(e => e.LevelId == entity.AppLevelId && e.IsDeleted != true && e.IsActive != false)
                        .Select(e => e.Role.Title)
                        .ToList(),
                    AdminRole = entity.LevelHasAllowedRoles
                        .Where(e => e.LevelId == entity.AppLevelId && e.IsParentOrAdminRole == true && e.IsDeleted != true && e.IsActive != false)
                        .Select(e => e.Role.Title).ToList(),
                    SameLeveladminAllowed = entity.SameLevelOtherOfficeAdminAllowed ?? false,
                    CreatedBy = entity.CreatedByNavigation != null ? entity.CreatedByNavigation.UserName : string.Empty
                });

            return result;
        }
        //public async Task<List<LevelGetDTO>> GetLevelsByApplicationIdForDropdown(int applicationId)
        //{
        //    List<LevelGetDTO> result = (List<LevelGetDTO>)await _levelRepository.GetSelectedColumnByConditionAsync(
        //        entity => entity.AppId == applicationId && entity.IsDeleted != true && entity.IsActive != false,
        //        entity => new LevelGetDTO
        //        {
        //            Id = entity.AppLevelId,
        //            Title = entity.Level != null ? entity.Level.LevelName : string.Empty,
        //            IsGlobal = entity.Level.IsGlobal,
        //            Rank = entity.Rank
        //        });

        //    return result;
        //}

        public async Task<List<LevelGetDTO>> GetLevelsByApplicationIdForDropdown(int applicationId)
        {
            List<LevelGetDTO> result = ((List<LevelGetDTO>)await _levelRepository.GetSelectedColumnByConditionAsync(
                entity => entity.AppId == applicationId && entity.IsDeleted != true && entity.IsActive != false,
                entity => new LevelGetDTO
                {
                    Id = entity.AppLevelId,
                    Title = entity.Level != null ? entity.Level.LevelName : string.Empty,
                    IsGlobal = entity.Level.IsGlobal,
                    Rank = entity.Rank
                }))
                .OrderBy(x => x.Rank)
                .ToList();

            return result;
        }
        public async Task<List<LevelGetDTO>> GetGlobalLevels(int applicationId)
        {
            var titles = (List<string>)await _levelRepository.GetSelectedColumnByConditionAsync(entity => entity.AppId == applicationId && entity.IsActive == true  && entity.IsDeleted == false,
                entity => entity.Level != null ? entity.Level.LevelName.ToLower() : string.Empty);
            var result = (await _levelRepository.GetSelectedColumnByConditionAsync(
                entity => entity.Level != null &&
                          entity.Level.IsGlobal == true &&
                          !titles.Contains(entity.Level.LevelName.ToLower()),
                entity => new LevelGetDTO
                {
                    Id = entity.Level.LevelId,
                    Title = entity.Level.LevelName
                }))
                .DistinctBy(x => x.Id)
                .ToList();

            return result;
        }
        public async Task<List<LevelGetDTO>> GetLevelByApplicationIdForSuperAdminAndUserAdmin(int applicationId)
        {
            List<LevelGetDTO> result = (List<LevelGetDTO>)await _levelRepository.GetSelectedColumnByConditionAsync(entity => entity.AppId == applicationId,
                entity => new LevelGetDTO
                {
                    Id = entity.AppLevelId,
                    Title = entity.Level != null ? entity.Level.LevelName : string.Empty
                });

            return result;
        }
        public async Task<List<LevelGetDTO>> GetLevelByApplicationForUserAdmin(int applicationId)
        {
            List<LevelGetDTO> result = (List<LevelGetDTO>)await _levelRepository.GetSelectedColumnByConditionAsync(
                entity => entity.AppId == applicationId && (entity.Rank ?? 0) > 1,
                entity => new LevelGetDTO
                {
                    Id = entity.AppLevelId,
                    Title = entity.Level != null ? entity.Level.LevelName : string.Empty,
                    ApplicationName = entity.App != null ? entity.App.Title : string.Empty,
                    Rank = entity.Rank ?? 0,
                });

            return result;
        }
        public async Task<LevelGetDTO> GetLevelById(int levelId)
        {
            LevelGetDTO result = (LevelGetDTO)await _levelRepository.GetSingleSelectedColumnByConditionAsync(entity => entity.AppLevelId == levelId,
              entity => new LevelGetDTO
              {
                  Id = entity.AppLevelId,
                  Title = entity.Level != null ? entity.Level.LevelName : string.Empty,
                  Rank = entity.Rank ?? 0,
                  ApplicationName = entity.App != null ? entity.App.Title : string.Empty,
                  ApplicationId = entity.AppId ?? 0,
              }
           );
            return result;
        }
        public async Task<int> GetLevelCodeById(int levelId)
        {
            int levelRank = await _levelRepository.GetSingleSelectedColumnByConditionAsync(entity => entity.AppLevelId == levelId, entity => entity.Rank ?? 0);
            return levelRank;
        }

        public async Task<List<GroupItemDTO>> LevelByApplications(List<int> applicationIds)
        {
            List<GroupItemDTO> result = (List<GroupItemDTO>)await _levelRepository.GetSelectedColumnByConditionAsync(entity => applicationIds.Contains(entity.AppId ?? 0),
              entity => new GroupItemDTO
              {
                  Label = entity.App != null ? entity.App.Title : string.Empty,
                  Items = new List<ItemDTO>
                   {
                       new ItemDTO{Label = entity.Level != null ? entity.Level.LevelName : string.Empty,
                           Value = entity.AppLevelId.ToString(),
                           ParentId = entity.AppId ?? 0}
                   }
              }
            );
            List<GroupItemDTO> groupedResult = new();
            if (result != null)
            {
                groupedResult = result.GroupBy(r => r.Label)
                             .Select(group => new GroupItemDTO
                             {
                                 Label = group.Key,
                                 Items = group.SelectMany(r => r.Items).ToList()
                             })
                             .ToList();
            }

            return groupedResult;
        }
        public async Task<(bool, string)> LevelInsert(List<LevelPayloadDTO> levels)
        {
            var appName = await _applicationRepository.GetSingleSelectedColumnByConditionAsync(e => e.Id == levels[0].ApplicationId,
                e => e.Title);
            var res = await _levelRepository.CreateLevel(levels);
            if (res.Item1)
            {
                //await _rabbitMQPublisherService.PublishMessage($"{appName}-GETLEVELS", JsonSerializer.Serialize(res.Item3));
            }
            return (res.Item1, res.Item2);
        }
        public async Task<List<LevelGetDTO>> GetLevelByApplicationOtherRoleId(int applicationId, int? levelId)
        {
            var levels = new List<LevelGetDTO>();
            levels = (List<LevelGetDTO>)await _levelRelationshipRepository.GetSelectedColumnByConditionAsync(entity => entity.AccessLevelId == levelId,
                entity => new LevelGetDTO
                {
                    Id = entity.Level.AppLevelId,
                    Title = entity.Level.Level != null ? entity.Level.Level.LevelName : string.Empty,
                    ApplicationName = entity.Level.App != null ? entity.Level.App.Title : string.Empty,
                    Rank = entity.Level.Rank ?? 0,
                }
                );
            return levels;
        }
        public async Task<List<LevelGetDTO>> GetLevelsByApplicationIdRoleIdForUMOwnOffice(int applicationId, int umRoleId)
        {
            var IdToFindOriginalLevelIdFromUserRoleHAsLevelTable = await _userApplicationHasUserRoleRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.RoleId == umRoleId && e.AppId == applicationId && e.UserHasApp.UserId == _claimService.GetUserId(),
                e => e.Id
            );
            var levelData = (List<LevelGetDTO>)await _userRoleHasUserLevelRepository.GetSelectedColumnByConditionAsync(
                e => e.ApplicationHasRoleId == IdToFindOriginalLevelIdFromUserRoleHAsLevelTable,
                e => new LevelGetDTO
                {
                    Id = e.RoleHasLevelId,
                    Title = e.RoleHasLevel.Level != null ? e.RoleHasLevel.Level.LevelName : string.Empty,
                });

            return levelData;
        }
        public async Task<List<LevelGetDTO>> GetLevelByApplicationForSuperAdmin(int applicationId)
        {
            var levels = new List<LevelGetDTO>();
            if (applicationId != 5)
            {
                levels = (List<LevelGetDTO>)await _levelRepository.GetSelectedColumnByConditionAsync(
                    e => (e.AppId == applicationId && (e.Rank ?? 0) == 1),
                    e => new LevelGetDTO
                    {
                        Id = e.AppLevelId,
                        Title = e.Level != null ? e.Level.LevelName : string.Empty
                    });
            }
            else
            {
                levels = (List<LevelGetDTO>)await _levelRepository.GetSelectedColumnByConditionAsync(
                    e => (e.AppId == applicationId && (e.Rank ?? 0) == 2),
                    e => new LevelGetDTO
                    {
                        Id = e.AppLevelId,
                        Title = e.Level != null ? e.Level.LevelName : string.Empty
                    });
            }
            return levels;
        }
        public async Task<List<LevelGetDTO>> GetLevelByApplicationForApplicationUserAdmin(int applicationId)
        {
            var levels = new List<LevelGetDTO>();
            levels = (List<LevelGetDTO>)await _levelRepository.GetSelectedColumnByConditionAsync(
                e => (e.AppId == applicationId && (e.Rank ?? 0) > 1),
                e => new LevelGetDTO
                {
                    Id = e.AppLevelId,
                    Title = e.Level != null ? e.Level.LevelName : string.Empty
                });
            return levels;
        }
        public async Task<List<LevelFetchDTO>> GetOwnLevels(int applicationId)
        {
            var levels = (List<LevelFetchDTO>)await _userRoleHasUserLevelRepository.GetSelectedColumnByConditionAsync(e => e.UserHasApp.UserId == _claimService.GetUserId() && e.UserHasApp.AppId == applicationId, e => new LevelFetchDTO
            {
                LevelId = e.RoleHasLevelId,
                LevelName = e.RoleHasLevel.Level != null ? e.RoleHasLevel.Level.LevelName : string.Empty
            });
            return levels;
        }
        public async Task<List<LevelGetDTO>> GetLevelsForOwnOffice(int applicationId)
        {
            var levels = (List<LevelGetDTO>)await _userRoleHasUserLevelRepository.GetSelectedColumnByConditionAsync(e => e.UserHasApp.UserId == _claimService.GetUserId() && e.UserHasApp.AppId == applicationId, e => new LevelGetDTO
            {
                Id = e.RoleHasLevelId,
                Title = e.RoleHasLevel.Level != null ? e.RoleHasLevel.Level.LevelName : string.Empty
            });
            return levels;
        }
        public async Task<List<LevelGetDTO>> GetLevelsForOtherOffice(int applicationId)
        {

            var levels = await _userRoleHasUserLevelRepository.GetSelectedColumnByConditionAsync(e => e.UserHasApp.UserId == _claimService.GetUserId() && e.UserHasApp.AppId == applicationId, e => e.RoleHasLevelId);

            var childLevelsIncludingOwn = new List<int>();

            var childRoles = (List<int>)await _levelRelationshipRepository.GetSelectedColumnByConditionAsync(
              e => levels.Contains(e.LevelId) && e.LevelId != e.AccessLevelId,
              e => e.AccessLevelId);

            childLevelsIncludingOwn.AddRange(childRoles);

            var ownRoles = (List<int>)await _levelRepository.GetSelectedColumnByConditionAsync(
                e => levels.Contains(e.AppLevelId) && e.SameLevelOtherOfficeAdminAllowed == true,
                e => e.AppLevelId);

            childLevelsIncludingOwn.AddRange(ownRoles);

            var distinctLevelsWhichHasSameParent = new HashSet<int>(childLevelsIncludingOwn);
            var distinctLevelsList = distinctLevelsWhichHasSameParent.ToList();

            var levelData = (List<LevelGetDTO>)await _levelRepository.GetSelectedColumnByConditionAsync(
                e => distinctLevelsList.Contains(e.AppLevelId),
                e => new LevelGetDTO
                {
                    Id = e.AppLevelId,
                    Title = e.Level != null ? e.Level.LevelName : string.Empty,
                });

            return levelData;
        }
        public async Task<List<LevelFetchDTO>> GetParentsOfLevelsForScopeParentEntry(int levelid)
        {
            var levels = (List<LevelFetchDTO>)await _levelRelationshipRepository.GetSelectedColumnByConditionAsync(
                    e => e.AccessLevelId == levelid,
                    e => new LevelFetchDTO
                    {
                        LevelId = e.Level.AppLevelId,
                        LevelName = e.Level.Level != null ? e.Level.Level.LevelName : string.Empty
                    });
            return levels;
        }

        public async Task<AcessLevelAndAllLevel> GetLevelAccesByAppId(int AppId, int LevelId)
        {
            var selectedLevel = await _levelRelationshipRepository.GetSelectedColumnByConditionAsync(e => e.AccessLevelId == LevelId && e.AccessLevelId != e.LevelId,
                e => new AcesseLevel
                {
                    LevelId = e.LevelId,
                    LevelName = e.Level.Level != null ? e.Level.Level.LevelName : string.Empty
                });
            var AllLevel = await _levelRepository.GetSelectedColumnByConditionAsync(e => e.AppId == AppId && e.AppLevelId != LevelId,
                e => new AllLevel
                {
                    LevelId = e.AppLevelId,
                    LevelName = e.Level != null ? e.Level.LevelName : string.Empty
                });
            return new AcessLevelAndAllLevel
            {
                AllLevel = AllLevel.ToList(),
                AcesseLevel = selectedLevel.ToList()
            };
        }
        public async Task<(string, bool)> UpdateLevel(LevelUpdateDTO levelUpdateDTO)
        {
            var level = await _levelRepository.GetFiltered(e => e.AppLevelId == levelUpdateDTO.levelId)
                .Include(e => e.Level)
                .SingleOrDefaultAsync();
            if (level != null)
            {
                string levelName = level.Level != null ? level.Level.LevelName : string.Empty;
                var oldLevel = $"{levelName}_{level.AppId}";
                var newLevel = $"{levelUpdateDTO.name}_{level.AppId}";
                var flag = true;
                // Get all asigned level
                var parentLevels = await _levelRelationshipRepository.GetAllByConditionAsync(e => e.AccessLevelId == levelUpdateDTO.levelId && e.LevelId != levelUpdateDTO.levelId);
                var allowedRoles = await _levelHasAllowedRoleRepository.GetAllByConditionAsync(e => e.LevelId == levelUpdateDTO.levelId);

                // level Update of required fields
                if (level.Level != null) level.Level.LevelName = levelUpdateDTO.name;
                level.Rank = levelUpdateDTO.rank;
                level.SameLevelOtherOfficeAdminAllowed = levelUpdateDTO.SameLevelAdminAllowed;
                level.UpdatedBy = _claimService.GetUserId();
                level.UpdatedAt = DateTime.Now;
                if (_levelRepository.Update(level))
                {

                    if (_levelRelationshipRepository.DeleteRange(parentLevels) && _levelHasAllowedRoleRepository.DeleteRange(allowedRoles))
                    {
                        foreach (var newParentLevel in levelUpdateDTO.acesseLevel)
                        {
                            var levelRelationship = new LevelRelationship
                            {
                                LevelId = newParentLevel.LevelId,
                                AccessLevelId = levelUpdateDTO.levelId,
                            };
                            if (!_levelRelationshipRepository.Add(levelRelationship))
                            {
                                return ("Unsuccesfull Updation", false);
                            }
                        }
                        foreach (var role in levelUpdateDTO.allowedRoles)
                        {
                            var levelHasAllowedRole = new LevelHasAllowedRole
                            {
                                LevelId = levelUpdateDTO.levelId,
                                RoleId = role.id,
                                IsParentOrAdminRole = role.id == levelUpdateDTO.adminRole.id
                            };
                            if (!_levelHasAllowedRoleRepository.Add(levelHasAllowedRole))
                            {
                                return ("Unsuccesfull Updation", false);
                            }
                        }
                        //if (levelName != (level.Level != null ? level.Level.LevelName : string.Empty))
                        //{
                        //    var result = await _scopeRepository.UpdatePartitionTable(oldLevel, newLevel);
                        //    if (!result.Item1)
                        //    {
                        //        return (result.Item2, result.Item1);
                        //    }
                        //}
                            _levelHasAllowedRoleRepository.SaveChangesManaged();
                            _levelRelationshipRepository.SaveChangesManaged();
                            _levelRepository.SaveChangesManaged();
                            return ("Update Successful", true);
                    }
                    else
                    {
                        return ("Unsuccesfull Updation", false);
                    }
                //}
                //    return ("Unsuccesfull Updation", false);
                }
                else
                {
                    return ("Unsuccesfull Updation", false);
                }

            } 
            else 
            {
                return ("Item Does Not Exist!!!", false);
            }
        }

        public async Task<(string, bool)> DeleteLevel(int Id)
        {
            var res = await _levelRepository.GetSingleAysnc(p => p.AppLevelId == Id);
            var masterLevel = await _levelMasterRepository.GetSingleAysnc(p => p.LevelId == res.LevelId);
            if (res != null)
            {
                // 1. Soft Delete Level Relationships (both where it is the level or access level)
                var assignedLevel = await _levelRelationshipRepository.GetAllByConditionAsync(e => e.AccessLevelId == Id || e.LevelId == Id);
                foreach (var rel in assignedLevel)
                {
                    rel.IsDeleted = true;
                    rel.IsActive = false;
                    _levelRelationshipRepository.Update(rel);
                }

                // 2. Soft Delete Allowed Roles
                var allowedRoles = await _levelHasAllowedRoleRepository.GetAllByConditionAsync(e => e.LevelId == Id);
                foreach (var role in allowedRoles)
                {
                    role.IsDeleted = true;
                    role.IsActive = false;
                    _levelHasAllowedRoleRepository.Update(role);
                }

                // 3. Soft Delete ApplicationLevel
                string levelTitle = res.Level != null ? res.Level.LevelName : string.Empty;
                res.IsDeleted = true;
                res.IsActive = false;
                res.UpdatedAt = DateTime.Now;


                //Soft delete in the Level Master table if the level is not global
                if (masterLevel.IsGlobal == false) 
                {
                    masterLevel.IsDeleted = true;
                    masterLevel.IsActive = false;
                    res.UpdatedAt = DateTime.Now;
                    

                }


                try
                {
                    res.UpdatedBy = _claimService.GetUserId();
                    if (masterLevel.IsGlobal == false)
                    {
                        masterLevel.UpdatedBy = _claimService.GetUserId();
                    }
                }
                catch { /* Ignore if no claim context is available */ }

                if (_levelRepository.Update(res) && _levelMasterRepository.Update(masterLevel))
                {
                    
                    _levelRelationshipRepository.SaveChangesManaged();
                    _levelHasAllowedRoleRepository.SaveChangesManaged();
                    _levelRepository.SaveChangesManaged();
                    _levelMasterRepository.SaveChangesManaged();
                    return ("Level Deleted Succesful", true);
                }
                else
                {
                    return ("Level Deleted Unsuccesful", false);
                }
            }
            else
            {
                return (("Item Dose Not Exist!!!", false));
            }
        }

        public async Task<FetchRoleDataByLevel> GetAllowedRolesForSpecificLevel(int LevelId, int AppId)
        {
            var allRole = await _roleRepository.GetSelectedColumnByConditionAsync(e => e.ApplicationId == AppId,
                e => new AllRole
                {
                    id = e.Id,
                    title = e.Title
                });
            var AllowdRole = await _levelHasAllowedRoleRepository.GetSelectedColumnByConditionAsync(e => e.LevelId == LevelId ,
                e => new AllowedRole
                {
                    id = e.Role.Id,
                    title = e.Role.Title
                });
            return new FetchRoleDataByLevel
            {
                allRole = allRole.ToList(),
                allowdRoles = AllowdRole.ToList()
            };
        }

        public async Task<List<LevelGetDTO>> GetAllLevelsFromLevelMaster()
        {
            return await _scopeRepository.GetAllLevelMastersAsync();
        }
    }
}
