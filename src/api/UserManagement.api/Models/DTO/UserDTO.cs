using Microsoft.AspNetCore.Authentication.BearerToken;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace UserManagement.Models.DTO
{
    public class UserAviblityCheckDTO
    {
        public int ApplicationId { get; set; }
        public int RoleId { get; set; }
        public int? LevelId { get; set; }
        public string ScopeValue { get; set; } = null!;
    }
    public class ExistsUsersListDTO
    {
        public string Name { get; set; } = null!;
        public string Application { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string[] Permissions { get; set; } = null!;
        public string? Level { get; set; }
        public string[] ScopeValues { get; set; } = null!;
    }
    public class HrmsDeatilsDTO
    {
        public string? UniqueId { get; set; }
        public string? Name { get; set; }
        public string? Designation { get; set; }
        public string? MobileNumber { get; set; }
        public string? Email { get; set; }
    }
    public class UserDetails
    {
        public int Id { get; set; }

        [StringLength(100)]
        [RegularExpression(
            @"[.a-zA-Z\s]*",
            ErrorMessage = "{0} must be alphabetic, can contain spaces, can contain dots(.)"
        )]
        public string Name { get; set; } = null!;
    }

    public class UserBasicDetailsSetDTO
    {
        public string LoginId { get; set; }
        public int? HrmsId { get; set; }
        public string Name { get; set; }
        public string? Password { get; set; }
        public byte[]? PasswordHash { get; set; }
        public byte[]? PasswordSalt { get; set; }
        public short? Status { get; set; }
        public string Designation { get; set; }
        public string MobileNumber { get; set; }
        public string? Email { get; set; }
        public long? CreatedBy { get; set; }
    }

    public class UserCreateDTO
    {
        public UserBasicDetailsSetDTO UserBasicDetails { get; set; }
        public List<UserPrivilegeSetDTO> UserPrivileges { get; set; }
    }

    public class UserDetailsForVerificationDTO
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public short UnsuccessfulLoginAttempt { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserDetailsAfterAuthenticationDTO
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }

    }

    public class UserMasterInsertDTO
    {
        [Required]
        [RegularExpression(
            @"[a-zA-Z0-9][a-zA-Z0-9.,&\-_()\s]*",
            ErrorMessage = "{0} must be aplanumeric and can contain dots(.) and underscores(_)"
        )]
        public string UserName { get; set; } = null!;

        [RegularExpression(
            @"[a-zA-Z0-9]*",
            ErrorMessage = "{0} must be alphanumeric"
        )]
        public string HrmsId { get; set; } = null!;

        [StringLength(100)]
        [RegularExpression(
            @"[.a-zA-Z\s]*",
            ErrorMessage = "{0} must be alphabetic, can contain spaces, can contain dots(.)"
        )]
        public string Name { get; set; } = null!;

        [RegularExpression(
            @"[a-zA-Z0-9\s-.()]*",
            ErrorMessage = "{0} must be alphanumeric, can contain hyphens(-), can contain dots(.), can contain first brackets(()) and spaces"
        )]
        public string Designation { get; set; } = null!;

        [StringLength(10)]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Mobile Number")]
        public string MobileNumber { get; set; } = null!;

        [EmailAddress]
        public string Email { get; set; } = null!;
    }

    public class PermissionAndScope
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class UseAccessInsertDTO
    {
        public short AppId { get; set; }
        public string AppName { get; set; } = null!;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public List<int> Permissions { get; set; } = null!;
        public List<PermissionAndScope> Permission { get; set; } = null!;
        public long LevelId { get; set; }
        public string LevelName { get; set; } = null!;
        public List<int> Scopes { get; set; } = null!;
        public List<PermissionAndScope> Scope { get; set; } = null!;
        public bool UserManagementEnabled { get; set; }

    }

    public class UserRegistrationDTO
    {
        public UserMasterInsertDTO UserMaster { get; set; } = null!;
        public List<UseAccessInsertDTO> UserAccess { get; set; } = null!;

    }
    public class ApplicationForUMAndMMDTO
    {
        public short Id { get; set; }
        public string Title { get; set; } = null!;
    }

    public class UserRegistrationNewDTO
    {
        public UserMasterInsertDTO UserMaster { get; set; } = null!;
        public List<UseAccessInsertDTO>? UserAccess { get; set; }
        public List<ApplicationForUMAndMMDTO>? UMApps { get; set; }
        public List<ApplicationForUMAndMMDTO>? MMApps { get; set; }

    }
    public class UserPrivilegeDTO
    {
        public int AppId { get; set; }
        public string AppName { get; set; } = null!;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public List<PermissionAndScope>? Permissions { get; set; }
        public long LevelId { get; set; }
        public string LevelName { get; set; } = null!;
        public List<PermissionAndScope?> Scopes { get; set; } = null!;
        public bool IsAdmin { get; set; }
    }
    public class ScopeValue:PermissionAndScope
    {
        public string Value { get; set; }
        public string ParentScope { get; set; }

    }

    public class RegisterUserPrivilegeDTO
    {
        public int AppId { get; set; }
        public string AppName { get; set; } = null!;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public List<PermissionAndScope>? Permissions { get; set; }
        public long LevelId { get; set; }
        public string LevelName { get; set; } = null!;
        public List<ScopeValue> Scopes { get; set; } = null!;
        public bool? IsAdmin { get; set; }
    }


    public class UserDetailsWithPrivilegesDTO : UserMasterInsertDTO
    {
        public long Id { get; set; }
        public string? EffectiveFrom { get; set; }
        public string? ExpiresOn { get; set; }
        public bool? IsActive { get; set; }
        public List<RegisterUserPrivilegeDTO>? Privileges { get; set; }
    }

    public class UserLoginDTO
    {
        [Required]
        [RegularExpression(
            @"[a-zA-Z0-9][a-zA-Z0-9.,&\-_()]*",
            ErrorMessage = "{0} must be aplanumeric and can contain dots(.) and underscores(_)"
        )]
        public string Username { get; set; } = null!;

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Invalid OTP")]
        public string Otp { get; set; } = null!;

        public List<string>? DeviceInfo { get; set; }
    }
    public class UserCredentialDTO
    {
        [Required]
        [RegularExpression(
            @"[a-zA-Z0-9][a-zA-Z0-9.,&\-_()]*",
            ErrorMessage = "{0} must be aplanumeric and can contain dots(.) and underscores(_)"
        )]
        public string Username { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        [Required]
        [RegularExpression(
            @"[a-zA-Z0-9]*",
            ErrorMessage = "{0} must be aplanumeric"
        )]
        public string CaptchaCode { get; set; } = null!;

        [Required]
        public int CaptchaId { get; set; }

        public List<string>? DeviceInfo { get; set; }

        [Required]
        public string OtpType { get; set; } = "NORMAL_OTP";  // NORMAL_OTP or TOTP
    }

    public class TOTPVerificationDTO
    {
        [Required]
        [RegularExpression(
            @"[a-zA-Z0-9][a-zA-Z0-9.,&\-_()]*",
            ErrorMessage = "{0} must be aplanumeric and can contain dots(.) and underscores(_)"
        )]
        public string Username { get; set; } = null!;

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "TOTP code must be exactly 6 digits")]
        public string TotpCode { get; set; } = null!;

        public List<string>? DeviceInfo { get; set; }
    }

    public class VerifyTOTPSetupRequest
    {
        /// <summary>
        /// User's username or email
        /// </summary>
        [Required]
        [RegularExpression(
            @"[a-zA-Z0-9][a-zA-Z0-9.,&\-_()]*",
            ErrorMessage = "{0} must be alphanumeric and can contain dots(.) and underscores(_)"
        )]
        public string Username { get; set; } = null!;

        /// <summary>
        /// Base32-encoded TOTP secret from GetTOTPQR
        /// </summary>
        [Required]
        public string Secret { get; set; } = null!;

        /// <summary>
        /// 6-digit test code from authenticator app (used to verify setup)
        /// </summary>
        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Test code must be exactly 6 digits")]
        public string TestCode { get; set; } = null!;
    }

    public class GetTOTPQRResponse
    {
        /// <summary>
        /// Base64-encoded PNG image of QR code
        /// </summary>
        public string QrCodeImage { get; set; } = null!;

        /// <summary>
        /// Base32-encoded secret key for manual setup
        /// </summary>
        public string Secret { get; set; } = null!;

        /// <summary>
        /// otpauth:// URI (used to generate QR code)
        /// </summary>
        public string SetupUrl { get; set; } = null!;
    }

    public class ApplicationClaim
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public List<LevelClaim> Levels { get; set; } = null!;
        public List<RoleClaim> Roles { get; set; } = null!;
    }
    public class RoleClaim
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public List<string> Permissions { get; set; } = null!;
    }
    public class LevelClaim
    {
        public int? Id { get; set; }
        public string Name { get; set; } = null!;
        public List<string> Scope { get; set; } = null!;
    }
    public class CustomJwtClaims
    {
        public List<ApplicationClaim> Application { get; set; } = null!;
        public int NameId { get; set; }
        public string Name { get; set; } = null!;
        public long Nbf { get; set; }
        public long Exp { get; set; }
        public long Iat { get; set; }
    }

    public class UserPrivilegeSetDTO
    {
        public int ApplicationId { get; set; }
        public int RoleId { get; set; }
        public List<int> PermissionIds { get; set; } = null!;
        public int? LevelId { get; set; }
        public List<string?> ScopeValue { get; set; } = null!;
    }

    public class AuthenticatedUserRoleSelectedResponse
    {
        public bool IsSingleApplication { get; set; }
        public List<RoleFetchDTO> Roles { get; set; } = null!;
        public AuthTokenForModules authTokenForModules { get; set; } = null!;
        public bool IsMaintenance { get; set; } = false;
    }

    public class UserDetailsForDisplayDTO
    {
        public long Id { get; set; }
        public string UserName { get; set; } = null!;
        public string HrmsId { get; set; } = "";
        public string Name { get; set; } = null!;
        public string Designation { get; set; } = null!;
        public string MobileNumber { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool? IsActive { get; set; }
        public bool? IsBlocked { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? CreatedBy { get; set; }

    }
    public class UserDetailsForDisplayDTOForDeserialize
    {
        public long id { get; set; }
        public string userName { get; set; } = null!;
        public string hrmsId { get; set; } = "";
        public string name { get; set; } = null!;
        public string designation { get; set; } = null!;
        public string mobileNumber { get; set; } = null!;
        public string email { get; set; } = null!;
        public bool? isActive { get; set; }
        public bool? isBlocked { get; set; }
        public DateTime? createdAt { get; set; }
        public string? createdBy { get; set; }
    }

    public class UserDetailsForDisplayWithCountDTO
    {
        public List<UserDetailsForDisplayDTOForDeserialize> Users { get; set; } = [];
        public int Count { get; set; }
        public int ActiveUsers { get; set; }
        public int InActiveUsers { get; set; }
        public int NewlyRegisteredUsers { get; set; }
    }
    public class UserDetailsForDisplay
    {
        public List<UserDetailsForDisplayDTO> Users { get; set; } = [];
        public int Count { get; set; }
        public int InActiveUsers { get; set; }


    }

    public class UserProfileDTO
    {
        public List<UserProfileDetailsDTO> Users { get; set; } = [];
    }

    public class UserProfileDetailsDTO
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
        public string MobileNumber { get; set; }
        public string Email { get; set; }
        public List<string> Level { get; set; }
        public string? SignerID { get; set; }
    }


    public class UserUpdateDTO
    {
        [Required]
        [RegularExpression(
            @"[a-zA-Z0-9][a-zA-Z0-9.,&\-_()]*",
            ErrorMessage = "{0} must be aplanumeric and can contain dots(.) and underscores(_)"
        )]
        public string UserName { get; set; } = null!;

        [RegularExpression(
            @"[a-zA-Z0-9]*",
            ErrorMessage = "{0} must be alphanumeric"
        )]
        public string HrmsId { get; set; } = null!;

        [StringLength(100)]
        [RegularExpression(
            @"[.a-zA-Z\s]*",
            ErrorMessage = "{0} must be alphabetic, can contain spaces, can contain dots(.)"
        )]
        public string Name { get; set; } = null!;

        [RegularExpression(
            @"[a-zA-Z0-9\s-.()]*",
            ErrorMessage = "{0} must be alphanumeric, can contain hyphens(-), can contain dots(.), can contain first brackets(()) and spaces"
        )]
        public string Designation { get; set; } = null!;

        [StringLength(10)]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Mobile Number")]
        public string MobileNumber { get; set; } = null!;

        [EmailAddress]
        public string Email { get; set; } = null!;
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public bool IsBlocked { get; set; }
    }
    public class ChangePasswordDTO
    {
        [Required]
        public string Current { get; set; } = null!;
        [Required]
        public string New { get; set; } = null!;
    }
    public class ChangeUserBasicDetailsDTO
    {
        [Required]
        public string Password { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(10)]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Mobile Number")]
        public string MobileNumber { get; set; } = null!;
    }
    public class TwoFAConfigDTO
    {
        public bool IsActive { get; set; }
        public List<string> Methods { get; set; } = new() { "NORMAL_OTP", "TOTP" };
    }

    public class UserLoginTokenDTO
    {
        public string AccessToken { get; set; } = null!;
        public string? RefreshToken { get; set; }
        public bool IsFirstLogin { get; set; }
        public long? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? MobileNumber { get; set; }
        public bool UserLoggedInMultipleTimes { get; set; }
        public List<object>? UserRoles { get; set; }
        public TwoFAConfigDTO? TwoFAConfig { get; set; }
        public string? Otp { get; set; }  // Optional: Only for development/testing
    }
    public class ChangeForgottenPasswordDTO
    {
        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Invalid OTP")]
        public string Otp { get; set; } = null!;

        [Required]
        [RegularExpression(
            @"[a-zA-Z0-9][a-zA-Z0-9.,&\-_()]*",
            ErrorMessage = "{0} must be aplanumeric and can contain dots(.) and underscores(_)"
        )]
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
    public class VerifiedUserDTO
    {
        public string? MobileNumber { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public bool? IsFirstLogin { get; set; }
        public bool? Enable2FA { get; set; }
        public bool UserLoggedInMultipleTimes { get; set; }
        public long? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public List<object>? UserRoles { get; set; }
        public TwoFAConfigDTO? TwoFAConfig { get; set; }
        public string? Otp { get; set; }  // Optional: Only for development/testing
    }

    public class UserAccessDTO
    {
        public long? Id { get; set; }
        public UserPrivilegesDTO? data { get; set; }
        public List<ChildUserPrivilegesDTO>? children { get; set; }
    }
    public class UserPrivilegesDTO
    {
        public ApplicationsDto application { get; set; } = null!;
        public RolesDto role { get; set; } = null!;
        public LevelsDto level { get; set; } = null!;
        public List<PermissionsDto> permissions { get; set; } = null!;
        public List<ScopesDto> scopes { get; set; } = null!;
        public bool? UserManagementEnabled { get; set; }
    }
    public class ChildUserPrivilegesDTO
    {
        public UserPrivilegesDTO data { get; set; } = null!;
    }

    public class ApplicationsDto
    {
        public int id { get; set; }
        public string title { get; set; } = null!;
    }

    public class RolesDto
    {
        public int id { get; set; }
        public string title { get; set; } = null!;
    }

    public class LevelsDto
    {
        public int id { get; set; }
        public string title { get; set; } = null!;
    }

    public class PermissionsDto
    {
        public int id { get; set; }
        public string name { get; set; } = null!;
    }

    public class ScopesDto
    {
        public string? name { get; set; }
        public string? value { get; set; }
        public long id { get; set; }
    }

    public class GetApplicationDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
    }

    public class UserPrivilegeUpdateDTO
    {
        public long userId { get; set; }
        public List<GetApplicationDTO> UMApps { get; set; } = null!;
        public List<GetApplicationDTO> MMApps { get; set; } = null!;
        public List<UserPrivilegesDTO> UserPrivileges { get; set; } = null!;
    }
    public class OTPResponseDTO
    {
        public bool IsSent { get; set; }
        public double TimeRemaining { get; set; }
    }
    public class GUIDExchangeDTO
    {
        public string AccessToken { get; set; } = null!;
    }
    public class SearchDataForAdminManagementDTO
    {
        public string Scope { get; set; }
        public int LevelId { get; set; }
    }
    public class AdminManagementDTO
    {
        public List<long> UserIds { get; set; }
        public long ScopeId { get; set; }
        public int AppId { get; set; }
        public bool IsSingleAdmin { get; set; }
    }




    public class DeviceLoginCount
    {
        public string Device { get; set; }
        public int TotalLogins { get; set; }
    }
    public class AgentLoginCount
    {
        public string Agent { get; set; }
        public int TotalLogins { get; set; }
    }

    public class UpdateEmailDTO
    {
        public string OTP { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
    }



public class UserCountsDto
    {
        [JsonPropertyName("totalUsers")]
        public long totalUsers { get; set; }

        [JsonPropertyName("ddoUserCount")]
        public long ddoUserCount { get; set; }

        [JsonPropertyName("wbjitUserCount")]
        public long wbjitUserCount { get; set; }

        [JsonPropertyName("totalScopeCount")]
        public long totalScopeCount { get; set; }

        [JsonPropertyName("transactionTime")]
        public DateTime transactionTime { get; set; }

        [JsonPropertyName("totalActiveUsers")]
        public long totalActiveUsers { get; set; }

        [JsonPropertyName("roleWiseUserCounts")]
        public Dictionary<string, long> roleWiseUserCounts { get; set; }

        [JsonPropertyName("totalInactiveUsers")]
        public long totalInactiveUsers { get; set; }
    }

    public class FetchUserPrivilegeResponse
    {
        public UserPrivilegePaginatedResult? result { get; set; }
        public int apiResponseStatus { get; set; }
        public string message { get; set; } = string.Empty;
        public string? validationResults { get; set; }
    }

    public class UserPrivilegePaginatedResult
    {
        public int? totalCount { get; set; }
        public int? pageNumber { get; set; }
        public int? pageSize { get; set; }
        public List<FlatUserAccessDTO>? data { get; set; }
    }

    public class FlatUserAccessDTO
    {
        public long? id { get; set; }
        public ApplicationsDto? application { get; set; }
        public RolesDto? role { get; set; }
        public LevelsDto? level { get; set; }
        public List<PermissionsDto>? permissions { get; set; }
        public List<ScopesDto>? scopes { get; set; }
        public bool? userManagementEnabled { get; set; }
        public List<ChildUserPrivilegesDTO>? children { get; set; }
    }
}
