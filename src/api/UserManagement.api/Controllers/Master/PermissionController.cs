using Microsoft.AspNetCore.Mvc;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.Filters;
using UserManagement.Helper;
using UserManagement.Models.DTO;

namespace UserManagement.Controllers.Master
{
    [Authorize("Super Admin,User Admin,Level Admin,Module Admin,IFMS USER")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class PermissionController : Controller
    {
        private readonly IPermissionService _permissionService;
        private readonly IRoleHasPermissionService _roleHasPermission;

        public PermissionController(IPermissionService permissionService, IRoleHasPermissionService roleHasPermissionService)
        {
            _permissionService = permissionService;
            _roleHasPermission = roleHasPermissionService;
        }

        [HttpGet("GetApplicationPermissionByApplicationId")]
        public async Task<APIResponseClass<List<PermissionGetDTO>>> GetApplicationPermissionByApplicationId(int ApplicationId)
        {
            APIResponseClass<List<PermissionGetDTO>> response = new();
            try
            {
                var permissions = await _permissionService.GetPermissionByApplicationId(ApplicationId);
                if (permissions.Count > 0)
                {
                    response.message = "Data Collected";
                }
                else
                {
                    response.message = "No data found in the records";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = permissions;
                return response;

            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Error";
                return response;
            }

        } 
        
        [HttpGet("GetPermissionsByApplicationIdForViewOnly")]
        public async Task<APIResponseClass<List<PermissionGetDTO>>> GetPermissionsByApplicationIdForViewOnly(int ApplicationId)
        {
            APIResponseClass<List<PermissionGetDTO>> response = new();
            try
            {
                var permissions = await _permissionService.GetPermissionByApplicationIdForViewOnly(ApplicationId);
                if (permissions.Count > 0)
                {
                    response.message = "Data Collected";
                }
                else
                {
                    response.message = "No data found in the records";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = permissions;
                return response;

            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Error";
                return response;
            }

        }
        [HttpGet("GetPermissionsNotAssignedToARoleByRoleId")]
        public async Task<APIResponseClass<List<PermissionGetDTO>>> GetPermissionsNotAssignedToARoleByRoleId(int roleId, int applicationId)
        {
            APIResponseClass<List<PermissionGetDTO>> response = new();
            try
            {
                var permissions = await _permissionService.GetPermissionsNotAssignedToARoleByRoleId(roleId, applicationId);
                if (permissions.Count > 0)
                {
                    response.message = "Data Collected";
                }
                else
                {
                    response.message = "No data found in the records";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = permissions;
                return response;

            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Error" + Ex;
                return response;
            }

        }

        [HttpPost("GetPermissionByRoles")]
        public async Task<APIResponseClass<List<PermissionGetDTO>>> GetPermissionByRoles(PermissionByRoleDTO permissionByRoleDTO)
        {
            APIResponseClass<List<PermissionGetDTO>> response = new();
            try
            {
                List<PermissionGetDTO> result = new();
                result = await _roleHasPermission.PermissionByRoleIds(permissionByRoleDTO.roleIds[0]);
                if (result.Count > 0)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "Data Found";
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "No Data Found";
                }
                response.result = result;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again..";
                return response;
            }
        }

        [HttpPost("CreateNewPermission")]
        public async Task<APIResponseClass<bool>> CreatePermission(List<PermissionSetDTO> permissionSetDTO)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _permissionService.InsertPermission(permissionSetDTO);
                if (res)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "Permission create successfully.";
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Permission create Failed.";
                }
                response.result = res;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = Ex.Message;
                return response;
            }

        }

        [HttpPost("UpdatePermissionById")]
        public async Task<APIResponseClass<bool>> UpdatePermission(PermissionUpdateDTO permissionUpdateDTO)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _permissionService.UpdatePermission(permissionUpdateDTO);
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
        [HttpDelete("DeletePermissionById")]
        public async Task<APIResponseClass<bool>> DeletePermission(int permissionId)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _permissionService.DeletePermission(permissionId);
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
