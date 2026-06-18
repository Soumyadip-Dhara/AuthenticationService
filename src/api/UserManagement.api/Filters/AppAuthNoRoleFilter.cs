//using UserManagement.DAL.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using System.Security.Claims;
using UserManagement.Model.Claims;
using UserManagement.Models.SSO;
using UserManagement.Models.Claims;
using UserManagement.Enum;
using UserManagement.DAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL.Repositories;
using UserManagement.DAL.Entities;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Services;


namespace UserManagement.Filters
{

    public class AppAuthNoRoleFilterAttribute : IAuthorizationFilter
    {
        private readonly IUserActivityLogService _userActivityLogService;
        private readonly IUserActivityLogRepository _userActivityLogRepository;
        private readonly IClaimService _claimService;
        public AppAuthNoRoleFilterAttribute(IUserActivityLogService UserActivityLogService, IUserActivityLogRepository UserActivityLogRepository, IClaimService claimService)
        {
            _userActivityLogService = UserActivityLogService;
            _userActivityLogRepository = UserActivityLogRepository;
            _claimService = claimService;


        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {

            if (context.HttpContext.Items["userclaimmodel"] != null)
            {
                AuthClaimModel userclaimmodel = (AuthClaimModel)context.HttpContext.Items["userclaimmodel"];
                List<Claim> userclaim = userclaimmodel.claims;
                bool acs = true;
                var application = userclaim.Where(c => c.Type == "application").Select(application => application).ToArray();

                var expClaim = userclaim.FirstOrDefault(c => c.Type == "exp");
                if (expClaim == null || !long.TryParse(expClaim.Value, out long expSeconds))
                {
                    context.Result = new JsonResult(new { message = "Invalid JWT expiration claim", apiResponseStatus = 3, result = false })
                    { StatusCode = StatusCodes.Status401Unauthorized };

                    var userActivityLog = new UserActivityLog
                    {
                        UserId = _claimService.GetUserId(),
                        IsLogin = false,
                        PublicIp =  "",
                        PrivateIp =  "",
                        //Device = device,
                        //Agent = agent,
                        IsSystemLogout = true
                        
                    };
                    _userActivityLogRepository.Add(userActivityLog);
                    _userActivityLogRepository.SaveChangesManaged();
                    //UserActivityLog? userActivityLog = new()
                    //{
                    //    //ApplicationId = 0,
                    //    UserId = user.Id,
                    //    //LoginTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"))
                    //};
                    //_userActivityLogRepository.Add(userActivityLog);
                    //_userActivityLogRepository.SaveChangesManaged();

                    return;
                }

                // Convert expiration from seconds since Unix epoch to DateTime
                var expDate = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
                if (expDate <= DateTime.UtcNow)
                {
                    context.Result = new JsonResult(new { message = "JWT Expired", apiResponseStatus = 3, result = false })
                    { StatusCode = StatusCodes.Status401Unauthorized };
                    var userActivityLog = new UserActivityLog
                    {
                        UserId = _claimService.GetUserId(),
                        IsLogin = false,
                        PublicIp = "",
                        PrivateIp = "",
                        //Device = device,
                        //Agent = agent,
                        IsSystemLogout = true
                    };
                    _userActivityLogRepository.Add(userActivityLog);
                    _userActivityLogRepository.SaveChangesManaged();
                    return;
                }
                //var app = JsonSerializer.Deserialize<ClaimModel.Application>(application[0].Value);
                //acs &= _roles.Contains(app.Role.Name);
                //if (!acs)
                //{
                //    context.Result = new JsonResult(new { message = "Unauthorized Access", apiResponseStatus = 3, result = false }) { StatusCode = StatusCodes.Status200OK };
                //    return;
                //}
            }
            else
            {
                context.Result = new JsonResult(
                    new { 
                        message = "UnAuthenticated", 
                        apiResponseStatus = 3, 
                        result = false 
                    }
                ) 
                { 
                    StatusCode = StatusCodes.Status401Unauthorized 
                };
                var userActivityLog = new UserActivityLog
                {
                    UserId = _claimService.GetUserId(),
                    IsLogin = false,
                    PublicIp = "",
                    PrivateIp = "",
                    //Device = device,
                    //Agent = agent,
                    IsSystemLogout = true
                };
                _userActivityLogRepository.Add(userActivityLog);
                _userActivityLogRepository.SaveChangesManaged();
                return;
            }

        }
    }

    public class NoRoleAuthorizeAttribute : TypeFilterAttribute
    {
        public NoRoleAuthorizeAttribute() : base(typeof(AppAuthNoRoleFilterAttribute))
        {
            
        }
    }

}
