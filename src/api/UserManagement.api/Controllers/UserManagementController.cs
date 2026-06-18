using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL;
using UserManagement.Filters;
using UserManagement.Helper;
using UserManagement.Models.DTO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UserManagement.Controllers
{
    [Authorize("Super Admin,User Admin,Level Admin,IFMS USER")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class UserManagementController : Controller
    {
        private readonly IConfiguration _config;
        private readonly UserManagementDBContext _context;
        private readonly ITempHrmService _tempHrmService;
        private readonly IUserService _userService;
        private readonly IApplicationService _applicationService;
        private readonly IJWTService _jwtService;
        private readonly IRoleService _roleService;
        private readonly ILevelService _levelService;
        private readonly IClaimService _claimService;
        private readonly ILevelRelationshipService _levelRelationshipService;
        private readonly IScopeService _scopeService;

        public UserManagementController(UserManagementDBContext context, IConfiguration config, IUserService userService, ITempHrmService tempHrmService, IApplicationService applicationService, IJWTService jwtService, IRoleService roleService, IClaimService claimService, ILevelService levelService, IScopeService scopeService, ILevelRelationshipService levelRelationshipService)
        {
            _context = context;
            _config = config;
            _userService = userService;
            _tempHrmService = tempHrmService;
            _applicationService = applicationService;
            _jwtService = jwtService;
            _roleService = roleService;
            _levelService = levelService;
            _claimService = claimService;
            _levelRelationshipService = levelRelationshipService;
            _scopeService = scopeService;
        }

        [HttpGet("Application")]
        public async Task<APIResponseClass<List<ApplicationGetDTO>>> GetApplicationsForUM()
        {
            APIResponseClass<List<ApplicationGetDTO>> response = new();
            try
            {
                List<ApplicationGetDTO> applicationResult = new List<ApplicationGetDTO>();
                string[] userRoles = _claimService.GetRoles();
                if (Array.Exists(userRoles, roleName => roleName == "Super Admin")) // L1 L2
                {
                    applicationResult = await _applicationService.GetAllApplicationsForSuperAdmin();
                }
                else
                {
                    applicationResult = await _applicationService.GetApplicationsByUserIdForUM(_claimService.GetUserId());
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Data collected successfully";
                response.result = applicationResult;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }

        [HttpGet("Role")]
        public async Task<APIResponseClass<List<RoleGetDTO>>> Role(int applicationId, short officeCode, int levelId)
        {
            // applicationId must be the appId that is selected from the frontend.
            APIResponseClass<List<RoleGetDTO>> response = new();
            try
            {
                List<RoleGetDTO> RoleResult = new List<RoleGetDTO>();
                string[] userRoles = _claimService.GetRoles();
                if (Array.Exists(userRoles, roleName => roleName == "Super Admin"))
                {
                    RoleResult = await _roleService.GetRolesByApplicationIdForSuperAdminAndUserAdmin(applicationId);
                }
                // if requirement changes then change the service for user admin (this is done intentionally)
                else if (Array.Exists(userRoles, roleName => roleName.ToLower().Contains("user admin")))
                {
                    RoleResult = await _roleService.GetRolesByApplicationIdForSuperAdminAndUserAdmin(applicationId);
                }
                else if (officeCode == 1 || officeCode == 2)
                {
                    RoleResult = await _roleService.GetRolesByApplicationIdRoleIdForUM(applicationId, officeCode, levelId);
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Bad Request. Wrong office id provided.";
                    response.result = null;
                    return response;
                }
                if (RoleResult.Count > 0)
                {
                    response.message = "Data Fetched";
                }
                else
                {
                    response.message = "Data Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = RoleResult;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }

        [HttpGet("Level")]
        public async Task<APIResponseClass<List<LevelGetDTO>>> Level(int applicationId, short officeCode)
        {
            APIResponseClass<List<LevelGetDTO>> response = new();
            try
            {
                List<LevelGetDTO> levelResult = new List<LevelGetDTO>();
                string[] userRoles = _claimService.GetRoles();
                int roleId = _claimService.GetRoleIdByApplicationId(applicationId);

                if (Array.Exists(userRoles, roleName => roleName == "Super Admin"))
                {
                    levelResult = await _levelService.GetLevelByApplicationIdForSuperAdminAndUserAdmin(applicationId);
                }
                // if requirement changes then change the service for user admin (this is done intentionally)
                else if (Array.Exists(userRoles, roleName => roleName.ToLower().Contains("user admin")))
                {
                    levelResult = await _levelService.GetLevelByApplicationIdForSuperAdminAndUserAdmin(applicationId);
                }
                else if (officeCode == 1)
                {
                    // own office levels 
                    levelResult = await _levelService.GetLevelsForOwnOffice(applicationId);
                }
                else if (officeCode == 2)
                {
                    // Other office levels 
                    levelResult = await _levelService.GetLevelsForOtherOffice(applicationId);
                }
                if (levelResult.Count > 0)
                {
                    response.message = "Data Fetched";
                }
                else
                {
                    response.message = "Data Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = levelResult;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }

        [HttpGet("Scope")]
        public async Task<APIResponseClass<ScopeFetchReturnDTO>> Scope(int appId, int levelId, int officeCode, int offset, int limit, string search = "")
        {
            APIResponseClass<ScopeFetchReturnDTO> response = new();
            try
            {
                string[] userRoles = _claimService.GetRoles();
                string msg = "Data Not Found";
                var scopeData = new ScopeFetchReturnDTO();

                if (Array.Exists(userRoles, roleName => roleName == "Super Admin") || Array.Exists(userRoles, roleName => roleName.ToLower().Contains("user admin")))
                {
                    scopeData = await _scopeService.GetScopeByLevelIdSuperAdminAndUserAdmin(levelId, offset, limit, search);
                }
                else if (officeCode == 1)
                {
                    // own office scopes 
                    scopeData = await _scopeService.GetScopeByLevelIdForOwnOffice(appId, levelId, offset, limit, search);
                }
                else if (officeCode == 2)
                {
                    // Other office scopes 
                    var res = await _scopeService.GetScopeByLevelIdForOtherOffice(appId, levelId, offset, limit, search);
                    scopeData = res.Item2;
                    msg = res.Item1;
                }

                if (scopeData.Scopes.Count != 0)
                {
                    response.message = "Data Fetched";
                }
                else
                {
                    response.message = msg;
                }

                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = scopeData;

                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }

        }

        // super admin creation
        [Authorize("Super Admin")]
        [HttpGet("ApplicationForSuperAdminCreation")]
        public async Task<APIResponseClass<List<ApplicationGetDTO>>> GetApplicationForSuperAdminCreation()
        {
            APIResponseClass<List<ApplicationGetDTO>> response = new();
            try
            {
                List<ApplicationGetDTO> applicationResult = new List<ApplicationGetDTO>();
                string[] userRoles = _claimService.GetRoles();
                if (Array.Exists(userRoles, roleName => roleName == "Super Admin"))
                {
                    applicationResult = await _applicationService.GetAllApplications();
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Data collected successfully";
                response.result = applicationResult;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }

        [Authorize("Super Admin,User Admin")]
        [HttpPost("GetDataForAdminManagement")]
        public async Task<APIResponseClass<object>> GetDataForAdminManagement(SearchDataForAdminManagementDTO data)
        {
            var response = new APIResponseClass<object>();
            try
            {
                response.result = await _userService.GetDataForAdminManagement(data);
                response.message = "Data Fetched";
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = ex.Message;
            }
            return response;
        }
        [Authorize("Super Admin,User Admin")]
        [HttpPost("ManageAdmin")]
        public async Task<APIResponseClass<object>> ManageAdmin(AdminManagementDTO data)
        {
            var response = new APIResponseClass<object>();
            try
            {
                var res = await _userService.ManageAdmin(data);
                response.result = new 
                {
                    Status = res.Item1,
                    Message = res.Item2
                };
                response.message = "Data Fetched";
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = ex.Message;
            }
            return response;
        }
    }
}
