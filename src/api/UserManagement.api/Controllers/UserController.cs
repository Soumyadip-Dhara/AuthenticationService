using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL;

using UserManagement.Helper;
using UserManagement.Models.DTO;

namespace UserManagement.Controllers
{
    [Authorize("Super Admin,User Admin,Level Admin,IFMS USER,Module Admin")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class UserController : Controller
    {
        private readonly IConfiguration _config;
        private readonly UserManagementDBContext _context;
        private readonly ITempHrmService _tempHrmService;
        private readonly IUserService _userService;
        private readonly IApplicationService _applicationService;
        private readonly IRoleService _roleService;
        private readonly IClaimService _claimService;
        public UserController(UserManagementDBContext context, IConfiguration config, IUserService userService, ITempHrmService tempHrmService, IApplicationService applicationService, IClaimService claimService, IRoleService roleService)
        {
            _context = context;
            _config = config;
            _userService = userService;
            _tempHrmService = tempHrmService;
            _applicationService = applicationService;
            _roleService = roleService;
            _claimService = claimService;
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
