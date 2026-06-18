using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL;
using UserManagement.Filters;
using UserManagement.Helper;
using UserManagement.Models.DTO;

namespace UserManagement.Controllers
{
    //[Authorize("Super Admin,User Admin,Level Admin,IFMS USER,Module Admin")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class UserController : Controller
    {
        private readonly IConfiguration _config;
        private readonly UserManagementDBContext _context;
        private readonly ITempHrmService _tempHrmService;
        private readonly IUserService _userService;
        private readonly IApplicationService _applicationService;
        private readonly IJWTService _jwtService;
        private readonly IRoleService _roleService;
        private readonly IClaimService _claimService;
        private readonly IAuthService _authService;
        private readonly ILoginLogService _loginLogService;

        public UserController(UserManagementDBContext context, IConfiguration config, IUserService userService, ITempHrmService tempHrmService, IApplicationService applicationService, IClaimService claimService, IJWTService jwtService, IRoleService roleService, IAuthService authService, ILoginLogService loginLogService)
        {
            _context = context;
            _config = config;
            _userService = userService;
            _tempHrmService = tempHrmService;
            _applicationService = applicationService;
            _jwtService = jwtService;
            _roleService = roleService;
            _claimService = claimService;
            _authService = authService;
            _loginLogService = loginLogService;
        }


        //Check Existing User
        [HttpGet("CheckExistingUser")]
        public async Task<APIResponseClass<bool>> CheckExistingUser(string userName)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _userService.CheckExistingUserByLoginId(userName);
                ; if (res)
                {
                    response.message = "User already exists.";
                }
                else
                {
                    response.message = "User does not exist.";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = res;
                return response;
            }
            catch (Exception e)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Something went wrong" + e.Message;
                return response;
            }
        }


        [HttpGet("GetHrmsDetails/{hrmsId}")]
        public async Task<APIResponseClass<HrmsDeatilsDTO>> HrmsDetails(string hrmsId)
        {
            APIResponseClass<HrmsDeatilsDTO> response = new();
            try
            {
                HrmsDeatilsDTO hrmsDeatils = await _tempHrmService.HrmsDeatilsByHremsId(hrmsId);
                if (hrmsDeatils != null)
                {
                    response.message = "Data Collect Successfully";
                }
                else
                {
                    response.message = "Data Not Found";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = hrmsDeatils;
                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed " + ex.Message;
                return response;
            }
        }

        [HttpPost("GetJWTFromAccessToken")]
        public APIResponseClass<AuthToken> GetJWTFromAccessToken(GUIDExchangeDTO guid)
        {
            APIResponseClass<AuthToken> response = new();
            try
            {
                (string, string) data = _userService.GetJWTFromAccessToken(guid.AccessToken);

                if (data.Item1 != null && data.Item2 != null)
                {
                    response.result = new AuthToken
                    {
                        AccessToken = data.Item1,
                        RefreshToken = data.Item2
                    };
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "Token found.";
                    return response;
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Authentication Failed. Please Try Again";
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Invalid Access Token." + Ex.Message;
                return response;
            }
        }


        [HttpPost("UserRegistration")]
        public async Task<APIResponseClass<bool>> UserRegistration(UserRegistrationNewDTO user, bool isSuperAdminCreation = false)
        {
            APIResponseClass<bool> response = new();
            try
            {
                (bool, string) res;
                string[] userRoles = _claimService.GetRoles();
                if (Array.Exists(userRoles, roleName => roleName == "Super Admin"))
                {
                    res = await _userService.NewUserRegistrationBySuperAdmin(user, isSuperAdminCreation);
                }
                else
                {
                    res = await _userService.NewUserRegistration(user);
                }
                if (res.Item1)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                }
                response.result = res.Item1;
                response.message = res.Item2;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed to create user. Please try again." + Ex.Message;
                return response;
            }
        }


        [HttpPost("GetJWTTokenForMultiple")]
        public async Task<APIResponseClass<AuthTokenForModules>> GetJWTTokenForMultiple(GetJWTPayload getJWTPayload)
        {
            APIResponseClass<AuthTokenForModules> response = new();
            try
            {
                var baseUrl = await _applicationService.GetApplicationUrl(getJWTPayload.Application[0].Id);
                var jwt = await _userService.GetJWTDirect(getJWTPayload);
                var url = baseUrl + jwt.Url;
                if (jwt != null)
                {
                    await _userService.AddApplicationToActivityLog((long)getJWTPayload.UserId, getJWTPayload.Application[0].Id);
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "JWT Created";
                response.result = jwt;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Login failed, please try again..";
                return response;
            }
        }

        [HttpPost("CheckLoginAfterAppChoose")]
        public async Task<APIResponseClass<AuthenticatedUserRoleSelectedResponse>> CheckLoginAfterAppChoose(DataCollectionJWTDTO dataCollectionJWTDTO)
        {
            dataCollectionJWTDTO.UserId = _claimService.GetUserId();

            APIResponseClass<AuthenticatedUserRoleSelectedResponse> response = new();
            try
            {
                var res = new AuthenticatedUserRoleSelectedResponse();

                //if (dataCollectionJWTDTO.AppId == 1 || dataCollectionJWTDTO.AppId == 5)
                //{
                //    var multipleCheck = await _loginLogService.IsMultipleLoggedIn(dataCollectionJWTDTO.UserId, dataCollectionJWTDTO.AppId);
                //    if (multipleCheck.Item1)
                //    {
                //        response.apiResponseStatus = Enum.APIResponseStatus.Error;
                //        response.message = multipleCheck.Item2;
                //        return response;
                //    }

                //}
                // check if multiple permission
                var appStatus = await _applicationService.GetApplicationStatusById(dataCollectionJWTDTO.AppId);
                if (appStatus)
                {
                    var maintenance_msg = await _applicationService.GetApplicationMaintenanceMsgById(dataCollectionJWTDTO.AppId);
                    if (maintenance_msg == null)
                    {
                        maintenance_msg = "Application Under Maintenance.";
                    }
                    res.IsMaintenance = appStatus;
                    response.result = res;
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = maintenance_msg;
                    return response;
                }
                var isSinglePriviledged = await _userService.IsSinglePriviledged(dataCollectionJWTDTO);

                if (isSinglePriviledged)   // single permission
                {
                    var token = new AuthTokenForModules();
                    //var baseUrl = await _applicationService.GetApplicationUrl(dataCollectionJWTDTO.AppId);
                    if (dataCollectionJWTDTO.AppId != 1 && dataCollectionJWTDTO.AppId != 5)
                    {
                        token = await _jwtService.JWTTokenCreationForOther(dataCollectionJWTDTO);
                    }
                    else
                    {
                        token = await _jwtService.JWTTokenCreationForUMAndMM(dataCollectionJWTDTO);
                    }
                    if(token != null)
                    {
                       await _userService.AddApplicationToActivityLog((long)dataCollectionJWTDTO.UserId, dataCollectionJWTDTO.AppId);
                    }
                    res.authTokenForModules = token;
                }
                else
                {   // multiple
                    res.Roles = await _roleService.GetRolesOfAuthenticatedUserByApplicationId(dataCollectionJWTDTO.UserId, dataCollectionJWTDTO.AppId);
                }
                res.IsSingleApplication = isSinglePriviledged;
                response.result = res;
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Data fetched";
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Unable to get application permissions, please try again..";
#if DEBUG
                response.message = "Unable to get application permissions, please try again.. " + Ex.ToString();
#endif
                return response;
            }
        }


        [HttpGet("LockedUserDetailsForDisplay")]
        public async Task<APIResponseClass<UserDetailsForDisplay>> LockedUserDetailsForDisplay()
        {
            APIResponseClass<UserDetailsForDisplay> response = new();
            try
            {

                var lockedUsers = await _userService.LockedUserDetailsForDisplay();

                if (lockedUsers.Count > 0)
                {
                    response.result = lockedUsers;
                    response.message = "Data fetched";
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
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Something went wrong.";
                return response;
            }
        }

        [HttpPost("UnlockedUserByUserIds")]
        public async Task<APIResponseClass<bool>> UnlockedUserByUserIds(List<long> userId)
        {
            APIResponseClass<bool> response = new();
            try
            {

                var isUnlocked = await _userService.UnlockedUserByUserIds(userId);

                if (isUnlocked)
                {
                    response.message = "User unlocked";
                }
                else
                {
                    response.message = "Couldn't unlock the user";
                }
                response.result = isUnlocked;
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Something went wrong.";
                return response;
            }
        }

        
        [HttpPost("UserDetailsForDisplay")]
        public async Task<APIResponseClass<UserDetailsForDisplayWithCountDTO>> UserDetailsForDisplay(FilterData payload)

        {
            APIResponseClass<UserDetailsForDisplayWithCountDTO> response = new();
            try
            {
                var Users = await _userService.UserDetailsForDisplay(payload);

                if (Users != null)
                {
                    response.result = Users;
                    response.message = "Data fetched";
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
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Something went wrong.";
                return response;
            }
        }

        [HttpDelete("DeleteUser")]
        public async Task<APIResponseClass<bool>> DeleteUser(long userId)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _userService.DeleteUser(userId);
                if (res)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "User Deleted";
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Unable to delete the user";
                }
                response.result = res;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed to delete. Please try again." + Ex.Message;
                return response;
            }
        }

        [HttpPost("UpdateUser")]
        public async Task<APIResponseClass<bool>> UpdateUser(UserUpdateDTO user)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _userService.UpdateUser(user);
                if (res)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "User Updated";
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Unable to update the user";
                }
                response.result = res;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed update. Please try again." + Ex.Message;
                return response;
            }
        }

        [HttpPost("ResetPassword")]
        public async Task<APIResponseClass<bool>> ResetPassword(long userId)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _userService.ResetPassword(userId);
                if (res)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "User Password has been reset.";
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Unable to reset the user password";
                }
                response.result = res;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Failed reset password. Please try again." + Ex.Message;
                return response;
            }
        }

        [HttpPost("ChangePassword")]
        public async Task<APIResponseClass<bool>> ChangePassword(ChangePasswordDTO password)
        {
            APIResponseClass<bool> response = new();
            try
            {
                return await _userService.ChangePassword(password);
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Couldn't update password, please try again..";
                return response;
            }
        }
        [HttpPost("ChangeUserBasicDetails")]
        public async Task<APIResponseClass<bool>> ChangeUserBasicDetails(ChangeUserBasicDetailsDTO userDetails)
        {
            APIResponseClass<bool> response = new();
            try
            {
                return await _userService.ChangeUserBasicDetails(userDetails);
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Couldn't update, please try again..";
                return response;
            }
        }

        [HttpPost("ValidateToken")]
        public async Task<APIResponseClass<bool>> ValidateToken(string token)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var isAuthenticated = await _authService.ValidateToken(token);
                if (isAuthenticated)
                {
                    response.message = "Authenticated";
                }
                else
                {
                    response.message = "UnAuthenticated";
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = isAuthenticated;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = Ex.Message;
                return response;
            }
        }

        [HttpGet("Logout")]
        public async Task<APIResponseClass<bool>> Logout(int appId, string device, string agent)
        {
            Console.WriteLine("User Logout from app: " + appId);
            var publicIP = HttpContext.Request.Headers["Src"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            var privateIP = "";
            return await _userService.Logout(new DataCollectionJWTDTO
            {
                AppId = appId,
                UserId = _claimService.GetUserId()
            }, publicIP, privateIP, device, agent);
        }

        [HttpGet("GetUserPrivilegeByUserId")]
        public async Task<APIResponseClass<List<UserAccessDTO>>> GetUserPrivilegeByUserId(long userId)
        {
            APIResponseClass<List<UserAccessDTO>> response = new();
            try
            {
                var Users = await _userService.GetUserPrivilegeByUserId(userId);

                if (Users.Count > 0)
                {
                    response.result = Users;
                    response.message = "Data fetched";
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
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Something went wrong." + Ex.Message;
                return response;
            }
        }
        [HttpPost("ModifyUserPrivilege")]
        public async Task<APIResponseClass<bool>> ModifyUserPrivilege(UserPrivilegeUpdateDTO userPrivilegeUpdate)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var res = await _userService.UpdateUserPrivilege(userPrivilegeUpdate);
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
                response.message = "Failed to update privilege. Please try again." + Ex.Message;
                return response;
            }
        }

        [HttpGet("UserProfileData/{userName}")]
        public async Task<APIResponseClass<UserProfileDTO>> UserProfileData(string userName)
        {
            var response = new APIResponseClass<UserProfileDTO>();

            try
            {
                var result = await _userService.GetUserProfileData(userName);

                if (result != null)
                {
                    response.result = result;
                    response.message = "Data fetched successfully";
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                }
                else
                {
                    response.message = "No Data Found";
                }
            }
            catch (Exception ex)
            {
                response.message = "Something went wrong";
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
            }

            return response;
        }

        [HttpGet("SendOTPForEmailChange")]
        public Task<APIResponseClass<OTPResponseDTO>> SendOTPForEmailChange(string username)
        {
            return _userService.SendOTPForEmailChange(username);
        }

        [HttpPost("UpdateEmailWithOtpValidation")]
        public async Task<APIResponseClass<bool>> UpdateEmailWithOtpValidation(UpdateEmailDTO updateEmailDTO)
        {
            var response = new APIResponseClass<bool>();

            try
            {
                var result = await _userService.UpadteUserEmail(updateEmailDTO);

                if (result)
                {
                    response.result = true;
                    response.message = "Email Update Succesfully";
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                }
                else
                {
                    response.result = false;
                    response.message = "Invalid OTP";
                }
            }
            catch (Exception ex)
            {
                response.message = "Something went wrong";
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
            }
            return response;

        }

        [HttpPost("UpdateSignerId")]
        public async Task<APIResponseClass<bool>> UpdateSignerId(string userName, string signerId)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var isUpdated = await _userService.UpdateSignerIdAsync(userName, signerId);

                response.result = isUpdated;
                response.message = isUpdated ? "Signer ID updated successfully." : "Failed to update Signer ID.";
                response.apiResponseStatus = Enum.APIResponseStatus.Success;

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
