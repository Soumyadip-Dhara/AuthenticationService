using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.BAL.Interfaces;

using UserManagement.Helper;
using UserManagement.Models.DTO;

namespace UserManagement.Controllers
{
    //[Authorize("Super Admin")]
    [Authorize("Super Admin,User Admin,IFMS USER,Module Admin")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }
        [HttpGet("GetMonthlyLogedInUser")]
        public async Task<APIResponseClass<List<DailyUserLoginDTO>>> GetMonthlyLogedInUser(string? period)
        {
            APIResponseClass<List<DailyUserLoginDTO>> response = new();
            try
            {
                var res = await _dashboardService.GetMonthlyLoggedInUser(period);
                if (res == null)
                {
                    response.message = "No User Statstics Found";
                }
                else
                {
                    response.message = "User Statstics Found Successfully";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = res;
                return response;
            }
            catch (Exception e)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Something went wrong " + e.Message;
                return response;
            }
        }
        [HttpGet("DashboardSummary")]
        public async Task<APIResponseClass<DashboardSummaryDTO>> GetDashboardSummary()
        {
            APIResponseClass<DashboardSummaryDTO> response = new();
            try
            {
                var result = await _dashboardService.GetDashboardSummaryAsync();
                if (result == null)
                {
                    response.message = "No Dashboard Summary Found";
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.result = null;
                }
                else
                {
                    response.message = "Dashboard Summary Found Successfully";
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.result = result;
                }
                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Something went wrong: " + ex.Message;
                return response;
            }
        }
        [HttpGet("MostLoggedInUsers")]
        public async Task<APIResponseClass<List<MostLoggedInUsersDTO>>> MostLoggedInUsers()
        {
            APIResponseClass<List<MostLoggedInUsersDTO>> response = new();
            try
            {
                var result = await _dashboardService.MostLoggedInUsers();
                if (result == null)
                {
                    response.message = "No Dashboard Summary Found";
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.result = null;
                }
                else
                {
                    response.message = "Dashboard Summary Found Successfully";
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.result = result;
                }
                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Something went wrong: " + ex.Message;
                return response;
            }
        }

        [HttpGet("GetLogInLogData")]
        public async Task<APIResponseClass<List<MostLoggedInUsersDTO>>> GetLogInLogData()
        {
            APIResponseClass<List<MostLoggedInUsersDTO>> response = new();
            try
            {
                var result = await _dashboardService.MostLoggedInUsers();
                if (result == null)
                {
                    response.message = "No Dashboard Summary Found";
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.result = null;
                }
                else
                {
                    response.message = "Dashboard Summary Found Successfully";
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.result = result;
                }
                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Something went wrong: " + ex.Message;
                return response;
            }
        }
        [HttpGet("RecentlyUsersCreated")]
        public async Task<APIResponseClass<List<RecentlyUsersCreatedDTO>>> RecentlyUsersCreated()
        {
            APIResponseClass<List<RecentlyUsersCreatedDTO>> response = new();
            try
            {
                var result = await _dashboardService.RecentlyUsersCreated();
                if (result == null)
                {
                    response.message = "No Recently Created Users Found";
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.result = null;
                }
                else
                {
                    response.message = "Recently Created Users Found Successfully";
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.result = result;
                }
                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Something went wrong: " + ex.Message;
                return response;
            }
        }
        [HttpGet("GetMonthlyRegisteredUserCount")]
        public async Task<APIResponseClass<List<MonthlyUserLoginCountDTO>>> GetMonthlyRegisteredUserCount()
        {
            APIResponseClass<List<MonthlyUserLoginCountDTO>> response = new();
            try
            {
                var res = await _dashboardService.GetMonthlyLoggedInUserCount();
                if (res == null)
                {
                    response.message = "No User Statstics Found";
                }
                else
                {
                    response.message = "User Statstics Found Successfully";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = res;
                return response;
            }
            catch (Exception e)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Something went wrong " + e.Message;
                return response;
            }
        }

        [HttpPost("GetDailyUserActivity")]
        public async Task<APIResponseClass<PaginatedResult<UserActivityLogDTO>>> GetDailyUserActivity(
    ActivityLogFilterDTO filter)
        {
            APIResponseClass<PaginatedResult<UserActivityLogDTO>> response = new();

            try
            {
                var res = await _dashboardService.GetDailyUserActivity(filter);

                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = res.TotalRecords > 0
                    ? "User Statistics Found Successfully"
                    : "No User Statistics Found";

                response.result = res;

                return response;
            }
            catch (Exception e)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Something went wrong " + e.Message;
                return response;
            }
        }

        //[HttpPost("GetDailyUserActivity")]
        //public async Task<APIResponseClass<List<UserActivityLogDTO>>> GetDailyUserActivity(ActivityLogFilterDTO activityLogFilterDTO)
        //{
        //    APIResponseClass<List<UserActivityLogDTO>> response = new();
        //    try
        //    {
        //        var res = await _dashboardService.GetDailyUserActivity(activityLogFilterDTO);
        //        if (res == null)
        //        {
        //            response.message = "No User Statstics Found";
        //        }
        //        else
        //        {
        //            response.message = "User Statstics Found Successfully";
        //        }
        //        response.apiResponseStatus = Enum.APIResponseStatus.Success;
        //        response.result = res;
        //        return response;
        //    }
        //    catch (Exception e)
        //    {
        //        response.apiResponseStatus = Enum.APIResponseStatus.Error;
        //        response.message = "Something went wrong " + e.Message;
        //        return response;
        //    }
        //}


        //[HttpGet("GetDeviceLogInData")]
        //public async Task<APIResponseClass<List<DeviceLoginCount>>> GetDeviceLogInData()
        //{
        //    APIResponseClass<List<DeviceLoginCount>> response = new();
        //    try
        //    {
        //        var result = await _dashboardService.GetDeviceLoginsAsync();
        //        if (result == null || result.Count == 0)
        //        {
        //            response.message = "No Device Login Data Found";
        //            response.apiResponseStatus = Enum.APIResponseStatus.Error;
        //            response.result = null;
        //        }
        //        else
        //        {
        //            response.message = "Device Login Data Retrieved Successfully";
        //            response.apiResponseStatus = Enum.APIResponseStatus.Success;
        //            response.result = result;
        //        }
        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.apiResponseStatus = Enum.APIResponseStatus.Error;
        //        response.message = "Something went wrong: " + ex.Message;
        //        response.result = null;
        //        return response;
        //    }
        //}

        [HttpGet("GetDeviceLogInData")]
        public async Task<APIResponseClass<object>> GetDeviceLogInData()
        {
            APIResponseClass<object> response = new();
            try
            {
                var result = await _dashboardService.GetDeviceLoginsAsync();

                if (result == null || result.Count == 0)
                {
                    response.message = "No Device Login Data Found";
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.result = null;
                }
                else
                {
                    var devices = result.Select(r => r.Device).ToList();
                    var counts = result.Select(r => r.TotalLogins).ToList();

                    response.message = "Device Login Data Retrieved Successfully";
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.result = new
                    {
                        devices,
                        counts
                    };
                }
                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Something went wrong: " + ex.Message;
                response.result = null;
                return response;
            }
        }

        //[HttpGet("GetAgentLogInData")]
        //public async Task<APIResponseClass<List<AgentLoginCount>>> GetAgentLogInData()
        //{
        //    APIResponseClass<List<AgentLoginCount>> response = new();
        //    try
        //    {
        //        var result = await _dashboardService.GetAgentLoginsAsync();
        //        if (result == null || result.Count == 0)
        //        {
        //            response.message = "No Agent Login Data Found";
        //            response.apiResponseStatus = Enum.APIResponseStatus.Error;
        //            response.result = null;
        //        }
        //        else
        //        {
        //            response.message = "Agent Login Data Retrieved Successfully";
        //            response.apiResponseStatus = Enum.APIResponseStatus.Success;
        //            response.result = result;
        //        }
        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.apiResponseStatus = Enum.APIResponseStatus.Error;
        //        response.message = "Something went wrong: " + ex.Message;
        //        response.result = null;
        //        return response;
        //    }
        //}
        [HttpGet("GetAgentLogInData")]
        public async Task<APIResponseClass<object>> GetAgentLogInData()
        {
            APIResponseClass<object> response = new();
            try
            {
                var result = await _dashboardService.GetAgentLoginsAsync();

                if (result == null || result.Count == 0)
                {
                    response.message = "No Agent Login Data Found";
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.result = null;
                }
                else
                {
                    var agents = result.Select(r => r.Agent).ToList();  
                    var counts = result.Select(r => r.TotalLogins).ToList(); 

                    response.message = "Agent Login Data Retrieved Successfully";
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.result = new
                    {
                        agents,
                        counts
                    };
                }
                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Something went wrong: " + ex.Message;
                response.result = null;
                return response;
            }
        }

        [HttpGet("GetCurrentLoggedInUserCount")]
        public async Task<APIResponseClass<List<UserLoginCountDTO>>> GetCurrentLoggedInUserCount()
        {
            APIResponseClass<List<UserLoginCountDTO>> response = new();
            try
            {
                var count = await _dashboardService.GetCurrentLoggedInUserCount();
                response.result = count;
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Current Logged In User Count Retrieved Successfully";
                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Something went wrong: " + ex.Message;
                return response;
            }
        }



        [HttpPost("GetUserSessionActivityLogs")]
        public async Task<APIResponseClass<ActivityPageResponse>> GetUserSessionActivityLogs([FromBody] ActivityLogRequestDto request)
        {
            APIResponseClass<ActivityPageResponse> response = new();

            try
            {
                var result = await _dashboardService.GetPagedActivities(request);

                response.result = result;
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "User session activity logs retrieved successfully";

                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Something went wrong: " + ex.Message;

                return response;
            }
        }


        [HttpPost("VerifyHashChain")]
        public async Task<IActionResult> VerifyHashChain([FromBody] HashCheckRequest req)
        {
            APIResponseClass<object> response = new();

            try
            {
                bool result = await _dashboardService.VerifyHashChain(req.Hash, req.Depth);

                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Hash chain verification completed successfully.";
                response.result = new
                {
                    hash = req.Hash,
                    depth = req.Depth,
                    isValid = result
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "An error occurred during hash chain verification.";
                response.result = null;

                // Optional: Log exception
                // _logger.LogError(ex, "Error verifying hash chain");

                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }


        //[HttpPost("VerifyHashChain")]
        //public async Task<IActionResult> VerifyHashChain([FromBody] HashCheckRequest req)
        //{
        //    bool result = await _dashboardService.VerifyHashChain(req.Hash, req.Depth);

        //    return Ok(new
        //    {
        //        hash = req.Hash,
        //        depth = req.Depth,
        //        isValid = result
        //    });
        //}









    }

}
