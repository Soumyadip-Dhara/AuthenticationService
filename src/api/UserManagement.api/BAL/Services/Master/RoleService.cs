using AutoMapper;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.DAL.Repositories;
using UserManagement.DAL.Repositories.Master;
using UserManagement.Helper;
using UserManagement.Models;
using UserManagement.Models.DTO;
using UserManagement.Utils;
using UserManagement.Utils.Interfaces;
using static Dapper.SqlMapper;

namespace UserManagement.BAL.Services.Master
{

    public class RoleService : IRoleService
    {
        private readonly IApplicationHasRoleRepository _ApplicationHasRoleRepository;
        private readonly IMapper _mapper;
        private readonly IClaimService _claimService;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleHasUserLevelRepository _userRoleHasUserLevelRepository;
        private readonly IUserRoleHasUserPermissionRepository _userRoleHasUserPermissionRepository;
        private readonly IRoleRelationshipRepository _roleRelationshipRepository;
        private readonly IUserApplicationHasUserRoleRepository _userApplicationHasUserRoleRepository;
        private readonly IUserRoleHasOwnAppRepository _userRoleHasOwnAppRepository;
        private readonly IRoleHasPermissionRepository _roleHasPermissionRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly ILevelHasAllowedRoleRepository _levelHasAllowedRoleRepository;
        private readonly IRabbitMQPublisherService _rabbitMQPublisherService;
        private readonly IApplicationRepository _applicationRepository;

        public RoleService(IRoleRepository RoleRepository, IMapper mapper, IClaimService claimService,
            IApplicationHasRoleRepository applicationHasRoleRepository, 
            IUserRoleHasUserPermissionRepository userRoleHasUserPermissionRepository,
            IUserRoleHasUserLevelRepository userRoleHasUserLevelRepository, 
            IRoleRelationshipRepository roleRelationshipRepository,
            IUserApplicationHasUserRoleRepository userApplicationHasUserRoleRepository, 
            IUserRoleHasOwnAppRepository userRoleHasOwnAppRepository,
            IRoleHasPermissionRepository roleHasPermissionRepository,
            ILevelHasAllowedRoleRepository levelHasAllowedRoleRepository,
            IPermissionRepository permissionRepository, IRabbitMQPublisherService rabbitMQPublisherService, IApplicationRepository applicationRepository)
        {
            _roleRepository = RoleRepository;
            _mapper = mapper;
            _claimService = claimService;
            _ApplicationHasRoleRepository = applicationHasRoleRepository;
            _userRoleHasUserLevelRepository = userRoleHasUserLevelRepository;
            _userRoleHasUserPermissionRepository = userRoleHasUserPermissionRepository;
            _roleRelationshipRepository = roleRelationshipRepository;
            _userApplicationHasUserRoleRepository = userApplicationHasUserRoleRepository;
            _userRoleHasOwnAppRepository = userRoleHasOwnAppRepository;
            _roleHasPermissionRepository = roleHasPermissionRepository;
            _permissionRepository = permissionRepository;
            _levelHasAllowedRoleRepository = levelHasAllowedRoleRepository;
            _rabbitMQPublisherService = rabbitMQPublisherService;
            _applicationRepository = applicationRepository;
        }


        public async Task<List<GroupItemDTO>> GetRolesByApplications(List<int> applicationIds)
        {
            List<GroupItemDTO> result = (List<GroupItemDTO>)await _roleRepository.GetSelectedColumnByConditionAsync(entity => applicationIds.Contains(entity.ApplicationId),
               entity => new GroupItemDTO
               {
                   Label = entity.Application.Title,
                   Items = new List<ItemDTO>
                   {
                       new ItemDTO{Label=entity.Title,Value=entity.Id.ToString(),ParentId=entity.ApplicationId}
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

        public async Task<List<RoleGetDTO>> GetRolesByApplicationId(int applicationId)
        {
            List<RoleGetDTO> result = (List<RoleGetDTO>)await _roleRepository.GetSelectedColumnByConditionAsync(entity => entity.ApplicationId == applicationId,
                entity => new RoleGetDTO
                {
                    Id = entity.Id,
                    Title = entity.Title,
                    permissions = entity.RoleHasPermissions.Where(r => r.RoleId == entity.Id)
                        .Select(r => r.Permission.Name)
                        .ToList(),
                    VisibleTo = entity.RoleRelationshipRoles.Where(v => v.RoleId == entity.Id)
                        .Select(v => v.AccessRole.Title)
                        .ToList(),
                    ApplicationName = entity.Application.Title,
                    CreatedBy = entity.CreatedByNavigation.Name,
                    IsOperational = (bool)entity.IsOperational
                });
            return result;
        
        }
        public async Task<List<RoleGetDTO>> GetRolesByApplicationIdForDropdown(int applicationId)
        {
            List<RoleGetDTO> result = (List<RoleGetDTO>)await _roleRepository.GetSelectedColumnByConditionAsync(entity => entity.ApplicationId == applicationId,
                entity => new RoleGetDTO
                {
                    Id = entity.Id,
                    Title = entity.Title
                });
            return result;
        
        }
        
        public async Task<List<RoleGetDTO>> GetRolesByApplicationIdForSuperAdminAndUserAdmin(int applicationId)
        {
            List<RoleGetDTO> result = (List<RoleGetDTO>)await _roleRepository.GetSelectedColumnByConditionAsync(entity => entity.ApplicationId == applicationId,
                entity => new RoleGetDTO
                {
                    Id = entity.Id,
                    Title = entity.Title
                });
            return result;
        }
       
        public async Task<List<RoleGetDTO>> GetRolesByApplicationIdForApplicationUserAdmin(int applicationId)
        {
            List<RoleGetDTO> result = (List<RoleGetDTO>)await _roleRepository.GetSelectedColumnByConditionAsync(entity => entity.ApplicationId == applicationId && !entity.Title.ToLower().Contains("application admin"),
                entity => new RoleGetDTO
                {
                    Id = entity.Id,
                    Title = entity.Title,
                });
            return result;
        }

        public async Task<List<RoleGetDTO>> GetRolesByApplicationIdRoleId(int applicationId, int viewRoleId)
        {
            List<RoleGetDTO> result = (List<RoleGetDTO>)await _ApplicationHasRoleRepository.GetSelectedColumnByConditionAsync(entity => entity.ApplicationId == applicationId && entity.VisibleTo == viewRoleId,
                entity => new RoleGetDTO
                {
                    Id = entity.Role.Id,
                    Title = entity.Role.Title,
                    ApplicationName = entity.Application.Title
                }
            );
            return result;
        }
        
        public async Task<List<RoleGetDTO>> GetRolesByApplicationIdRoleIdForUM(int applicationId, int viewRoleId, string appName)
        {
            var data = (List<RoleGetDTO>)await _roleRepository.GetSelectedColumnByConditionAsync(
                e => e.ApplicationId == applicationId && e.Title.ToLower().Contains(appName) && !e.Title.ToLower().Contains("super admin"),
                e => new RoleGetDTO
                {
                    Id = e.Id,
                    Title = e.Title,
                    ApplicationName = e.Application.Title
                });
            return data;
        }

        public async Task<List<RoleGetDTO>> GetRolesByApplicationIdRoleIdForUM(int applicationId, short isOwnOffice, int levelId)
        {
            string[] userRoles = _claimService.GetRoles(); // um role
            //var currentRoleId = await _roleRepository.GetSingleSelectedColumnByConditionAsync(
            //        e => e.Title == userRoles[0] && e.ApplicationId == 1,
            //        e => e.Id
            //    );
            var ownRoleIds = (List<int>)await _userApplicationHasUserRoleRepository.GetSelectedColumnByConditionAsync(
                    e => e.UserHasApp.AppId == applicationId && e.UserHasApp.UserId == _claimService.GetUserId(),
                    e => e.RoleId
                );
            //var ownAppRoleId = await _userRoleHasOwnAppRepository.GetSingleSelectedColumnByConditionAsync(
            //        e => e.UserAppHasRoleId == userApplicationHasUserRoleId,
            //        e => e.OwnAppRoleId
            //    );
            //var roleIds = new List<int>();
            var data = new List<RoleGetDTO>();
            if (isOwnOffice == 1)
            {
                //roleIds = (List<int>)await _roleRelationshipRepository.GetSelectedColumnByConditionAsync(e => ownRoleIds.Contains(e.AccessRoleId), e => e.RoleId);
                var levelHasAllowedRoles = (List<int>)await _levelHasAllowedRoleRepository.GetSelectedColumnByConditionAsync(
                    e => e.LevelId == levelId && !ownRoleIds.Contains(e.RoleId),
                    e => e.RoleId);
                data = (List<RoleGetDTO>)await _roleRepository.GetSelectedColumnByConditionAsync(e => levelHasAllowedRoles.Contains(e.Id), e => new RoleGetDTO
                {
                    Id = e.Id,
                    Title = e.Title,
                    ApplicationName = e.Application.Title
                });

            }
            else
            {
                // own role & child role
                //roleIds = (List<int>)await _roleRelationshipRepository.GetSelectedColumnByConditionAsync(e => e.AccessRoleId == ownAppRoleId, e => e.RoleId);
                // only child role
                //roleIds = (List<int>)await _roleRelationshipRepository.GetSelectedColumnByConditionAsync(e => ownRoleIds.Contains(e.AccessRoleId) && e.AccessRoleId != e.RoleId, e => e.RoleId);
                // own role only
                // roleIds = ownRoleId;
                var levelHasAllowedRoles = (List<int>)await _levelHasAllowedRoleRepository.GetSelectedColumnByConditionAsync(
                    e => e.LevelId == levelId && e.IsParentOrAdminRole == true,
                    e => e.RoleId);
                data = (List<RoleGetDTO>)await _roleRepository.GetSelectedColumnByConditionAsync(e => levelHasAllowedRoles.Contains(e.Id), e => new RoleGetDTO
                {
                    Id = e.Id,
                    Title = e.Title,
                    ApplicationName = e.Application.Title
                });
            }

            return data;
        }

        public async Task<List<RoleGetDTO>> GetOtherOfficeRolesForUMByOwnRoleId(int applicationId, int umRoleId)
        {
            var ownAppRoleId = await _userApplicationHasUserRoleRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.RoleId == umRoleId && e.AppId == applicationId && e.UserHasApp.UserId == _claimService.GetUserId(),
                e => e.Id // todo urgent
            );
            var roleIds = (List<int>)await _roleRelationshipRepository.GetSelectedColumnByConditionAsync(e => e.AccessRoleId == ownAppRoleId, e => e.RoleId);
            var data = (List<RoleGetDTO>)await _roleRepository.GetSelectedColumnByConditionAsync(e => roleIds.Contains(e.Id) && e.Id != ownAppRoleId, e => new RoleGetDTO
            {
                Id = e.Id,
                Title = e.Title,
                ApplicationName = e.Application.Title
            });
            return data;
        }

        public async Task<int> GetRoleIdByRoleName(string roleName)
        {
            return await _roleRepository.GetSingleSelectedColumnByConditionAsync(entity => entity.Title == roleName, etity => etity.Id);
        }

        public async Task<RoleGetDTO> GetRolesByRoleId(int roleId)
        {
            RoleGetDTO result = (RoleGetDTO)await _roleRepository.GetSingleSelectedColumnByConditionAsync(entity => entity.Id == roleId,
               entity => new RoleGetDTO
               {
                   Id = entity.Id,
                   Title = entity.Title,
               }
            );
            return result;
        }

        public async Task<RoleGetDTO> InsertRole(RoleModel roleModel)
        {
            var res = await _roleRepository.GetSingleAysnc(e => e.Title == roleModel.Title);
            if (res == null)
            {
                Role role = _mapper.Map<Role>(roleModel);
                role.CreatedBy = _claimService.GetUserId();
                if (_roleRepository.Add(role))
                {
                    _roleRepository.SaveChangesManaged();
                    return _mapper.Map<RoleGetDTO>(role);
                }
            }
            return new RoleGetDTO { Id = 0, Title = "" };
        }

        public async Task<List<RoleGetDTO>> GetRolesByParentName(string roleName)
        {
            List<RoleGetDTO> result = (List<RoleGetDTO>)await _roleRepository.GetSelectedColumnByConditionAsync(entity => entity.Title == roleName,
               entity => new RoleGetDTO
               {
                   Id = entity.Id,
                   Title = entity.Title,
               }
            );

            return result;
        }

        private static bool IsParentIdContains(string? parentIds, int parentId)
        {
            if (string.IsNullOrEmpty(parentIds))
            {
                return false;
            }

            string[] parentIdArray = parentIds.Split(',')
                                               .Select(id => id.Trim()) // Remove spaces
                                               .ToArray();

            return parentIdArray.Contains(parentId.ToString());
        }

        public async Task<List<RoleFetchDTO>> GetRolesOfAuthenticatedUserByApplicationId(long userId, int applicationId)
        {
            var res = await _roleRepository.GetRolesOfAuthenticatedUserByApplicationId(userId, applicationId);
            return res;
        }

        public async Task<RoleSelectedDataFetchDTO> GetLevelAndPermissionsOfAuthenticatedUserByRoleId(long roleId)
        {
            var levels = await _userRoleHasUserLevelRepository.GetSelectedColumnByConditionAsync(e => e.ApplicationHasRoleId == roleId, e => new LevelFetchDTO
            {
                LevelId = e.Id,
                LevelName = e.RoleHasLevel.Level != null ? e.RoleHasLevel.Level.LevelName : string.Empty,
                OriginalId = e.RoleHasLevelId
            });
            var permissions = await _userRoleHasUserPermissionRepository.GetSelectedColumnByConditionAsync(e => e.ApplicationHasRoleId == roleId, e => e.RoleHasPermission.Name);
            var res = new RoleSelectedDataFetchDTO
            {
                LevelFetch = levels.ToList(),
                PermissionNames = permissions.ToList()
            };
            return res;
        }

        public async Task<RoleSelectedDataFetchDTO> GetLevelsAndPermissionsOfAuthenticatedUserByUserIdAndApplicationIdAndRoleId(long userId, int applicationId, long roleId)
        {
            //var userAppHasRolePk = await _userApplicationHasUserRoleRepository.GetSelectedColumnByConditionAsync(
            //    e => e.RoleId == roleId && e.UserHasApp.AppId == applicationId && e.UserHasApp.UserId == userId, e => e.Id);

            //var levels = await _userRoleHasUserLevelRepository.GetSelectedColumnByConditionAsync(e => userAppHasRolePk.Contains(e.ApplicationHasRoleId), e => new LevelFetchDTO
            //{
            //    LevelId = e.Id,
            //    LevelName = e.RoleHasLevel.Title,
            //    OriginalId = e.RoleHasLevelId
            //});
            
            var levels = await _userRoleHasUserLevelRepository.GetSelectedColumnByConditionAsync(e => e.ApplicationHasRoleId == roleId, e => new LevelFetchDTO
            {
                LevelId = e.Id,
                LevelName = e.RoleHasLevel.Level != null ? e.RoleHasLevel.Level.LevelName : string.Empty,
                OriginalId = e.RoleHasLevelId
            });

            //var permissions = await _userRoleHasUserPermissionRepository.GetSelectedColumnByConditionAsync(e => userAppHasRolePk.Contains(e.ApplicationHasRoleId), e => e.RoleHasPermission.Name); 
            
            var permissions = await _userRoleHasUserPermissionRepository.GetSelectedColumnByConditionAsync(e => e.ApplicationHasRoleId == roleId, e => e.RoleHasPermission.Name);
            
            var res = new RoleSelectedDataFetchDTO
            {
                LevelFetch = levels.ToList(),
                PermissionNames = permissions.ToList()
            };
            return res;
        }
        public async Task<List<RoleGetDTO>> GetRespectiveApplicationRolesByApplicationIdForSuperAdmin(int applicationId)
        {
            var rolesParentOfItself = (List<int>)await _roleRelationshipRepository.GetSelectedColumnByConditionAsync(
                e => e.RoleId == e.AccessRoleId,
                e => e.RoleId);
            var roles = (List<RoleGetDTO>)await _roleRepository.GetSelectedColumnByConditionAsync(
                e => rolesParentOfItself.Contains(e.Id) && e.ApplicationId == applicationId,
                e => new RoleGetDTO
                {
                    Id = e.Id,
                    Title = e.Title,
                });
            return roles;
        }

        public async Task<(bool, string)> CreateRole(InsertRoleDTO insertRoleDTO)
        {
            var RoleId = await _roleRepository.GetSingleSelectedColumnByConditionAsync(e => e.Title == insertRoleDTO.Name && e.ApplicationId == insertRoleDTO.ApplicationId, e =>  e.Id);
            if (RoleId > 0)
            {
                return (false, "Role Already Exists");
            }
            var res = await _roleRepository.CreateRole(insertRoleDTO);

            
            if (res.Item1)
            {
                var app_name = await _applicationRepository.GetSingleSelectedColumnByConditionAsync(
                        e=> e.Id == insertRoleDTO.ApplicationId,
                        e=> e.Title
                    );
                var roleTask = new
                {
                    Id = res.Item3,
                    RoleName = insertRoleDTO.Name,
                };
                await _rabbitMQPublisherService.PublishMessage($"{app_name}-GETROLES", roleTask);
            }

            return (res.Item1, res.Item2);
        }

        public async Task<PermissionAllAndPreselectedDTO> GetAllAndSelectedPermission(RoleSelectionUsingAppIdDTO payload)
        {
            var SelectedPermission = await _roleHasPermissionRepository.GetSelectedColumnByConditionAsync(e => e.RoleId == payload.RoleId,
                e => new SelectedPermissionDTO
                {
                    Id = e.PermissionId,
                    Name = e.Permission.Name
                });
            var AllPermission = await _permissionRepository.GetSelectedColumnByConditionAsync(e => e.ApplicationId == payload.Appid,
                e => new AllPermissionDTO
                {
                    Id = e.Id,
                    Name = e.Name
                });
            return new PermissionAllAndPreselectedDTO
            {
                AllPermission = AllPermission.ToList(),
                SelectedPermission = SelectedPermission.ToList()
            };
        }
        public async Task<AllAndSelectedRoles> GetAllRolesAndSelectedRoles(RoleSelectionUsingAppIdDTO payload)
        {
            var SelectedRoles = await _roleRelationshipRepository.GetSelectedColumnByConditionAsync(e => e.RoleId == payload.RoleId && e.AccessRoleId != payload.RoleId,
                e => new SelectedRolesDTO
                {
                    Id = e.AccessRoleId,
                    Name = e.AccessRole.Title
                });
            var AllRoles = await _roleRepository.GetSelectedColumnByConditionAsync(e => e.ApplicationId == payload.Appid && e.Id != payload.RoleId,
                e => new AllRolesDTO
                {
                    Id = e.Id,
                    Name = e.Title
                });
            return new AllAndSelectedRoles
            {
                AllRoles = AllRoles.ToList(),
                SelectedRoles = SelectedRoles.ToList()
            };
        }
        //public async Task<(string, bool)> UpdateRole(RoleUpdateDTO roleUpdateDTO)
        //{
        //    var role =  await _roleRepository.GetSingleAysnc(e => e.Id == roleUpdateDTO.Id);

        //    if (role != null)
        //    {
        //        var assignedPermission = await _roleHasPermissionRepository.GetAllByConditionAsync(e => e.RoleId == roleUpdateDTO.Id);

        //        //var s1 = (HashSet<int>) assignedPermission.ToArray().Select(p => p.Id);
        //        //var s2 = (HashSet<int>) roleUpdateDTO.Permissions.ToArray().Select(p => p.Id);

        //        //var deletepermissions = s1.Except(s2).ToArray();
        //        //var addpermissions = s2.Except(s2).ToArray();

        //        var visibleRoles = await _roleRelationshipRepository.GetAllByConditionAsync(e => e.RoleId == roleUpdateDTO.Id && e.AccessRoleId != roleUpdateDTO.Id);


        //        role.Title = roleUpdateDTO.Name;
        //        role.UpdatedBy = _claimService.GetUserId();
        //        role.UpdatedAt = DateTime.Now;

        //        if (_roleRepository.Update(role))
        //        {
        //            _roleHasPermissionRepository.DeleteRange(assignedPermission);
        //            _roleRelationshipRepository.DeleteRange(visibleRoles);

        //            foreach (var permission in roleUpdateDTO.Permissions)
        //            {

        //                var newRoleHasPermission = new RoleHasPermission
        //                {
        //                    RoleId = roleUpdateDTO.Id,
        //                    PermissionId = permission.Id
        //                };
        //                _roleHasPermissionRepository.Add(newRoleHasPermission);
        //            }
        //            foreach (var visibleRole in roleUpdateDTO.Roles)
        //            {

        //                var roleRelationship = new RoleRelationship
        //                {
        //                    RoleId = roleUpdateDTO.Id,
        //                    AccessRoleId = visibleRole.Id
        //                };
        //                _roleRelationshipRepository.Add(roleRelationship);
        //            }
        //            _roleRepository.SaveChangesManaged();
        //            _roleHasPermissionRepository.SaveChangesManaged();
        //            _roleRelationshipRepository.SaveChangesManaged();
        //        }
        //        return ("Update Successful", true);
        //    }
        //    else
        //    {
        //        return ("Item Does Not Exist!!!", false);
        //    }
        //}
        public async Task<(string, bool)> UpdateRole(RoleUpdateDTO roleUpdateDTO)
        {
            return await _roleRepository.UpdateRole(roleUpdateDTO);
        }
        public async Task<(string, bool)> DeleteRole(int Id)
        {
            var res = _roleRepository.GetSingle(p => p.Id == Id);
            if (res != null)
            {
                if (_roleRepository.Delete(res))
                {
                    _roleRepository.SaveChangesManaged();
                    var assignedRole = await _roleHasPermissionRepository.GetAllByConditionAsync(e => e.RoleId == Id);
                    _roleHasPermissionRepository.DeleteRange(assignedRole);
                    _roleHasPermissionRepository.SaveChangesManaged();
                    return ("Role Deleted Succesful", true);
                }
                else
                {
                    return ("Role Deleted Unsuccesful", false);
                }
            }
            else
            {
                return (("Item Dose Not Exist!!!", false));
            }
        }

    }
}
