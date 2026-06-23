using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;

using UserManagement.Helper;
using UserManagement.Models;
using UserManagement.Models.DTO;

namespace UserManagement.Controllers.Master
{
    [Authorize("Super Admin,User Admin,Level Admin,Module Admin,IFMS USER")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class RoleController : Controller
    {
        private readonly IApplicationService _applicationService;
        private readonly IClaimService _claimService;
        private readonly IRoleHasPermissionService _roleHasPermissionService;
        private readonly IRoleRelationshipService _roleRelationshipService;
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService, IClaimService claimService, IApplicationService applicationService, IRoleRelationshipService roleRelationshipService, IRoleHasPermissionService roleHasPermissionService)
        {
            _roleService = roleService;
            _claimService = claimService;
            _applicationService = applicationService;
            _roleRelationshipService = roleRelationshipService;
            _roleHasPermissionService = roleHasPermissionService;
        }

        [HttpGet("GetRolesByApplication/{applicationId}")]
        public async Task<APIResponseClass<List<RoleGetDTO>>> GetRoleByApplication(int applicationId)
        {
            APIResponseClass<List<RoleGetDTO>> response = new();
            try
            {
                var roles = await _roleService.GetRolesByApplicationId(applicationId);
                if (roles.Count > 0)
                {
                    response.message = "Data fetched";
                }
                else
                {
                    response.message = "Data Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = roles;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }
        
        [HttpGet("GetRolesByApplicationIdForDropdown")]
        public async Task<APIResponseClass<List<RoleGetDTO>>> GetRolesByApplicationIdForDropdown(int applicationId)
        {
            APIResponseClass<List<RoleGetDTO>> response = new();
            try
            {
                var roles = await _roleService.GetRolesByApplicationIdForDropdown(applicationId);
                if (roles.Count > 0)
                {
                    response.message = "Data fetched";
                }
                else
                {
                    response.message = "Data Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = roles;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }

       
        [HttpPost("GetRolesByApplications")]
        public async Task<APIResponseClass<List<GroupItemDTO>>> GetRolesByApplications(RoleByApplicationIdsDTO roleByApplicationIdsDTO)
        {
            APIResponseClass<List<GroupItemDTO>> response = new();
            try
            {
                List<GroupItemDTO> RoleResult = new List<GroupItemDTO>();
                string[] userRoles = _claimService.GetRoles();
                if (Array.Exists(userRoles, roleName => roleName == "Super Admin"))
                {
                    RoleResult = await _roleService.GetRolesByApplications(roleByApplicationIdsDTO.applicationIds);
                }
                else
                {
                    List<int> roleIds = _claimService.GetRoleIdsByApplicationIds(roleByApplicationIdsDTO.applicationIds);
                    RoleResult = await _roleRelationshipService.RolesByAccessRoleIds(roleIds);
                }

                if (RoleResult != null)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "";
                    response.result = RoleResult;
                    return response;
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, Role Already Exists. Please try again..";
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again..";
                return response;
            }
        }

        [HttpPost("InsertNewRole")]
        public async Task<APIResponseClass<bool>> InserNewRole(RoleSetDTO roleSetDTO)
        {
            APIResponseClass<bool> response = new();
            try
            {
                RoleModel roleModel = new RoleModel();
                roleModel.Title = roleSetDTO.Role.Name;
                roleModel.ApplicationId = roleSetDTO.Role.ApplicationId;
                var retunResponce = await _roleService.InsertRole(roleModel);
                if (retunResponce.Id != 0)
                {
                    bool roleRelationshipAdded = false;
                    RoleRelationshipModel roleRelationshipModel = new RoleRelationshipModel();
                    if (roleSetDTO.VisibleToRoles != null)
                    {
                        foreach (VisibleToRoleDTO assecRole in roleSetDTO.VisibleToRoles)
                        {

                            roleRelationshipModel.RoleId = retunResponce.Id;
                            roleRelationshipModel.AccessRoleId = assecRole.Id;
                            roleRelationshipAdded = await _roleRelationshipService.Insert(roleRelationshipModel);
                        }
                    }
                    else
                    {
                        roleRelationshipModel.RoleId = retunResponce.Id;
                        roleRelationshipModel.AccessRoleId = retunResponce.Id;
                        roleRelationshipAdded = await _roleRelationshipService.Insert(roleRelationshipModel);
                    }

                    if (roleRelationshipAdded)
                    {
                        bool roleHasPermissionService = false;
                        foreach (PermissionDTO permission in roleSetDTO.Permissions)
                        {
                            RoleHasPermissionModel permissionHasPermissionModel = new RoleHasPermissionModel();
                            permissionHasPermissionModel.RoleId = retunResponce.Id;
                            permissionHasPermissionModel.PermissionId = permission.Id;
                            roleHasPermissionService = await _roleHasPermissionService.Insert(permissionHasPermissionModel);
                        }
                        if (roleHasPermissionService)
                        {
                            response.apiResponseStatus = Enum.APIResponseStatus.Success;
                            response.message = "Role create successfully";
                            response.result = true;
                            return response;
                        }
                    }
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.result = false;
                response.message = "Failed, please try again..";
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }

        [HttpGet("GetRolesOfAuthenticatedUserByApplicationId")]
        public async Task<APIResponseClass<List<RoleFetchDTO>>> GetRolesOfAuthenticatedUserByApplicationId(long userId, int applicationId)
        {
            APIResponseClass<List<RoleFetchDTO>> response = new();
            try
            {
                var result = await _roleService.GetRolesOfAuthenticatedUserByApplicationId(userId, applicationId);
                if (result.Count != 0)
                {
                    response.message = "Data Found";
                }
                else
                {
                    response.message = "Data Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = result;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }

        [HttpGet("GetLevelAndPermissionsOfAuthenticatedUserByRoleId")]
        public async Task<APIResponseClass<RoleSelectedDataFetchDTO>> GetLevelAndPermissionsOfAuthenticatedUserByRoleId(long roleId)
        {
            APIResponseClass<RoleSelectedDataFetchDTO> response = new();
            try
            {
                var result = await _roleService.GetLevelAndPermissionsOfAuthenticatedUserByRoleId(roleId);
                if (result != null)
                {
                    response.message = "Data Found";
                }
                else
                {
                    response.message = "Data Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = result;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }

        [HttpGet("GetLevelsAndPermissionsOfAuthenticatedUserByUserIdAndApplicationIdAndRoleId")]
        public async Task<APIResponseClass<RoleSelectedDataFetchDTO>> GetLevelsAndPermissionsOfAuthenticatedUserByUserIdAndApplicationIdAndRoleId(long userId, int applicationId, long roleId)
        {
            APIResponseClass<RoleSelectedDataFetchDTO> response = new();
            try
            {
                var result = await _roleService.GetLevelsAndPermissionsOfAuthenticatedUserByUserIdAndApplicationIdAndRoleId(userId, applicationId, roleId);
                if (result != null)
                {
                    response.message = "Data Found";
                }
                else
                {
                    response.message = "Data Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = result;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }

        [HttpPost("CreateRole")]
        public async Task<APIResponseClass<bool>> CreateRole(InsertRoleDTO insertRoleDTO)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _roleService.CreateRole(insertRoleDTO);
                if (res.Item1)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = res.Item2;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = res.Item2;

                }
                response.result = res.Item1;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Some error occured.. " + Ex;
                return response;
            }
        }

        [HttpPost("GetPermissionAllAndSelected")]
        public async Task<APIResponseClass<PermissionAllAndPreselectedDTO>> GetAllAndSelectedPermission(RoleSelectionUsingAppIdDTO payload)
        {
            APIResponseClass<PermissionAllAndPreselectedDTO> response = new();
            try
            {
                var result = await _roleService.GetAllAndSelectedPermission(payload);
                if (result != null)
                {
                    response.message = "Data Fetched";
                }
                else
                {
                    response.message = "Data Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = result;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }

        [HttpPost("GetAllRolesAndSelectedRoles")]
        public async Task<APIResponseClass<AllAndSelectedRoles>> GetAllRolesAndSelectedRoles(RoleSelectionUsingAppIdDTO payload)
        {
            APIResponseClass<AllAndSelectedRoles> response = new();
            try
            {
                var result = await _roleService.GetAllRolesAndSelectedRoles(payload);
                if (result != null)
                {
                    response.message = "Data Fetched";
                }
                else
                {
                    response.message = "Data Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = result;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }

        [HttpPost("UpdateRole")]
        public async Task<APIResponseClass<bool>> UpdateRole(RoleUpdateDTO roleUpdateDTO)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _roleService.UpdateRole(roleUpdateDTO);
                if (res.Item2)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = res.Item1;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = res.Item1;
                }
                response.result = res.Item2;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = Ex.Message;
                return response;
            }
        }

        [HttpDelete("DeleteRoleById")]
        public async Task<APIResponseClass<bool>> DeleteRole(int roleId)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _roleService.DeleteRole(roleId);
                if (res.Item2)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = res.Item1;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = res.Item1;
                }
                response.result = res.Item2;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = Ex.Message;
                return response;
            }
        }
    }
}
