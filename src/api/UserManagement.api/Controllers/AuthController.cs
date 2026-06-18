using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using backend.Helpers;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.Filters;
using UserManagement.Helper;
using UserManagement.Models.DTO;
using UserManagement.Throttling;
using static UserManagement.Models.Claims.ClaimModel;

namespace UserManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(

        IUserService userService, 
        IClaimService claimService, 
        IConfiguration configuration,
        IJWTService jwtService
    ) : Controller
    {

        private readonly IUserService _userService = userService;
        private readonly IClaimService _claimService = claimService;
        private readonly IConfiguration _configuration = configuration;
        private readonly IJWTService _jwtService = jwtService;
        private static readonly char[] chars = "abdefghmnrtABDEFGHLMNQRT2345678".ToCharArray();

        [HttpGet("get-version")]
        public APIResponseClass<string> GetVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return new() {
                apiResponseStatus = Enum.APIResponseStatus.Success,
                message = "Version: v" + fileVersionInfo.ProductVersion,
                result = fileVersionInfo.ProductVersion
            };
        }



        [HttpPost("UserLogin")]
        [AllowAnonymous]
        public async Task<APIResponseClass<UserLoginTokenDTO>> Userlogin(UserLoginDTO userLoginDTO)
        {
            APIResponseClass<UserLoginTokenDTO> response = new();
            try
            {
                List<string>? deviceInfo = userLoginDTO.DeviceInfo;
                string? publicIP = "";
                if (HttpContext.Request.Headers.ContainsKey("Src") && !string.IsNullOrEmpty(HttpContext.Request.Headers["Src"].ToString()))
                {
                    publicIP = HttpContext.Request.Headers["Src"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
                }
                var privateIP = "";
                var device = userLoginDTO.DeviceInfo[0];
                var agent = userLoginDTO.DeviceInfo[1];
                var res = await _userService.UserLogin(userLoginDTO, publicIP, privateIP, device, agent);
                if (res != null && res.result != null)
                {
                    return res;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = res.message;
                    response.result = null;
                    return response;
                }
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Login failed, please try again..";
                return response;
            }
        }

        [HttpPost("VerifyTOTP")]
        [AllowAnonymous]
        public async Task<APIResponseClass<UserLoginTokenDTO>> VerifyTOTP(TOTPVerificationDTO totpVerificationDTO)
        {
            APIResponseClass<UserLoginTokenDTO> response = new();
            try
            {
                string? publicIP = "";
                if (HttpContext.Request.Headers.ContainsKey("Src") && !string.IsNullOrEmpty(HttpContext.Request.Headers["Src"].ToString()))
                {
                    publicIP = HttpContext.Request.Headers["Src"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
                }
                var privateIP = "";
                //var device = totpVerificationDTO.DeviceInfo?[0] ?? "";
                //var agent = totpVerificationDTO.DeviceInfo?.Length > 1 ? totpVerificationDTO.DeviceInfo[1] : "";

                var device = totpVerificationDTO.DeviceInfo?.Count > 0
                    ? totpVerificationDTO.DeviceInfo[0]
                    : "";

                var agent = totpVerificationDTO.DeviceInfo?.Count > 1
                    ? totpVerificationDTO.DeviceInfo[1]
                    : "";

                var res = await _userService.VerifyTOTP(totpVerificationDTO, publicIP, privateIP, device, agent);
                
                if (res != null && res.result != null)
                {
                    return res;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = res?.message ?? "TOTP verification failed, please try again..";
                    response.result = null;
                    return response;
                }
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "TOTP verification failed, please try again..";
#if DEBUG
                response.message = $"TOTP verification failed: {Ex.Message}";
#endif
                return response;
            }
        }

        /// <summary>
        /// Generate TOTP QR code for user setup
        /// </summary>
        [HttpGet("GetTOTPQR")]
        [AllowAnonymous]
        public async Task<APIResponseClass<GetTOTPQRResponse>> GetTOTPQR([FromQuery] string username)
        {
            APIResponseClass<GetTOTPQRResponse> response = new();
            try
            {
                var res = await _userService.GetTOTPQRCode(username);
                return res;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Error generating TOTP QR code";
#if DEBUG
                response.message = $"Error: {Ex.Message}";
#endif
                response.result = null;
                return response;
            }
        }

        /// <summary>
        /// Verify TOTP setup with test code
        /// </summary>
        [HttpPost("VerifyTOTPSetup")]
        [AllowAnonymous]
        public async Task<APIResponseClass<object>> VerifyTOTPSetup([FromBody] VerifyTOTPSetupRequest request)
        {
            APIResponseClass<object> response = new();
            try
            {
                var res = await _userService.VerifyTOTPSetup(request);
                return res;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Error verifying TOTP setup";
#if DEBUG
                response.message = $"Error: {Ex.Message}";
#endif
                response.result = null;
                return response;
            }
        }

        /// <summary>
        /// DEBUG: Get current TOTP code for a secret (for testing/debugging only)
        /// </summary>
        [HttpGet("DebugGetTOTPCode")]
        [AllowAnonymous]
        public APIResponseClass<object> DebugGetTOTPCode([FromQuery] string secret)
        {
            APIResponseClass<object> response = new();
            try
            {
                if (string.IsNullOrWhiteSpace(secret))
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Secret is required";
                    response.result = null;
                    return response;
                }

                string currentCode = TOTPHelper.GetCurrentCode(secret);
                var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var secondsInWindow = unixTimestamp % 30;
                var secondsUntilNextCode = 30 - secondsInWindow;
                
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Current TOTP code retrieved";
                response.result = new 
                { 
                    code = currentCode,
                    secret = secret,
                    unixTimestamp = unixTimestamp,
                    secondsInCurrentWindow = secondsInWindow,
                    secondsUntilNextCode = secondsUntilNextCode,
                    message = $"Current code is: {currentCode}. Code valid for {secondsUntilNextCode} more seconds. Unix timestamp: {unixTimestamp}. Compare code with your authenticator app."
                };
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Error getting TOTP code";
#if DEBUG
                response.message = $"Error: {Ex.Message}";
#endif
                response.result = null;
                return response;
            }
        }

        //[Authorize]
        [NoRoleAuthorize()]
        [HttpGet("RefreshToken")]
        public async Task<APIResponseClass<AuthToken>> RefreshToken()
        {
            var response = new APIResponseClass<AuthToken>();
            try
            {
                var data = _claimService.GetClaimsForJWTForOtherApplication();
                // var appKey = await _userService.GetKeyofApplicationOfUserByRoleId(data.RoleId);
                // var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appKey));
                // var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);


                // var timeAccess = DateTime.UtcNow.AddMinutes(Double.Parse(_configuration["TimeInMinutes:OtherApp"].ToString())).ToString();
                // var timeRefresh = DateTime.UtcNow.AddMinutes(Double.Parse(_configuration["TimeInMinutes:OtherAppRefresh"].ToString())).ToString();

                // var accessClaims = new[]
                // {
                //     //new Claim("application", JsonConvert.SerializeObject(jwtDataPayload)),
                //     new Claim("role", data.Role),
                //     new Claim("roleId", data.RoleId.ToString()),
                //     new Claim("permissions", JsonConvert.SerializeObject(data.Permissions)),
                //     new Claim("level", data.Level),
                //     new Claim("levelId", data.LevelId.ToString()),
                //     new Claim("scope", data.Scope),
                //     new Claim("scopeId", data.ScopeId.ToString()),
                //     new Claim("nameid", data.NameId.ToString()),
                //     new Claim("email", !string.IsNullOrEmpty(data.Email) ? data.Email.ToString() : ""),
                //     new Claim("name" , data.Name),
                //     new Claim("created_by" , data.CreatedBy.ToString()),
                //     new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                //     // new Claim(JwtRegisteredClaimNames.Nbf, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                //     // new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddMinutes(Double.Parse(_configuration["TimeInMinutes:OtherApp"].ToString())).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                // };
                // var accessToken = new JwtSecurityToken(
                //     _configuration["Jwt:Issuer"],
                //     _configuration["Jwt:Audience"],
                //     accessClaims,
                //     notBefore: DateTime.UtcNow,
                //     expires: DateTime.UtcNow.AddMinutes(Double.Parse(_configuration["TimeInMinutes:OtherApp"].ToString())),
                //     signingCredentials: signIn);

                // var jwtAccessToken = new JwtSecurityTokenHandler().WriteToken(accessToken);
                // var refreshclaims = new[]
                // {
                //     //new Claim("application", JsonConvert.SerializeObject(data)),
                //     new Claim("role", data.Role),
                //     new Claim("roleId", data.RoleId.ToString()),
                //     new Claim("permissions", JsonConvert.SerializeObject(data.Permissions)),
                //     new Claim("level", data.Level),
                //     new Claim("levelId", data.LevelId.ToString()),
                //     new Claim("scope", data.Scope),
                //     new Claim("scopeId", data.ScopeId.ToString()),
                //     new Claim("nameid", data.NameId.ToString()),
                //     new Claim("email", !string.IsNullOrEmpty(data.Email) ? data.Email.ToString() : ""),
                //     new Claim("name" , data.Name),
                //     new Claim("created_by" , data.CreatedBy.ToString()),
                //     new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                //     // new Claim(JwtRegisteredClaimNames.Nbf, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                //     // new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddMinutes(Double.Parse(_configuration["TimeInMinutes:OtherAppRefresh"].ToString())).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                // };
                // signIn = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Auth:SecretKey"])), SecurityAlgorithms.HmacSha512);
                // var refreshtoken = new JwtSecurityToken(
                //     _configuration["Jwt:Issuer"],
                //     _configuration["Jwt:Audience"],
                //     refreshclaims,
                //     notBefore: DateTime.UtcNow,
                //     expires: DateTime.UtcNow.AddMinutes(Double.Parse(_configuration["TimeInMinutes:OtherAppRefresh"].ToString())),
                //     signingCredentials: signIn);

                // var jwtRefreshToken = new JwtSecurityTokenHandler().WriteToken(refreshtoken);

                AuthTokenForModules jwtToken = await _jwtService.IssueJwtToken(
                    0,
                    [
                        new Claim("role", data.Role),
                        new Claim("roleId", data.RoleId.ToString()),
                        new Claim("permissions", JsonConvert.SerializeObject(data.Permissions)),
                        new Claim("level", data.Level),
                        new Claim("levelId", data.LevelId.ToString()),
                        new Claim("scope", data.Scope),
                        new Claim("scopeId", data.ScopeId.ToString()),
                        new Claim("nameid", data.NameId.ToString()),
                        new Claim("email", !string.IsNullOrEmpty(data.Email) ? data.Email.ToString() : ""),
                        new Claim("name" , data.Name),
                        new Claim("created_by" , data.CreatedBy.ToString())
                    ],
                    Double.Parse(_configuration["TimeInMinutes:OtherApp"].ToString()),
                    Double.Parse(_configuration["TimeInMinutes:OtherAppRefresh"].ToString()),
                    long.Parse(data.NameId)
                );

                response.result.AccessToken = jwtToken.AccessToken;
                response.result.RefreshToken = jwtToken.RefreshToken;

                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Access token sent";
                return response;
            }
            catch(Exception ex)
            {
                    response.result = null;
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Invalid token structure";
                return response;
            }
        }

        [NoRoleAuthorize()]
        [HttpGet("check-auth")]
        public async Task<APIResponseClass<AuthTokenForModules>> CheckAuthentication()
        {
            APIResponseClass<AuthTokenForModules> response = new();
            Application? appData = _claimService.GetApplication();
            int userId = _claimService.GetUserId();
            if (userId > 0)
            {
                if (appData == null)
                {
                    var jwtDataPayload = new ApplicationDTO
                    {
                        Id = 0,
                        Name = "User Management",
                        Role = new RoleDTO
                        {
                            Id = 0,
                            Name = "IFMS USER",
                            Level = new LevelDTO
                            {
                                Id = 0,
                                Name = "Role Selection",
                                Scope = "Application",
                                ScopeId = 0,
                            },
                            Permissions = ["verified"],
                        }
                    };

                    (string MobileNumber, string? Email, string UserName) = await _userService.GetUserDetailForLogin(userId);


                    response.result = await _jwtService.IssueJwtToken(
                        0,
                        [
                            new Claim("application", JsonConvert.SerializeObject(jwtDataPayload)),
                            new Claim("nameid", userId.ToString()),
                            new Claim("name" , _claimService.GetUserName()),
                            new Claim("username" , UserName),
                            new Claim("email", Email ?? ""),
                            new Claim("mobilenumber", MobileNumber),
                        ],
                        Double.Parse(_configuration["TimeInMinutes:SSO"]!.ToString()),
                        Double.Parse(_configuration["TimeInMinutes:SSORefresh"]!.ToString()),
                        _claimService.GetUserId()
                    );
                    response.result.GuidToken = _configuration["TimeInMinutes:SSORefresh"];
                    response.result.RefreshTokenValidityInMinutes = Double.Parse(_configuration["TimeInMinutes:SSORefresh"] ?? "10");
                    response.apiResponseStatus = Enum.APIResponseStatus.Warning;
                    response.message = "User re-authenticated";
                }
                else
                {
                    response.result = null;
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = $"User already authenticated for app: {appData.Id}:{appData.Name} tokenId: {_claimService.GetTokenId()}";
                }
            }
            else
            {
                response.result = null;
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Unable to retrieve session, please login again.";
            }
            return await Task.FromResult(response);
        }

        [NoRoleAuthorize()]
        [HttpGet("refresh-token")]
        public async Task<APIResponseClass<AuthTokenForModules>> RenewOtherAppTokens()
        {
            APIResponseClass<AuthTokenForModules> response = new()
            {
                result = null,
                apiResponseStatus = Enum.APIResponseStatus.Error
            };

            int userId = _claimService.GetUserId();
            if (userId == 0) {
                response.message = "Unable to renew token, please login again.";
                return await Task.FromResult(response);
            }

            Claim[] jwtClaims = _claimService.GetRawJwtClaims();
            if (jwtClaims == null || jwtClaims.Length == 0)
            {
                response.message = "Invalid session data, please login again.";
                return await Task.FromResult(response);
            }

            string tokenType = jwtClaims.FirstOrDefault(
                    c => c.Type == JwtRegisteredClaimNames.Typ
                )?.Value ?? "0";

            if (tokenType != "ref")
            {
                response.message = "Refresh token is required to renew tokens.";
                return await Task.FromResult(response);
            }

            OtherModuleClaimsDTO otherModuleClaims = _claimService.GetClaimsForJWTForOtherApplication();
            if (otherModuleClaims.AppId > 5)
            {
                (string MobileNumber, string? Email, string UserName) = await _userService.GetUserDetailForLogin(userId);

                response.result = await _jwtService.IssueJwtToken(
                    otherModuleClaims.AppId,
                    jwtClaims,
                    Double.Parse(_configuration["TimeInMinutes:OtherApp"]!.ToString()),
                    Double.Parse(_configuration["TimeInMinutes:OtherAppRefresh"]!.ToString()),
                    _claimService.GetUserId()
                );

                response.result.GuidToken = _configuration["TimeInMinutes:OtherAppRefresh"];
                response.result.RefreshTokenValidityInMinutes = Double.Parse(_configuration["TimeInMinutes:OtherAppRefresh"] ?? "10");
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Tokens refreshed successfully.";
            }
            else
            {
                response.message = "Token is not valid for this application: " + otherModuleClaims.AppId;
                return await Task.FromResult(response);
            }

            return await Task.FromResult(response);
        }

        [NoRoleAuthorize()]
        [HttpGet("RefreshTokenForSSOUMMM")]
        public async Task<APIResponseClass<AuthToken>> RefreshTokenForSSOUMMM()
        {
            var response = new APIResponseClass<AuthToken>();
            var application = _claimService.GetApplication();
            if (application != null)
            {
                var userData = await _userService.GetUserPhoneEmailDueLogin(_claimService.GetUserId());
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Auth:SecretKey"]));
                var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);
                var accessToken = String.Empty;
                var refreshToken = String.Empty;
                var time = String.Empty;
                var refreshtime = String.Empty;
                if (application.Id == 1 || application.Id == 5)
                {
                    
                    AuthTokenForModules jwtTokens = await _jwtService.IssueJwtToken(
                        application.Id,
                        [
                        new Claim("application", JsonConvert.SerializeObject(application)),
                        new Claim("nameid", _claimService.GetUserId().ToString()),
                        new Claim("name" , _claimService.GetUserName()),
                        new Claim("email", userData.Item2),
                        new Claim("mobilenumber", userData.Item1),
                        ],
                        Double.Parse(_configuration["TimeInMinutes:UMMM"].ToString()),
                        Double.Parse(_configuration["TimeInMinutes:UMMMRefresh"].ToString()),
                        _claimService.GetUserId()
                    );
                    accessToken = jwtTokens.AccessToken;
                    refreshToken = jwtTokens.RefreshToken;

                    // time = DateTime.UtcNow.AddMinutes(Double.Parse(_configuration["TimeInMinutes:UMMM"].ToString())).ToString();
                    // refreshtime = DateTime.UtcNow.AddMinutes(Double.Parse(_configuration["TimeInMinutes:UMMM"].ToString())).ToString();
                    // var accessClaims = new[]
                    // {
                    //     new Claim("typ", "acc"),
                    //     new Claim("application", JsonConvert.SerializeObject(application)),
                    //     new Claim("nameid", _claimService.GetUserId().ToString()),
                    //     new Claim("name" , _claimService.GetUserName()),
                    //     new Claim("email", userData.Item2),
                    //     new Claim("mobilenumber", userData.Item1),
                    //     new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                    //     // new Claim(JwtRegisteredClaimNames.Nbf, DateTime.UtcNow.ToString()),
                    //     // new Claim(JwtRegisteredClaimNames.Exp, time)
                    // };

                    // var accessTokenData = new JwtSecurityToken(
                    //     _configuration["Jwt:Issuer"],
                    //     _configuration["Jwt:Audience"],
                    //     accessClaims,
                    //     notBefore: DateTime.UtcNow,
                    //     expires: DateTime.UtcNow.AddMinutes(Double.Parse(_configuration["TimeInMinutes:UMMM"].ToString())),
                    //     signingCredentials: signIn);

                    // accessToken = new JwtSecurityTokenHandler().WriteToken(accessTokenData);
                    
                    // var refreshClaims = new[]
                    // {
                    //     new Claim("typ", "ref"),
                    //     new Claim("application", JsonConvert.SerializeObject(application)),
                    //     new Claim("nameid", _claimService.GetUserId().ToString()),
                    //     new Claim("name" , _claimService.GetUserName()),
                    //     new Claim("email", userData.Item2),
                    //     new Claim("mobilenumber", userData.Item1),
                    //     new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                    //     // new Claim(JwtRegisteredClaimNames.Nbf, DateTime.UtcNow.ToString()),
                    //     // new Claim(JwtRegisteredClaimNames.Exp, time)
                    // };

                    // var refreshTokenData = new JwtSecurityToken(
                    //     _configuration["Jwt:Issuer"],
                    //     _configuration["Jwt:Audience"],
                    //     refreshClaims,
                    //     notBefore: DateTime.UtcNow,
                    //     expires: DateTime.UtcNow.AddMinutes(Double.Parse(_configuration["TimeInMinutes:UMMMRefresh"].ToString())),
                    //     signingCredentials: signIn);

                    // refreshToken = new JwtSecurityTokenHandler().WriteToken(refreshTokenData);
                    
                }
                else
                {
                    // time = DateTime.UtcNow.AddMinutes(Double.Parse(_configuration["TimeInMinutes:SSO"].ToString())).ToString();
                    // var accessClaims = new[]
                    // {
                    //     new Claim("typ", "acc"),
                    //     new Claim("application", JsonConvert.SerializeObject(application)),
                    //     new Claim("nameid", _claimService.GetUserId().ToString()),
                    //     new Claim("name" , _claimService.GetUserName()),
                    //     new Claim("email", userData.Item2),
                    //     new Claim("mobilenumber", userData.Item1),
                    //     new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                    //     // new Claim(JwtRegisteredClaimNames.Nbf, DateTime.UtcNow.ToString()),
                    //     // new Claim(JwtRegisteredClaimNames.Exp, time)
                    // };

                    // var accessTokenData = new JwtSecurityToken(
                    //     _configuration["Jwt:Issuer"],
                    //     _configuration["Jwt:Audience"],
                    //     accessClaims,
                    //     notBefore: DateTime.UtcNow,
                    //     expires: DateTime.UtcNow.AddMinutes(Double.Parse(_configuration["TimeInMinutes:SSO"].ToString())),
                    //     signingCredentials: signIn);

                    // accessToken = new JwtSecurityTokenHandler().WriteToken(accessTokenData);
                    // refreshtime = DateTime.UtcNow.AddMinutes(Double.Parse(_configuration["TimeInMinutes:SSO"].ToString())).ToString();
                    // var refreshClaims = new[]
                    // {
                    //     new Claim("typ", "ref"),
                    //     new Claim("application", JsonConvert.SerializeObject(application)),
                    //     new Claim("nameid", _claimService.GetUserId().ToString()),
                    //     new Claim("name" , _claimService.GetUserName()),
                    //     new Claim("email", userData.Item2),
                    //     new Claim("mobilenumber", userData.Item1),
                    //     new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                    //     // new Claim(JwtRegisteredClaimNames.Nbf, DateTime.UtcNow.ToString()),
                    //     // new Claim(JwtRegisteredClaimNames.Exp, time)
                    // };

                    // var refreshTokenData = new JwtSecurityToken(
                    //     _configuration["Jwt:Issuer"],
                    //     _configuration["Jwt:Audience"],
                    //     refreshClaims,
                    //     notBefore: DateTime.UtcNow,
                    //     expires: DateTime.UtcNow.AddMinutes(Double.Parse(_configuration["TimeInMinutes:SSORefresh"].ToString())),
                    //     signingCredentials: signIn);

                    // refreshToken = new JwtSecurityTokenHandler().WriteToken(refreshTokenData);

                    AuthTokenForModules jwtToken = await _jwtService.IssueJwtToken(
                        application.Id,
                        [
                            new Claim("application", JsonConvert.SerializeObject(application)),
                            new Claim("nameid", _claimService.GetUserId().ToString()),
                            new Claim("name" , _claimService.GetUserName()),
                            new Claim("email", userData.Item2),
                            new Claim("mobilenumber", userData.Item1)
                        ], 
                        accessTokenValidityInMinutes: Double.Parse(_configuration["TimeInMinutes:SSO"].ToString()), 
                        refreshTokenValidityInMinutes: Double.Parse(_configuration["TimeInMinutes:SSORefresh"].ToString())
                    );

                    accessToken = jwtToken.AccessToken;
                    refreshToken = jwtToken.RefreshToken;
                    
                }

                response.result = new AuthToken
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                };
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Access token sent";
            }
            else
            {
                response.result = null;
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Invalid token structure";
            }
            return response;
        }

        [AllowAnonymous]
        [HttpPost("ChangeForgottenPassword")]
        public Task<APIResponseClass<bool>> ChangeForgottenPassword(ChangeForgottenPasswordDTO changeForgottenPassword)
        {
            return _userService.ChangeForgottenPassword(changeForgottenPassword);
        }

        [AllowAnonymous]
        [HttpGet("SendOTPForForgotPassword")]
        public Task<APIResponseClass<OTPResponseDTO>> SendOTPForForgotPassword(string username)
        {
            return _userService.SendOTPForForgotPassword(username);
        }
        public class MemoryStreamJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(MemoryStream).IsAssignableFrom(objectType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var bytes = serializer.Deserialize<byte[]>(reader);
                return bytes != null ? new MemoryStream(bytes) : new MemoryStream();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var bytes = ((MemoryStream)value).ToArray();
                serializer.Serialize(writer, bytes);
            }
        }

        [AllowAnonymous]
        [HttpPost("VerifyUserCredentials")]
        public async Task<APIResponseClass<VerifiedUserDTO>> VerifyUserCredentials(UserCredentialDTO userCredentialDTO)
                    {
            APIResponseClass<VerifiedUserDTO> response = new();
            try
            {
                string? publicIP = "";
                if (HttpContext.Request.Headers.ContainsKey("Src") && !string.IsNullOrEmpty(HttpContext.Request.Headers["Src"].ToString()))
                {
                    publicIP = HttpContext.Request.Headers["Src"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
                }
                var privateIP = "";
                var device = userCredentialDTO.DeviceInfo[0];
                var agent = userCredentialDTO.DeviceInfo[1];
                var res = await _userService.VerifyUserCredentials(userCredentialDTO, publicIP, privateIP, device, agent);
                if (res != null && res.result != null)
                {
                    if (!string.IsNullOrEmpty(publicIP))
                    {
                        BlacklistStore.BlacklistedIps.TryRemove(publicIP, out _);
                        var cacheKey = $"RateLimit_{publicIP}";
                        RateLimitingCache.Cache.Remove(cacheKey);
                    }
                    return res;
                }
                else
                {
                    response.apiResponseStatus = res?.apiResponseStatus ?? Enum.APIResponseStatus.Error;
                    response.message = res?.message ?? "Login failed, please try again..";
                    response.result = null;
                    return response;
                }
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Login failed, please try again.." + Ex.ToString();
                return response;
            }
        }
    }
}

