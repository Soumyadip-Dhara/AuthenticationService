using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;

using UserManagement.Helper;
using UserManagement.Models.DTO;

namespace UserManagement.Controllers.Master
{
    [Authorize("Super Admin,User Admin,Level Admin,Module Admin,IFMS USER")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class LevelController : Controller
    {
        private readonly IClaimService _claimService;
        private readonly ILevelService _levelService;
        private ILevelRelationshipService _levelRelationshipService;
        private readonly IScopeService _scopeService;

        public LevelController(ILevelService levelService, IClaimService claimService, ILevelRelationshipService levelRelationshipService, IScopeService scopeService)
        {
            _levelService = levelService;
            _claimService = claimService;
            _levelRelationshipService = levelRelationshipService;
            _scopeService = scopeService;
        }

        [HttpGet("GetLevelByApplication/{applicationId}")]
        public async Task<APIResponseClass<List<LevelGetDTO>>> GetLevels(int applicationId)
        {
            APIResponseClass<List<LevelGetDTO>> response = new();
            try
            {
                var level = await _levelService.GetLevelByApplication(applicationId);
                if (level.Count > 0)
                {
                    response.message = "Data Collected";
                }
                else
                {
                    response.message = "Data Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = level;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }
        
        [HttpGet("GetLevelsByApplicationIdForDropdown")]
        public async Task<APIResponseClass<List<LevelGetDTO>>> GetLevelByApplicationId(int applicationId)
        {
            APIResponseClass<List<LevelGetDTO>> response = new();
            try
            {
                var levels = await _levelService.GetLevelsByApplicationIdForDropdown(applicationId);
                if (levels.Count > 0)
                {
                    response.message = "Data fetched";
                }
                else
                {
                    response.message = "Data Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = levels;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }
        
        [HttpGet("GetGlobalLevels")]
        public async Task<APIResponseClass<List<LevelGetDTO>>> GetGlobalLevels(int applicationId)
        {
            APIResponseClass<List<LevelGetDTO>> response = new();
            try
            {
                var levels = await _levelService.GetGlobalLevels(applicationId);
                if (levels.Count > 0)
                {
                    response.message = "Data fetched";
                }
                else
                {
                    response.message = "Data Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = levels;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }
        // new

        [HttpGet("GetLevelsByApplicationForUM")]
        public async Task<APIResponseClass<List<LevelGetDTO>>> GetLevelsByApplicationForUM(int applicationId, short isOwnOffice = 1)
        {
            APIResponseClass<List<LevelGetDTO>> response = new();
            try
            {
                List<LevelGetDTO> levelResult = new List<LevelGetDTO>();
                string[] userRoles = _claimService.GetRoles();
                int roleId = _claimService.GetRoleIdByApplicationId(applicationId);
                if (Array.Exists(userRoles, roleName => roleName == "Super Admin"))
                {
                    levelResult = await _levelService.GetLevelByApplicationForSuperAdmin(applicationId);
                }
                else if (Array.Exists(userRoles, roleName => roleName.ToLower().Contains("user admin")) && isOwnOffice == 2)
                {
                    levelResult = await _levelService.GetLevelByApplicationForApplicationUserAdmin(applicationId);
                }
                else if (isOwnOffice == 2)
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
                response.result = levelResult;
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }

        [HttpGet("GetOtherOfficeLevelsForUMByUMAppIdAndOwnRoleId")]
        public async Task<APIResponseClass<List<LevelGetDTO>>> GetOtherOfficeLevelsForUMByUMAppIdAndOwnRoleId(int applicationId, short isOwnOffice)
        {
            APIResponseClass<List<LevelGetDTO>> response = new();
            try
            {
                List<LevelGetDTO> levelResult = new List<LevelGetDTO>();
                string[] userRoles = _claimService.GetRoles();
                int roleId = _claimService.GetRoleIdByApplicationId(applicationId);
                if (Array.Exists(userRoles, roleName => roleName == "Super Admin"))
                {
                    levelResult = await _levelService.GetLevelByApplication(applicationId);
                }
                else if (isOwnOffice == 2)
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
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Failed, please try again.." + Ex.Message;
                return response;
            }
        }

        [HttpPost("GetLevelByApplications")]
        public async Task<APIResponseClass<List<GroupItemDTO>>> GetLevelByApplications(LevelByApplicationsDTO levelByApplicationsDTO)
        {
            APIResponseClass<List<GroupItemDTO>> response = new();
            try
            {
                List<GroupItemDTO> result = new();
                string[] userRoles = _claimService.GetRoles();
                if (Array.Exists(userRoles, roleName => roleName == "Super Admin"))
                {
                    result = await _levelService.LevelByApplications(levelByApplicationsDTO.applicationIds);
                }
                else
                {
                    List<int> levelIds = _claimService.GetLevelIdsByApplicationIds(levelByApplicationsDTO.applicationIds);
                    result = await _levelRelationshipService.LevelByAccessLevelIds(levelIds);
                }
                if (result != null)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "";
                    response.result = result;
                    return response;
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Faild";
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Faild";
                return response;
            }
        }

        [HttpPost("InsertNewLevel")]
        public async Task<APIResponseClass<bool>> InsertNewLevel(List<LevelPayloadDTO> levels)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _levelService.LevelInsert(levels);
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

        [HttpGet("GetchOwnLevelsForUM")]
        public async Task<APIResponseClass<List<LevelFetchDTO>>> GetchOwnLevelsForUM(int applicationId)
        {
            APIResponseClass<List<LevelFetchDTO>> response = new();
            try
            {
                //var ownRoleID
                var res = await _levelService.GetOwnLevels(applicationId);
                if (res.Count() > 0)
                {
                    response.message = "Data fetched";
                }
                else
                {
                    response.message = "Data Not Found";

                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = res;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Some error occured.. " + Ex;
                return response;
            }
        }
        [HttpGet("GetParentsOfLevelsForScopeParentEntry")]
        public async Task<APIResponseClass<List<LevelFetchDTO>>> GetParentsOfLevelsForScopeParentEntry(int levelId)
        {
            APIResponseClass<List<LevelFetchDTO>> response = new();
            try
            {
                //var ownRoleID
                var res = await _levelService.GetParentsOfLevelsForScopeParentEntry(levelId);
                if (res.Count() > 0)
                {
                    response.message = "Data fetched";
                }
                else
                {
                    response.message = "Data Not Found";

                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = res;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Some error occured.. " + Ex;
                return response;
            }
        }

        [HttpGet("GetLevelAccesByAppId")]
        public async Task<APIResponseClass<AcessLevelAndAllLevel>> GetLevelAccesByAppId(int AppId, int LevelId)
        {
            APIResponseClass<AcessLevelAndAllLevel> response = new();
            try
            {
                var result = await _levelService.GetLevelAccesByAppId(AppId, LevelId);
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

        [HttpPost("UpdateLevel")]
        public async Task<APIResponseClass<bool>> UpdateLevel(LevelUpdateDTO levelUpdateDTO)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _levelService.UpdateLevel(levelUpdateDTO);
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

        [HttpDelete("DeleteLevelById")]
        public async Task<APIResponseClass<bool>> DeleteLevel(int levelId)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _levelService.DeleteLevel(levelId);
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
        [HttpGet("GetAllowedRolesForSpecificLevel")]
        public async Task<APIResponseClass<FetchRoleDataByLevel>> GetAllowedRolesForSpecificLevel(int AppId, int levelId)
        {
            APIResponseClass<FetchRoleDataByLevel> response = new();
            try
            {
                var result = await _levelService.GetAllowedRolesForSpecificLevel(levelId, AppId);
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

        [HttpGet("getLeveid")]
        public async Task<APIResponseClass<List<LevelGetDTO>>> GetLeveid()
        {
            APIResponseClass<List<LevelGetDTO>> response = new();
            try
            {
                var result = await _levelService.GetAllLevelsFromLevelMaster();
                if (result != null && result.Count > 0)
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

        [HttpPost("getScopebylevelId")]
        public async Task<APIResponseClass<ScopeReturnDTO>> GetScopebylevelId([FromBody] ScopePaginationRequestDTO request)
        {
            APIResponseClass<ScopeReturnDTO> response = new();
            try
            {
                var result = await _scopeService.GetScopeByLevelIdWithPaginationAsync(
                    request.LevelId, 
                    request.Offset, 
                    request.Limit, 
                    request.Filter ?? string.Empty, 
                    request.Search ?? string.Empty);
                    
                if (result != null && result.Scopes != null && result.Scopes.Count > 0)
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
    }
}
