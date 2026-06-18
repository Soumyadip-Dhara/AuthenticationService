using AutoMapper;
using backend.Helpers;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL.Interfaces;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.Helper;
using UserManagement.Models.DTO;
using UserManagement.Utils.Interfaces;
using UserManagement.DAL.Entities;
using AngleSharp.Text;
using UserManagement.Enum;
using AngleSharp.Css;
using System.Security.Cryptography;
using UserManagement.DAL.Interfaces.MQueue;
using UserManagement.RbbitMQ;
using UserManagement.RbbitMQ.Constant;
using UserMangement.BAL.Interfaces.MQueue;

namespace UserManagement.BAL.Services
{
    public class UserMasterService : IUserService
    {
        private readonly IMapper _mapper;
        private readonly IUserMasterRepository _userMasterRepository;

        private readonly IClaimService _claimService;
        private readonly IConfiguration _config;
        private readonly IJWTService _JWTService;
        private readonly IUserService _userService;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserHasApplicationRepository _userHasApplicationRepository;
        private readonly IUserApplicationHasUserRoleRepository _userApplicationHasUserRoleRepository;
        private readonly IUserRoleHasUserLevelRepository _userRoleHasUserLevelRepository;
        private readonly IUserRoleHasUserPermissionRepository _userRoleHasUserPermissionRepository;
        private readonly IUserLevelHasUserScopeRepository _userLevelHasUserScopeRepository;
        private readonly IScopeRepository _scopeRepository;
        private readonly IRabbitMQPublisherService _rabbitMQService;
        private readonly ILoginLogRepository _loginLogRepository;
        private readonly INotificationService _notificationService;
        private readonly IPasswordChangeLogRepository _passwordChangeLogRepository;
        private readonly IOtpRepository _OtpRepository;
        private readonly IOTPService _otpService;
        private readonly IUserActivityLogRepository _userActivityLogRepository;
        private readonly IUserHasUserManagementRepository _userHasUserManagementRepository;
        private readonly IUserHasModuleManagementRepository _userHasModuleManagementRepository;
        private readonly IRoleRepository _roleRepository;
         private readonly IMessageQueueRepository _messageQueueRepository;
        private readonly IMQueueProcessingService _mQueueProcessingService;
        private readonly IAppScopeRepository _appScopeRepository;

        public UserMasterService(IMapper mapper, IUserMasterRepository userMasterRepository, IApplicationRepository applicationRepository, IClaimService claimService, IConfiguration config, IJWTService jwtService, ILoginLogRepository LoginLogRepository, IRoleRepository roleRepository,
            IUserHasApplicationRepository userHasApplicationRepository, IUserApplicationHasUserRoleRepository userHasUserRoleRepository, IUserHasUserManagementRepository userHasUserManagementRepository,
            IUserHasModuleManagementRepository userHasModuleManagement,
            IUserRoleHasUserLevelRepository userRoleHasUserLevelRepository, IUserRoleHasUserPermissionRepository userRoleHasUserPermissionRepository, IScopeRepository scopeRepository, IRabbitMQPublisherService rabbitMQService,
            IUserLevelHasUserScopeRepository userLevelHasUserScopeRepository, INotificationService notificationService, IPasswordChangeLogRepository passwordChangeLogRepository, IOtpRepository OtpRepository, IOTPService otpService, IUserActivityLogRepository userActivityLogRepository, IMessageQueueRepository messageQueueRepository, IMQueueProcessingService mQueueProcessingService, IAppScopeRepository appScopeRepository)
        {
            _userMasterRepository = userMasterRepository;

            _claimService = claimService;
            _config = config;
            _mapper = mapper;
            _JWTService = jwtService;
            _applicationRepository = applicationRepository;
            _userHasApplicationRepository = userHasApplicationRepository;
            _userApplicationHasUserRoleRepository = userHasUserRoleRepository;
            _userRoleHasUserLevelRepository = userRoleHasUserLevelRepository;
            _userRoleHasUserPermissionRepository = userRoleHasUserPermissionRepository;
            _userLevelHasUserScopeRepository = userLevelHasUserScopeRepository;
            _scopeRepository = scopeRepository;
            _rabbitMQService = rabbitMQService;
            _loginLogRepository = LoginLogRepository;
            _notificationService = notificationService;
            _passwordChangeLogRepository = passwordChangeLogRepository;
            _OtpRepository = OtpRepository;
            _otpService = otpService;
            _userActivityLogRepository = userActivityLogRepository;
            _userHasUserManagementRepository = userHasUserManagementRepository;
            _userHasModuleManagementRepository = userHasModuleManagement;
            _roleRepository = roleRepository;
            _messageQueueRepository = messageQueueRepository;
            _mQueueProcessingService = mQueueProcessingService;
            _appScopeRepository = appScopeRepository;
        }

        public async Task<bool> CheckExistingUserByLoginId(string userName)
        {
            var res = await _userMasterRepository.GetSingleSelectedColumnByConditionAsync(e => e.UserName == userName,
                e => e.Id);
            return res != 0;
        }

        public (string, string) GetJWTFromAccessToken(string accessToken)
        {
            if (accessToken != null)
            {
                //string query = "SELECT id, access_token->'hi' AS access_token_value FROM access_token.hstore_token;";
                var data = _userMasterRepository.GetJWTFromAccessToken(accessToken);
                return data;
            }
            else
            {
                return (null, null);
            }
        }
        private bool SetJWTAccessToken(string accessToken, (string, string) data)
        {
            if (accessToken != "" && data != (null, null))
            {
                _userMasterRepository.SetJWTAccessToken(accessToken, data);
                return true;
            }
            else
            {
                return false;
            }
        }
        public async Task<bool> DeleteUser(long userId)
        {
            var user = _userMasterRepository.GetSingle(t => t.Id == userId);
            user.IsActive = false;
            if (_userMasterRepository.Update(user))
            {
                if (user.IsAnAdmin)
                {
                    var scopeIds = await _userLevelHasUserScopeRepository.GetSelectedColumnByConditionAsync(
                        e => e.UserRoleHasLevel.UserHasApp.UserId == userId,
                        e => e.UserLevelHasScopeId);

                    var scopes = await _appScopeRepository.GetAllByConditionAsync(
                        e => scopeIds.Contains(e.ScopeId));
                    var flag = false;
                    foreach (var scope in scopes)
                    {
                        scope.IsAdminCreated = false;
                        if (!_appScopeRepository.Update(scope))
                        {
                            flag = true;
                        }
                    }
                    if (flag == false)
                    {
                        _scopeRepository.SaveChangesManaged();
                        _userMasterRepository.SaveChangesManaged();
                        return true;
                    }

                    return false;
                }
                _userMasterRepository.SaveChangesManaged();
                return true;
            }
            return false;
        }
        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }
        private static string ExtractValue(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            var parts = name.Split(") -");
            return parts.Length > 1 ? parts.Last().Trim() : string.Empty;
        }
        private static string ExtractCode(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            var start = name.IndexOf('(');
            var end = name.IndexOf(')');
            if (start >= 0 && end > start)
            {
                return name.Substring(start + 1, end - start - 1).Trim();
            }
            return string.Empty;
        }
        public static string GeneratePassword()
        {
            const int length = 12;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz@#&*$";

            char[] password = new char[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] buffer = new byte[length];
                rng.GetBytes(buffer);

                for (int i = 0; i < length; i++)
                {
                    password[i] = chars[buffer[i] % chars.Length];
                }
            }

            return new string(password);
        }

        public async Task<(bool, string)> NewUserRegistrationBySuperAdmin(UserRegistrationNewDTO user, bool isSuperAdminCreation)
        {
            //string password = "Ifms@WB2025";
            string password = GeneratePassword();
            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);
            var res = await _userMasterRepository.NewUserRegistrationBySuperAdmin(user, password, passwordHash, passwordSalt, isSuperAdminCreation);
            if (res.Item1)
            {
                var emailTask = new EmailPayload
                {
                    Email = user.UserMaster.Email,
                    Subject = "Welcome to IFMS - Your Account Has Been Created!",
                    Body = $@"
                        <html>
                            <body>
                                <h1>Welcome to the IFMS!</h1>
                                <p>Dear {user.UserMaster.Name},</p>
                                <p>We are thrilled to inform you that your account has been successfully created.</p>
                                <p>Your username for logging into the IFMS is: <strong>{user.UserMaster.UserName}</strong></p>
                                <p>Your temporary password is: <strong>{password}</strong></p>
                                <p>Please log in to the system and change your password at your earliest convenience to ensure the security of your account.</p>
                                <p>If you have any questions or need assistance, feel free to reach out to our support team.</p>
                                <p>Best Regards,<br/>The IFMS Team</p>
                            </body>
                        </html>"
                };

                var smsTask = new SmsPayload
                {
                    TemplateId = "1307159959651076860",
                    PhoneNumber = user.UserMaster.MobileNumber,
                    UserId = user.UserMaster.UserName,
                    Password = password
                };

                if (user.UserAccess != null && user.UserAccess.Count > 0)
                {
                    var queueNameToPrivilegesMap = new Dictionary<string, List<RegisterUserPrivilegeDTO>>();
                    foreach (var access in user.UserAccess)
                    {
                        if (!queueNameToPrivilegesMap.TryGetValue(access.AppName, out var privileges))
                        {
                            queueNameToPrivilegesMap[access.AppName] = new List<RegisterUserPrivilegeDTO>();
                        }

                        //var scopeList = access.Scope.Select(async s => new ScopeValue
                        //{
                        //    Id = s.Id,
                        //    Name = ExtractCode(s.Name),
                        //    Value = ExtractValue(s.Name),
                        //    ParentScope = (await _scopeRepository.GetParentScopeValuesAsync(s.Name, access.LevelId)).FirstOrDefault()

                        //}).ToList();
                        var scopeList = (await Task.WhenAll(
                            access.Scope.Select(async s => new ScopeValue
                            {
                                Id = s.Id,
                                Name = ExtractValue(s.Name),
                                Value = ExtractCode(s.Name),
                                ParentScope = (await _scopeRepository
                                    .GetParentScopeValuesAsync(ExtractCode(s.Name), access.LevelId))
                                    .FirstOrDefault()
                            })
                        )).ToList();

                        var privilege = new RegisterUserPrivilegeDTO
                        {
                            AppId = access.AppId,
                            AppName = access.AppName,
                            RoleId = access.RoleId,
                            RoleName = access.RoleName,
                            Permissions = access.Permission,
                            LevelId = access.LevelId,
                            LevelName = access.LevelName,
                            Scopes = scopeList,
                            IsAdmin = access.UserManagementEnabled
                        };

                        queueNameToPrivilegesMap[access.AppName].Add(privilege);
                    }

                    var userData = new UserDetailsWithPrivilegesDTO
                    {
                        Id = res.Item3,
                        Name = user.UserMaster.Name,
                        UserName = user.UserMaster.UserName,
                        Email = user.UserMaster.Email,
                        Designation = user.UserMaster.Designation,
                        HrmsId = user.UserMaster.HrmsId,
                        MobileNumber = user.UserMaster.MobileNumber,
                        EffectiveFrom = DateTime.Now.ToString("yyyy-MM-dd"),
                        ExpiresOn = DateTime.Now.AddYears(1).ToString("yyyy-MM-dd"),
                        IsActive = true
                    };


                    var consumingApps = await _applicationRepository.GetSelectedColumnByConditionAsync(
                        a => a.IsConsumingData == true,
                        a => new { a.Id }
                    );

                    var consumingAppIds = consumingApps.Select(x => x.Id).ToHashSet();

                    foreach (var queueName in queueNameToPrivilegesMap.Keys)
                    {
                        var privileges = queueNameToPrivilegesMap[queueName];

                        if (privileges.Any(p => consumingAppIds.Contains(p.AppId)))
                        {
                            userData.Privileges = privileges;
                            //await _rabbitMQService.PublishMessage($"{queueName}-GETUSERS", userData);
                            // throughy database queue for reliable message delivery and to handle cases where the consuming service might be down

                            try {
                                var message_id = Guid.NewGuid();
                                _messageQueueRepository.Add(new MessageQueue
                                {
                                    UniqueId = message_id,
                                    QueueName = $"UM_{queueName}_USER".ToLower(),
                                    MessageBody = JsonConvert.SerializeObject(userData),
                                    CreatedAt = DateTime.Now
                                });
                                _messageQueueRepository.SaveChangesManaged();
                                await _mQueueProcessingService.ProcessQueueAsync($"UM_{queueName}_USER");
                            }
                            catch (Exception ex )
                            {
                                throw;
                            }
                            
                        }

                    }

                    //foreach (var queueName in queueNameToPrivilegesMap.Keys)
                    //{
                    //    userData.Privileges = queueNameToPrivilegesMap[queueName];
                    //    await _rabbitMQService.PublishMessage($"{queueName}-GETUSERS", userData);
                    //}
                    queueNameToPrivilegesMap.Clear();
                }

                //var emailRes = await _notificationService.SendEmailUsingQueue(emailTask);

                var smsRes = await _notificationService.SendSmsUsingQueue(smsTask);
                if (smsRes.status == 3)
                {
                    throw new Exception(smsRes.message);
                }
            }
            return (res.Item1, res.Item2);
        }
        public async Task<(bool, string)> NewUserRegistration(UserRegistrationNewDTO user)
        {
            //string password = "Ifms@WB2025";
            string password = GeneratePassword();
            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);
            var res = await _userMasterRepository.UserRegistration(user, password, passwordHash, passwordSalt);
            if (res.Item1)
            {
                var emailTask = new EmailPayload
                {
                    Email = user.UserMaster.Email,
                    Subject = "Welcome to IFMS - Your Account Has Been Created!",
                    Body = $@"
                        <html>
                            <body>
                                <h1>Welcome to the IFMS!</h1>
                                <p>Dear {user.UserMaster.Name},</p>
                                <p>We are thrilled to inform you that your account has been successfully created.</p>
                                <p>Your username for logging into the IFMS is: <strong>{user.UserMaster.UserName}</strong></p>
                                <p>Your temporary password is: <strong>{password}</strong></p>
                                <p>Please log in to the system and change your password at your earliest convenience to ensure the security of your account.</p>
                                <p>If you have any questions or need assistance, feel free to reach out to our support team.</p>
                                <p>Best Regards,<br/>The IFMS Team</p>
                            </body>
                        </html>"
                };

                var smsTask = new SmsPayload
                {
                    TemplateId = "1307159959651076860",
                    PhoneNumber = user.UserMaster.MobileNumber,
                    UserId = user.UserMaster.UserName,
                    Password = password
                };

                if (user.UserAccess != null && user.UserAccess.Count > 0)
                {
                    var queueNameToPrivilegesMap = new Dictionary<string, List<RegisterUserPrivilegeDTO>>();
                    foreach (var access in user.UserAccess)
                    {
                        if (!queueNameToPrivilegesMap.TryGetValue(access.AppName, out var privileges))
                        {
                            queueNameToPrivilegesMap[access.AppName] = new List<RegisterUserPrivilegeDTO>();
                        }

                        //var scopeList = access.Scope.Select(async s => new ScopeValue
                        //{
                        //    Id = s.Id,
                        //    Name = ExtractCode(s.Name),
                        //    Value = ExtractValue(s.Name),
                        //    ParentScope = (await _scopeRepository.GetParentScopeValuesAsync(s.Name, access.LevelId)).FirstOrDefault()

                        //}).ToList();
                        var scopeList = (await Task.WhenAll(
                            access.Scope.Select(async s => new ScopeValue
                            {
                                Id = s.Id,
                                Name = ExtractValue(s.Name),
                                Value = ExtractCode(s.Name),
                                ParentScope = (await _scopeRepository
                                    .GetParentScopeValuesAsync(ExtractCode(s.Name), access.LevelId))
                                    .FirstOrDefault()
                            })
                        )).ToList();

                        var privilege = new RegisterUserPrivilegeDTO
                        {
                            AppId = access.AppId,
                            AppName = access.AppName,
                            RoleId = access.RoleId,
                            RoleName = access.RoleName,
                            Permissions = access.Permission,
                            LevelId = access.LevelId,
                            LevelName = access.LevelName,
                            Scopes = scopeList,
                            IsAdmin = access.UserManagementEnabled
                        };

                        queueNameToPrivilegesMap[access.AppName].Add(privilege);
                    }

                    var userData = new UserDetailsWithPrivilegesDTO
                    {
                        Id = res.Item3,
                        Name = user.UserMaster.Name,
                        UserName = user.UserMaster.UserName,
                        Email = user.UserMaster.Email,
                        Designation = user.UserMaster.Designation,
                        HrmsId = user.UserMaster.HrmsId,
                        MobileNumber = user.UserMaster.MobileNumber,
                        EffectiveFrom = DateTime.Now.ToString("yyyy-MM-dd"),
                        ExpiresOn = DateTime.Now.AddYears(1).ToString("yyyy-MM-dd"),
                        IsActive = true
                    };


                    var consumingApps = await _applicationRepository.GetSelectedColumnByConditionAsync(
                        a => a.IsConsumingData == true,
                        a => new { a.Id }
                    );

                    var consumingAppIds = consumingApps.Select(x => x.Id).ToHashSet();

                    foreach (var queueName in queueNameToPrivilegesMap.Keys)
                    {
                        var privileges = queueNameToPrivilegesMap[queueName];

                        if (privileges.Any(p => consumingAppIds.Contains(p.AppId)))
                        {
                            userData.Privileges = privileges;
                            //await _rabbitMQService.PublishMessage($"{queueName}-GETUSERS", userData);
                            // throughy database queue for reliable message delivery and to handle cases where the consuming service might be down

                            try
                            {
                                var message_id = Guid.NewGuid();
                                _messageQueueRepository.Add(new MessageQueue
                                {
                                    UniqueId = message_id,
                                    QueueName = $"UM_{queueName}_USER".ToLower(),
                                    MessageBody = JsonConvert.SerializeObject(userData),
                                    CreatedAt = DateTime.Now
                                });
                                _messageQueueRepository.SaveChangesManaged();
                                await _mQueueProcessingService.ProcessQueueAsync($"UM_{queueName}_USER");
                            }
                            catch (Exception ex)
                            {
                                throw;
                            }

                        }

                    }

                    //foreach (var queueName in queueNameToPrivilegesMap.Keys)
                    //{
                    //    userData.Privileges = queueNameToPrivilegesMap[queueName];
                    //    await _rabbitMQService.PublishMessage($"{queueName}-GETUSERS", userData);
                    //}
                    queueNameToPrivilegesMap.Clear();
                }
                //var emailRes = await _notificationService.SendEmailUsingQueue(emailTask);
                var smsRes = await _notificationService.SendSmsUsingQueue(smsTask);
                if (smsRes.status == 3)
                {
                    throw new Exception(smsRes.message);
                }

            }
            return (res.Item1, res.Item2);
        }
        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != passwordHash[i]) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Mask phone number to show only last 4 digits
        /// </summary>
        private string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
                return "XXXXXX0000";
            
            return "XXXXXX" + phoneNumber.Substring(phoneNumber.Length - 4);
        }

        public async Task<APIResponseClass<UserLoginTokenDTO>> UserLogin(UserLoginDTO userLoginDTO, string publicIP, string privateIP, string device, string agent)
        {
            APIResponseClass<UserLoginTokenDTO> response = new();
            try
            {
                var user = await _userMasterRepository.GetSingleAysnc(e => e.UserName == userLoginDTO.Username);


                if (user == null)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Invalid username or password.";
                    response.result = null;
                    return response;
                }

                if (!(bool)user.IsActive)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Your User Id is Suspendend. Please contact your department.";
                    response.result = null;
                    return response;
                }

                if (Convert.ToBoolean(user.IsBlocked))
                {
                    TimeOnly blockTime = user.BlockTime ?? TimeOnly.MinValue;
                    double blockDuration = double.Parse(_config["Block:TimeInMinutes"]);
                    TimeOnly unblockTime = blockTime.AddMinutes(blockDuration);
                    TimeOnly currentTime = TimeOnly.FromDateTime(DateTime.Now);

                    if (unblockTime > currentTime)
                    {
                        // Calculate the remaining blocked time in minutes
                        int remainingMinutes = (int)(unblockTime.ToTimeSpan().TotalMinutes - currentTime.ToTimeSpan().TotalMinutes);

                        response.apiResponseStatus = Enum.APIResponseStatus.Error;
                        response.message = $"Your User ID is Blocked Temporarily for {remainingMinutes} minutes.";
                        response.result = null;
                        return response;
                    }
                }
                var otpLog = await _OtpRepository.GetSingleAysnc(e => e.UserName == user.UserName && e.OtpType == (short)OTPType.Login);

                bool isMasterOPTEnable = Boolean.Parse(_config["OTP:MasterOTPEnable"]);
                string  masterOTP = "";
                if (isMasterOPTEnable)
                {
                    var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                    var first = MasterOTPHelper.DigitalSum(now.Year); // DigitalSum of the digits of the year        
                    var second = MasterOTPHelper.DigitalSum(now.Month); // DigitalSum of the digits of the month
                    var third = MasterOTPHelper.DigitalSum(now.Day); // DigitalSum of the digits of the day
                    var fourth = MasterOTPHelper.DigitalSum(now.Hour); // Last digit of the hour         
                    var fifth = MasterOTPHelper.DigitalSum(first + second + third + fourth); // Last digit of the sum of the first four digits
                    var sixth = MasterOTPHelper.DigitalSum(first + second + third + fourth + fifth); // Last digit of the sum of the first five digits
                    masterOTP = ((((((first * 10) + second) * 10 + third) * 10 + fourth) * 10 + fifth) * 10 + sixth).ToString(); // Final data
                }
                if (otpLog == null)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "No OTP in Database";
                    response.result = null;
                    return response;
                }
                else if (otpLog.CreatedAt.AddMinutes(Double.Parse(_config["OTP:Delay"])) < DateTime.Now)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "OTP expired";
                    response.result = null;
                    return response;
                }
                else if (otpLog.OtpValue != userLoginDTO.Otp && userLoginDTO.Otp != masterOTP.ToString())
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Invalid OTP";
#if DEBUG
                    response.message = "Invalid OTP: " + userLoginDTO.Otp + "\n Correct OTP: " + masterOTP;
#endif
                    response.result = null;
                    return response;
                }

                _OtpRepository.Delete(otpLog);
                _OtpRepository.SaveChangesManaged();

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


                // if (_config["Session:singleSession"].ToBoolean()) {
                //     _loginLogRepository.DeleteRange(
                //         await _loginLogRepository.GetAllByConditionAsync(e => e.UserId == user.Id)
                //     );
                // }

                // LoginLog? loginLog  = new()
                // {
                //     ApplicationId = 0,
                //     UserId = user.Id,
                //     LoginTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"))
                // };
                // _loginLogRepository.Add(loginLog);
                // _loginLogRepository.SaveChangesManaged();

                // var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Auth:SecretKey"]));
                // var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

                // var accessClaims = new[]
                // {
                //     new Claim("typ", "acc"),
                //     new Claim("application", JsonConvert.SerializeObject(jwtDataPayload)),
                //     new Claim("nameid", user.Id.ToString()),
                //     new Claim("name" , user.Name),
                //     new Claim("username" , user.UserName),
                //     new Claim("email", !string.IsNullOrEmpty(user.Email) ? user.Email.ToString() : ""),
                //     new Claim("mobilenumber", !string.IsNullOrEmpty(user.MobileNumber) ? user.MobileNumber.ToString() : ""),
                //     new Claim(JwtRegisteredClaimNames.Jti, loginLog.Id.ToString()),
                //     new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.AddSeconds(0).ToString()),
                //     // new Claim(JwtRegisteredClaimNames.Nbf, DateTime.UtcNow.AddSeconds(0).ToString()),
                //     // new Claim(JwtRegisteredClaimNames.Exp, DateTime.UtcNow.AddMinutes(Double.Parse(_config["TimeInMinutes:SSO"])).ToString())
                // };

                // var accessTokenData = new JwtSecurityToken(
                //     _config["Jwt:Issuer"],
                //     _config["Jwt:Audience"],
                //     accessClaims,
                //     DateTime.UtcNow,
                //     DateTime.UtcNow.AddMinutes(Double.Parse(_config["TimeInMinutes:SSO"].ToString())),
                //     signIn);

                // var accessToken = new JwtSecurityTokenHandler().WriteToken(accessTokenData);

                // var refreshClaims = new[]
                // {
                //     new Claim("typ", "ref"),
                //     new Claim("application", JsonConvert.SerializeObject(jwtDataPayload)),
                //     new Claim("nameid", user.Id.ToString()),
                //     new Claim("name" , user.Name),
                //     new Claim("username" , user.UserName),
                //     new Claim("email", !string.IsNullOrEmpty(user.Email) ? user.Email.ToString() : ""),
                //     new Claim("mobilenumber", !string.IsNullOrEmpty(user.MobileNumber) ? user.MobileNumber.ToString() : ""),
                //     new Claim(JwtRegisteredClaimNames.Jti, loginLog.Id.ToString()),
                //     new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.AddSeconds(0).ToString()),
                //     // new Claim(JwtRegisteredClaimNames.Nbf, DateTime.UtcNow.AddSeconds(0).ToString()),
                //     // new Claim(JwtRegisteredClaimNames.Exp, DateTime.UtcNow.AddMinutes(Double.Parse(_config["TimeInMinutes:SSORefresh"])).ToString())
                // };

                // var refreshTokenData = new JwtSecurityToken(
                //     _config["Jwt:Issuer"],
                //     _config["Jwt:Audience"],
                //     refreshClaims,
                //     DateTime.UtcNow,
                //     DateTime.UtcNow.AddMinutes(Double.Parse(_config["TimeInMinutes:SSORefresh"].ToString())),
                //     signIn);

                // var refreshToken = new JwtSecurityTokenHandler().WriteToken(refreshTokenData);
                
                var sessionId = Guid.NewGuid();

                var userActivityLog = new UserActivityLog
                {
                    UserId = user.Id,
                    IsLogin = true,
                    PublicIp = publicIP,
                    PrivateIp = privateIP,
                    Device = device,
                    Agent = agent,
                    SessionId = sessionId,

                };
                _userActivityLogRepository.Add(userActivityLog);
                _userActivityLogRepository.SaveChangesManaged();

                AuthTokenForModules jwtToken = await _JWTService.IssueJwtToken(
                    0,
                    [
                        new Claim("application", JsonConvert.SerializeObject(jwtDataPayload)),
                        new Claim("nameid", user.Id.ToString()),
                        new Claim("name" , user.Name),
                        new Claim("username" , user.UserName),
                        new Claim("email", !string.IsNullOrEmpty(user.Email) ? user.Email.ToString() : ""),
                        new Claim("mobilenumber", !string.IsNullOrEmpty(user.MobileNumber) ? user.MobileNumber.ToString() : ""),
                        new Claim("signerId", !string.IsNullOrEmpty(user.SignerId) ? user.SignerId.ToString() : "")

                    ],
                    Double.Parse(_config["TimeInMinutes:SSO"].ToString()),
                    Double.Parse(_config["TimeInMinutes:SSORefresh"].ToString()),
                    user.Id,
                    sessionId
                );

                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Credentials Verified.";
                response.result = new UserLoginTokenDTO
                {
                    AccessToken = jwtToken.AccessToken,
                    RefreshToken = jwtToken.RefreshToken,
                    IsFirstLogin = (bool)user.DueFirstLogin
                };
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Login failed, please try again..";
                return response;
            }
        }
        public async Task<AuthTokenForModules> GetJWTDirect(GetJWTPayload getJWTPayload)
        {
            if (getJWTPayload.Application[0].Id == 1 || getJWTPayload.Application[0].Id == 5)
                return await _JWTService.JWTTokenCreationForUMAndMMForMultiple(getJWTPayload);
            else
            {
                return await _JWTService.JWTTokenCreationForOtherForMultiple(getJWTPayload);
            }
        }
        public async Task<bool> IsSinglePriviledged(DataCollectionJWTDTO dataCollectionDTO)
        {


            int appId = dataCollectionDTO.AppId;
            int userId = dataCollectionDTO.UserId;

            // application
            var userHasApplication = await _userHasApplicationRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.AppId == appId && e.UserId == userId,
                e => new
                {
                    PrimaryKeyId = e.Id
                });

            // role
            var userApplicationHasRole = (List<long>)await _userApplicationHasUserRoleRepository.GetSelectedColumnByConditionAsync(
                e => e.UserHasAppId == userHasApplication.PrimaryKeyId,
                e => e.Id);


            // level
            var userRoleHasUserLevel = await _userRoleHasUserLevelRepository.GetSelectedColumnByConditionAsync(
                e => e.UserHasAppId == userHasApplication.PrimaryKeyId,
                e => new
                {
                    LevelId = e.RoleHasLevelId,
                    PrimayKey = e.Id
                });

            var LevelIds = userRoleHasUserLevel.Select(x => x.LevelId);
            var prmyIds = userRoleHasUserLevel.Select(x => x.PrimayKey);
            // scope
            var userLevelHasUserScope = (List<long>)await _userLevelHasUserScopeRepository.GetSelectedColumnByConditionAsync(
                e => LevelIds.Contains(e.LevelId) && prmyIds.Contains(e.UserRoleHasLevelId),
                e => e.UserLevelHasScopeId);


            return userApplicationHasRole.Count() == 1 && userRoleHasUserLevel.Count() == 1 && userLevelHasUserScope.Count() == 1;
        }
        public async Task<UserDetailsForDisplay> LockedUserDetailsForDisplay()
        {
            List<UserDetailsForDisplayDTO> users = new();

            string[] userRoles = _claimService.GetRoles();

            if (Array.Exists(userRoles, roleName => roleName == "Super Admin"))
            {
                users = (List<UserDetailsForDisplayDTO>)await _userMasterRepository.GetSelectedColumnByConditionAsync(
                    e => !(bool)e.IsActive,
                    e => new UserDetailsForDisplayDTO
                    {
                        HrmsId = e.HrmsId,
                        Designation = e.Designation,
                        Email = e.Email,
                        MobileNumber = e.MobileNumber,
                        Name = e.Name,
                        UserName = e.UserName,
                        Id = e.Id,
                        IsActive = e.IsActive
                    }
                );
            }
            else
            {
                users = (List<UserDetailsForDisplayDTO>)await _userMasterRepository.GetSelectedColumnByConditionAsync(
                    e => !(bool)e.IsActive && e.CreatedBy == _claimService.GetUserId(),
                    e => new UserDetailsForDisplayDTO
                    {
                        HrmsId = e.HrmsId,
                        Designation = e.Designation,
                        Email = e.Email,
                        MobileNumber = e.MobileNumber,
                        Name = e.Name,
                        UserName = e.UserName,
                        Id = e.Id,
                        IsActive = e.IsActive,
                        CreatedBy = e.CreatedByNavigation.Name
                    }
                );
            }
            return new UserDetailsForDisplay { Users = users, Count = (short)users.Count(), InActiveUsers = (short)users.Count() };
        }
        public async Task<UserDetailsForDisplayWithCountDTO> UserDetailsForDisplay(FilterData payload)
        {

            string[] userRoles = _claimService.GetRoles();
            var userId = _claimService.GetUserId();

            return await _userMasterRepository.UserDetailsForDisplay(payload, userRoles[0], userId);


            //List<UserDetailsForDisplayDTO> users = new();
            //ICollection<UserDetailsForDisplayDTO> allUsers;
            //int activeUsers = 0;
            //int inActiveUsers = 0;
            //int newlyRegisteredUsers = 0;

            //string[] userRoles = _claimService.GetRoles();
            //var userId = _claimService.GetUserId();

            //// Step 1: Fetch users with CreatedBy ID
            //if (Array.Exists(userRoles, roleName => roleName == "Super Admin"))
            //{
            //    // Apply filters based on Contains and StartsWith
            //    allUsers = (await _userMasterRepository.GetSelectedColumnAsync(
            //        e => new UserDetailsForDisplayDTO
            //        {
            //            Id = e.Id,
            //            HrmsId = e.HrmsId,
            //            Designation = e.Designation,
            //            Email = e.Email,
            //            MobileNumber = e.MobileNumber,
            //            Name = e.Name,
            //            UserName = e.UserName,
            //            IsActive = e.IsActive,
            //            IsBlocked = e.IsBlocked,
            //            CreatedAt = e.CreatedAt,
            //            CreatedBy = e.CreatedByNavigation.Name
            //        }
            //    ))
            //    .Where(user =>
            //    (!payload.Filters.ContainsKey("Name") || string.IsNullOrEmpty(payload.Filters["Name"].Value) || user.Name.StartsWith(payload.Filters["Name"].Value)) &&
            //    (!payload.Filters.ContainsKey("Email") || string.IsNullOrEmpty(payload.Filters["Email"].Value) || user.Email.Contains(payload.Filters["Email"].Value)) &&
            //    (!payload.Filters.ContainsKey("Designation") || string.IsNullOrEmpty(payload.Filters["Designation"].Value) || user.Designation.StartsWith(payload.Filters["Designation"].Value)) &&
            //    (!payload.Filters.ContainsKey("MobileNumber") || string.IsNullOrEmpty(payload.Filters["MobileNumber"].Value) || user.MobileNumber.StartsWith(payload.Filters["MobileNumber"].Value)) &&
            //    (!payload.Filters.ContainsKey("UserName") || string.IsNullOrEmpty(payload.Filters["UserName"].Value) || user.UserName.StartsWith(payload.Filters["UserName"].Value))
            //).ToList();


            //    //Apply pagination
            //    users = allUsers.Skip(payload.First).Take(payload.Rows).ToList();

            //}
            //else
            //{
            //    allUsers = (await _userMasterRepository.GetSelectedColumnByConditionAsync(
            //        e => e.CreatedBy == userId,
            //        e => new UserDetailsForDisplayDTO
            //        {
            //            Id = e.Id,
            //            HrmsId = e.HrmsId,
            //            Designation = e.Designation,
            //            Email = e.Email,
            //            MobileNumber = e.MobileNumber,
            //            Name = e.Name,
            //            UserName = e.UserName,
            //            IsActive = e.IsActive,
            //            IsBlocked = e.IsBlocked,
            //            CreatedAt = e.CreatedAt,
            //            CreatedBy = e.CreatedByNavigation.Name
            //        }
            //    ));
            //    users = allUsers.Skip(payload.First).Take(payload.Rows).ToList();
            //}

            ////// Step 2: Fetch names for CreatedBy IDs
            ////var createdByIds = users.Select(u => u.CreatedBy).Distinct();
            ////var createdByNames = await _userMasterRepository.GetSelectedColumnByConditionAsync(
            ////    x => createdByIds.Contains(x.Id.ToString()),
            ////    x => new { x.Id, x.Name }
            ////);

            ////var createdByNameMap = createdByNames.ToDictionary(x => x.Id, x => x.Name);

            //// Step 3: Map CreatedBy names to the users
            //foreach (var user in allUsers)
            //{
            //    //user.CreatedBy = createdByNameMap.GetValueOrDefault<long, string>(int.Parse(user.CreatedBy), "Unknown");

            //    // Count active and inactive users
            //    if (user.IsActive == true)
            //    {
            //        activeUsers += 1;
            //    }
            //    else
            //    {
            //        inActiveUsers += 1;
            //    }
            //    if (user.CreatedAt > DateTime.Now.AddDays(-1))
            //    {
            //        newlyRegisteredUsers += 1;
            //    }
            //}

            //return new UserDetailsForDisplayWithCountDTO
            //{
            //    Users = users,
            //    Count = (short)allUsers.Count,
            //    ActiveUsers = activeUsers,
            //    InActiveUsers = inActiveUsers,
            //    NewlyRegisteredUsers = newlyRegisteredUsers
            //};
        }
        public async Task<bool> UnlockedUserByUserIds(List<long> userId)
        {
            var user = await _userMasterRepository.GetSingleAysnc(e => userId.Contains(e.Id));
            user.IsActive = true;
            user.UnsuccessfulLoginAttempt = 0;
            var res = _userMasterRepository.Update(user);
            _userMasterRepository.SaveChangesManaged();
            return res;
        }

        public async Task<bool> ResetPassword(long userId)
        {
            var existingUser = await _userMasterRepository.GetSingleAysnc(e => e.Id == userId);
            if (existingUser != null)
            {
                string password = GeneratePassword();
                byte[] passwordHash, passwordSalt;
                CreatePasswordHash(password, out passwordHash, out passwordSalt);


                existingUser.PasswordHash = passwordHash;
                existingUser.PasswordSalt = passwordSalt;
                existingUser.DueFirstLogin = true;

                var smsTask = new SmsPayload
                {
                    TemplateId = "1307159959651076860",   // TO DO Change the template ID
                    PhoneNumber = existingUser.MobileNumber,
                    UserId = existingUser.UserName,
                    Password = password
                };


                if (_userMasterRepository.Update(existingUser))
                {
                    _userMasterRepository.SaveChangesManaged();

                    //var emailRes = await _notificationService.SendEmailUsingQueue(emailTask);
                    var smsRes = await _notificationService.SendSmsUsingQueue(smsTask);
                    if (smsRes.status == 3)
                    {
                        throw new Exception(smsRes.message);
                    }


                    return true;
                }
                return false;

            }
            else
            {
                return false;
            }
        }

        public async Task<bool> UpdateUser(UserUpdateDTO user)
        {
            var logedInUserId = _claimService.GetUserId();
            var existingUser = await _userMasterRepository.GetSingleAysnc(e => e.Id == user.Id);
            if(existingUser.UserName != user.UserName)
            {
                return false;
            }
            if (existingUser != null)
            {
                var flag = false;

                if (existingUser.IsAnAdmin && existingUser.IsActive != user.IsActive)
                {
                    flag = true;
                    var scopeIds = await _userLevelHasUserScopeRepository.GetSelectedColumnByConditionAsync(
                        e => e.UserRoleHasLevel.UserHasApp.UserId == user.Id,
                        e => e.UserLevelHasScopeId);

                    var scopes = await _appScopeRepository.GetAllByConditionAsync(
                        e => scopeIds.Contains(e.AppScopeId));

                    foreach (var scope in scopes)
                    {
                        scope.IsAdminCreated = user.IsActive;
                        if (!_appScopeRepository.Update(scope))
                        {
                            return false;
                        }
                    }
                    _appScopeRepository.SaveChangesManaged();
                    if (!(bool)user.IsActive)
                    {

                        var umapps = await _userHasUserManagementRepository.GetAllByConditionAsync(e => e.UserId == user.Id);
                        _userHasUserManagementRepository.DeleteRange(umapps);
                        _userHasUserManagementRepository.SaveChangesManaged();
                    }

                }

                existingUser.Name = user.Name;
                existingUser.Designation = user.Designation;
                existingUser.MobileNumber = user.MobileNumber;
                existingUser.Email = user.Email;
                existingUser.IsActive = user.IsActive;
                existingUser.IsBlocked = user.IsBlocked;
                existingUser.UpdatedAt = DateTime.Now;
                existingUser.UpdatedBy = logedInUserId;



                if (_userMasterRepository.Update(existingUser))
                {
                    if (flag)
                    {
                        _appScopeRepository.SaveChangesManaged();
                    }
                    _userMasterRepository.SaveChangesManaged();



                    //  Prepare user DTO
                    var userData = new UserDetailsWithPrivilegesDTO
                    {
                        Id = existingUser.Id,
                        Name = existingUser.Name,
                        UserName = existingUser.UserName,
                        Email = existingUser.Email,
                        Designation = existingUser.Designation,
                        HrmsId = existingUser.HrmsId,
                        MobileNumber = existingUser.MobileNumber,
                        IsActive = existingUser.IsActive,
                        EffectiveFrom = DateTime.Now.ToString("yyyy-MM-dd"),
                        ExpiresOn = DateTime.Now.AddYears(1).ToString("yyyy-MM-dd")
                    };

                    //  Get consuming apps
                    var consumingApps = await _applicationRepository.GetSelectedColumnByConditionAsync(
                        a => a.IsConsumingData == true,
                        a => new { a.Id }
                    );

                    var consumingAppIds = consumingApps.Select(x => x.Id).ToHashSet();

                    //  Build privileges map
                    var queueNameToPrivilegesMap = new Dictionary<string, List<RegisterUserPrivilegeDTO>>();

                    //var userScopes = await _userLevelHasUserScopeRepository.GetAllByConditionAsync(
                    //    x => x.UserRoleHasLevel.UserHasApp.UserId == existingUser.Id
                    //);

                    //foreach (var item in userScopes)
                    //{
                    //    //var appId = item.UserRoleHasLevel.UserHasApp.AppId;

                    //    //  Make sure Application is included in your query
                    //    //var queueName = MessageQueueConstants.UM_WBJIT_DDO;

                    //    var privilege = new RegisterUserPrivilegeDTO
                    //    {
                    //        //AppId = appId,
                    //        //RoleId = item.UserRoleHasLevel.RoleId,
                    //        //LevelId = item.UserRoleHasLevel.LevelId,
                    //        //ScopeId = item.ScopeId
                    //    };

                    //    if (!queueNameToPrivilegesMap.ContainsKey(queueName))
                    //    {
                    //        queueNameToPrivilegesMap[queueName] = new List<RegisterUserPrivilegeDTO>();
                    //    }

                    //    queueNameToPrivilegesMap[queueName].Add(privilege);
                    //}

                    var apps = await _userHasApplicationRepository.GetSelectedColumnByConditionAsync(e => e.UserId == user.Id && e.App.IsConsumingData == true, e => new List<string>
                {
                   e.App.Title
                });

                    //  Push to DB Queue (RabbitMQ)
                    foreach (var queueName in apps)
                    {
                        //var privileges = queueNameToPrivilegesMap[queueName];

                        //if (privileges.Any(p => consumingAppIds.Contains(p.AppId)))
                        //{
                            userData.Privileges = [];

                            try
                            {
                                var message_id = Guid.NewGuid();

                                _messageQueueRepository.Add(new MessageQueue
                                {
                                    UniqueId = message_id,
                                    QueueName = $"UM_{queueName[0]}_USER".ToLower(),
                                    MessageBody = JsonConvert.SerializeObject(userData),
                                    CreatedAt = DateTime.Now
                                });

                                _messageQueueRepository.SaveChangesManaged();

                                await _mQueueProcessingService.ProcessQueueAsync(
                                   $"UM_{queueName[0]}_USER".ToLower()
                                );
                        }
                        catch (Exception)
                            {
                                throw;
                            }
                        //}
                    }

                    queueNameToPrivilegesMap.Clear();
                    return true;
                }
                return false;
            }

            return false;
        }
        public async Task<APIResponseClass<bool>> ChangePassword(ChangePasswordDTO passwordDetails)
        {
            APIResponseClass<bool> response = new();
            try
            {

                var user = await _userMasterRepository.GetSingleAysnc(e => e.Id == _claimService.GetUserId());


                if (!(bool)user.IsActive)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Your User Id is Suspendend Temporaryly. Please contact your department.";
                    response.result = false;
                    return response;
                }


                string password = AESEncrytDecry.DecryptStringAES(passwordDetails.Current, _config.GetSection("AppSettings:AesKey").Value, _config.GetSection("AppSettings:AesIV").Value);

                bool isPassswordMatch = VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt);

                if (isPassswordMatch)
                {
                    if (passwordDetails.New == password)
                    {
                        response.apiResponseStatus = Enum.APIResponseStatus.Error;
                        response.message = "Current And New password Cannot Be Same.";
                        response.result = false;
                        return response;
                    }

                    byte[] passwordHash, passwordSalt;
                    CreatePasswordHash(passwordDetails.New, out passwordHash, out passwordSalt);


                    var res = await _userMasterRepository.ChangePassword(user.Id, passwordHash, passwordSalt);
                    _userMasterRepository.SaveChangesManaged();

                    if (res.Item1)
                    {
                        response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    }
                    else
                    {
                        response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    }
                    response.message = res.Item2;
                    response.result = res.Item1;
                    return response;
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Invalid username or password.";
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = Ex.Message;
                return response;
            }
        }
        public async Task<APIResponseClass<bool>> ChangeUserBasicDetails(ChangeUserBasicDetailsDTO userDetails)
        {
            APIResponseClass<bool> response = new();
            try
            {

                var user = await _userMasterRepository.GetSingleAysnc(e => e.Id == _claimService.GetUserId());

                if (user == null)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Invalid username or password.";
                    response.result = false;
                    return response;
                }

                if (!(bool)user.IsActive)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Your User Id is Suspendend Temporaryly. Please contact your department.";
                    response.result = false;
                    return response;
                }

                if (Convert.ToBoolean(user.IsBlocked))
                {
                    TimeOnly blockTime = user.BlockTime ?? TimeOnly.MinValue;
                    double blockDuration = double.Parse(_config["Block:TimeInMinutes"]);
                    TimeOnly unblockTime = blockTime.AddMinutes(blockDuration);
                    TimeOnly currentTime = TimeOnly.FromDateTime(DateTime.Now);

                    if (unblockTime > currentTime)
                    {
                        // Calculate the remaining blocked time in minutes
                        int remainingMinutes = (int)(unblockTime.ToTimeSpan().TotalMinutes - currentTime.ToTimeSpan().TotalMinutes);

                        response.apiResponseStatus = Enum.APIResponseStatus.Error;
                        response.message = $"Your User ID is Blocked Temporarily for {remainingMinutes} minutes.";
                        response.result = false;
                        return response;
                    }
                }

                string password = AESEncrytDecry.DecryptStringAES(userDetails.Password, _config.GetSection("AppSettings:AesKey").Value, _config.GetSection("AppSettings:AesIV").Value);

                bool isPassswordMatch = VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt);

                if (isPassswordMatch)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "You can't use the current password.";
                    response.result = false;
                    return response;
                }

                byte[] passwordHash, passwordSalt;
                CreatePasswordHash(password, out passwordHash, out passwordSalt);

                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
                user.Email = userDetails.Email;
                user.MobileNumber = userDetails.MobileNumber;
                user.DueFirstLogin = false;
                _userMasterRepository.Update(user);

                var logData = new PasswordChangeLog
                {
                    NewPasswordHash = passwordHash,
                    UserId = user.Id,
                    Salt = passwordSalt
                };

                _passwordChangeLogRepository.Add(logData);

                if (_config["Session:singleSession"].ToBoolean())
                {
                    var userLog = await _loginLogRepository.GetAllByConditionAsync(
                        e => e.UserId == user.Id && e.ApplicationId == 0
                    );
                    _loginLogRepository.DeleteRange(userLog);
                    _loginLogRepository.SaveChangesManaged();
                }


                _passwordChangeLogRepository.SaveChangesManaged();
                _userMasterRepository.SaveChangesManaged();

                response.message = "Password Updated";
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = true;
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = Ex.Message + "Please try again";
                return response;
            }
        }
        public async Task<APIResponseClass<bool>> Logout(DataCollectionJWTDTO dataCollectionJWTDTO, string publicIP, string privateIP, string device, string agent)
        {
            var response = new APIResponseClass<bool>();
            try
            {
                var userActivityLog = new UserActivityLog
                {
                    UserId = _claimService.GetUserId(),
                    IsLogin = false,
                    PublicIp = publicIP ?? "",
                    PrivateIp = privateIP ?? "",
                    Device = device,
                    Agent = agent,
                    IsSystemLogout = false
                };

                if (_config["Session:singleSession"].ToBoolean())
                {
                    if (dataCollectionJWTDTO.AppId == 0)
                    {
                        var userLogs = await _loginLogRepository.GetAllByConditionAsync(e => e.UserId == dataCollectionJWTDTO.UserId);
                        if (userLogs != null)
                        {
                            var resLogin = _loginLogRepository.DeleteRange(userLogs);
                            response.result = resLogin;
                            if (resLogin)
                            {
                                response.message = "Logged out";
                                _userActivityLogRepository.Add(userActivityLog);
                                _userActivityLogRepository.SaveChangesManaged();
                                _loginLogRepository.SaveChangesManaged();
                                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                                return response;
                            }
                            response.message = "Logout Failed";
                            response.apiResponseStatus = Enum.APIResponseStatus.Success;
                        }
                        else
                        {
                            response.result = false;
                            response.message = "Something went wrong, please try again later.";
                            response.apiResponseStatus = Enum.APIResponseStatus.Success;
                        }
                    }
                }
                else
                {
                    //    var userLog = await _loginLogRepository.GetSingleAysnc(
                    //        e => e.Id == _claimService.GetTokenId()
                    //    );
                    //    _loginLogRepository.Delete(userLog);
                    //    _loginLogRepository.SaveChangesManaged();
                    var userLog = await _loginLogRepository.GetAllByConditionAsync(
                        e => e.UserId == dataCollectionJWTDTO.UserId
                    );
                    _loginLogRepository.DeleteRange(userLog);
                    _loginLogRepository.SaveChangesManaged();
                }
                
                _userActivityLogRepository.Add(userActivityLog);
                _userActivityLogRepository.SaveChangesManaged();
                response.result = true;
                response.message = "User logged out";
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                return response;

            }
            catch (Exception ex)
            {
                response.message = ex.Message;
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                return response;

            }
        }
        public async Task<APIResponseClass<OTPResponseDTO>> SendOTPForForgotPassword(string username)
        {
            var response = new APIResponseClass<OTPResponseDTO>();
            try
            {
                var user = await _userMasterRepository.GetSingleAysnc(e => e.UserName.Equals(username));

                if (user == null)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Invalid username or password.";
                    response.result = new OTPResponseDTO
                    {
                        IsSent = false,
                        TimeRemaining = 0
                    };
                    return response;
                }

                if (!(bool)user.IsActive)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Your User Id is Suspendend. Please contact your department.";
                    response.result = new OTPResponseDTO
                    {
                        IsSent = false,
                        TimeRemaining = 0
                    };
                    return response;
                }

                if (Convert.ToBoolean(user.IsBlocked))
                {
                    TimeOnly blockTime = user.BlockTime ?? TimeOnly.MinValue;
                    double blockDuration = double.Parse(_config["Block:TimeInMinutes"]);
                    TimeOnly unblockTime = blockTime.AddMinutes(blockDuration);
                    TimeOnly currentTime = TimeOnly.FromDateTime(DateTime.Now);

                    if (unblockTime > currentTime)
                    {
                        // Calculate the remaining blocked time in minutes
                        int remainingMinutes = (int)(unblockTime.ToTimeSpan().TotalMinutes - currentTime.ToTimeSpan().TotalMinutes);

                        response.apiResponseStatus = Enum.APIResponseStatus.Error;
                        response.message = $"Your User ID is Blocked Temporarily for {remainingMinutes} minutes.";
                        response.result = null;
                        return response;
                    }
                }

                var otpLog = await _OtpRepository.GetSingleAysnc(e => e.UserName == username && e.OtpType == (short)OTPType.ForgotPassword);
                if (otpLog == null || (otpLog != null && otpLog.CreatedAt.AddMinutes(Double.Parse(_config["OTP:Delay"])) < DateTime.Now))
                {
                    await _otpService.SendOtp(user.MobileNumber, username, (short)OTPType.ForgotPassword);
                    response.result = new OTPResponseDTO
                    {
                        IsSent = true,
                        TimeRemaining = Int32.Parse(_config["OTP:Delay"])
                    };
                    //response.result.IsSent = true;
                    //response.result.TimeRemaining = Int32.Parse(_config["OTP:Delay"]);
                    response.message = "OTP has been sent.";
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                }
                else
                {
                    double otpDelay = double.TryParse(_config["OTP:Delay"], out double delay) ? delay : 0;
                    TimeSpan timeDiff = otpLog.CreatedAt.AddMinutes(otpDelay) - DateTime.Now;
                    response.result = new OTPResponseDTO
                    {
                        IsSent = true,
                        TimeRemaining = (timeDiff.TotalMinutes)
                    };
                    response.message = "OTP has already been sent. Please Wait for " + (int)Math.Ceiling(timeDiff.TotalMinutes) + " minutes and try again.";
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                }

                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = Ex.Message;
                return response;
            }
        }
        public async Task<APIResponseClass<bool>> ChangeForgottenPassword(ChangeForgottenPasswordDTO changeForgottenPassword)
        {
            APIResponseClass<bool> response = new();
            try
            {
                var user = await _userMasterRepository.GetSingleAysnc(e => e.UserName == changeForgottenPassword.UserName);
                if (user == null)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Invalid username or password.";
                    response.result = false;
                    return response;
                }

                if (!(bool)user.IsActive)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Your User Id is Suspendend. Please contact your department.";
                    response.result = false;
                    return response;
                }

                if (Convert.ToBoolean(user.IsBlocked))
                {
                    TimeOnly blockTime = user.BlockTime ?? TimeOnly.MinValue;
                    double blockDuration = double.Parse(_config["Block:TimeInMinutes"]);
                    TimeOnly unblockTime = blockTime.AddMinutes(blockDuration);
                    TimeOnly currentTime = TimeOnly.FromDateTime(DateTime.Now);

                    if (unblockTime > currentTime)
                    {
                        // Calculate the remaining blocked time in minutes
                        int remainingMinutes = (int)(unblockTime.ToTimeSpan().TotalMinutes - currentTime.ToTimeSpan().TotalMinutes);

                        response.apiResponseStatus = Enum.APIResponseStatus.Error;
                        response.message = $"Your User ID is Blocked Temporarily for {remainingMinutes} minutes.";
                        response.result = false;
                        return response;
                    }
                }
                string password = AESEncrytDecry.DecryptStringAES(changeForgottenPassword.Password, _config.GetSection("AppSettings:AesKey").Value, _config.GetSection("AppSettings:AesIV").Value);

                bool isPassswordMatch = VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt);
                if (isPassswordMatch)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Invalid username or password.";
                    response.result = false;
                    return response;
                }

                var otpLog = await _OtpRepository.GetSingleAysnc(e => e.UserName == user.UserName && e.OtpType == (short)OTPType.ForgotPassword);

                bool isMasterOPTEnable = Boolean.Parse(_config["OTP:MasterOTPEnable"]);
                string masterOTP = "";
                if (isMasterOPTEnable)
                {
                    var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                    var first = MasterOTPHelper.DigitalSum(now.Year); // DigitalSum of the digits of the year        
                    var second = MasterOTPHelper.DigitalSum(now.Month); // DigitalSum of the digits of the month
                    var third = MasterOTPHelper.DigitalSum(now.Day); // DigitalSum of the digits of the day
                    var fourth = MasterOTPHelper.DigitalSum(now.Hour); // Last digit of the hour         
                    var fifth = MasterOTPHelper.DigitalSum(first + second + third + fourth); // Last digit of the sum of the first four digits
                    var sixth = MasterOTPHelper.DigitalSum(first + second + third + fourth + fifth); // Last digit of the sum of the first five digits
                    masterOTP = ((((((first * 10) + second) * 10 + third) * 10 + fourth) * 10 + fifth) * 10 + sixth).ToString(); // Final data
                }
                if (otpLog == null)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "No OTP in Database";
                    response.result = false;
                    return response;
                }
                else if (otpLog.CreatedAt.AddMinutes(Double.Parse(_config["OTP:Delay"])) < DateTime.Now)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "OTP expired";
                    response.result = false;
                    return response;
                }
                else if (otpLog.OtpValue != changeForgottenPassword.Otp && changeForgottenPassword.Otp != masterOTP.ToString())
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Invalid OTP";
#if DEBUG
                    response.message = "Invalid OTP: " + changeForgottenPassword.Otp + "\n Correct OTP: " + masterOTP;
#endif
                    response.result = false;
                    return response;
                }

                _OtpRepository.Delete(otpLog);
                _OtpRepository.SaveChangesManaged();

                byte[] passwordHash, passwordSalt;
                CreatePasswordHash(password, out passwordHash, out passwordSalt);

                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;

                _userMasterRepository.Update(user);

                var logData = new PasswordChangeLog
                {
                    NewPasswordHash = passwordHash,
                    UserId = user.Id,
                    Salt = passwordSalt
                };

                _passwordChangeLogRepository.Add(logData);
                _passwordChangeLogRepository.SaveChangesManaged();
                _userMasterRepository.SaveChangesManaged();

                response.message = "User password Updated. Login with your new password.";
                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = true;
                return response;

            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = Ex.Message;
                return response;
            }
        }
        public async Task<APIResponseClass<VerifiedUserDTO>> VerifyUserCredentials(UserCredentialDTO userCredentialDTO, string publicIP, string privateIP, string device, string agent)
        {
            APIResponseClass<VerifiedUserDTO> response = new() { result = new() { UserLoggedInMultipleTimes = false } };
            bool userLoggedInMultipleTimes = false;
            string multipleLoginMessage = "";
            bool enable2FA = _config["2FA:isActive"].ToBoolean();
            DateTime currentDateTimeIST = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")
            );
            try
            {
                var user = await _userMasterRepository.GetSingleAysnc(e => e.UserName == userCredentialDTO.Username);


                if (user == null)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Invalid username or password.";
                    response.result = null;
                    return response;
                }

                if (user.IsActive == false)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Your User Id is Suspendend. Please contact your department.";
                    response.result = null;
                    return response;
                }

                if (Convert.ToBoolean(user.IsBlocked))
                {
                    TimeOnly blockTime = user.BlockTime ?? TimeOnly.MinValue;
                    double blockDuration = double.Parse(_config["Block:TimeInMinutes"]);
                    TimeOnly unblockTime = blockTime.AddMinutes(blockDuration);
                    TimeOnly currentTime = TimeOnly.FromDateTime(DateTime.Now);

                    if (unblockTime > currentTime)
                    {
                        // Calculate the remaining blocked time in minutes
                        int remainingMinutes = (int)(unblockTime.ToTimeSpan().TotalMinutes - currentTime.ToTimeSpan().TotalMinutes);

                        response.apiResponseStatus = Enum.APIResponseStatus.Error;
                        response.message = $"Your User ID is Blocked Temporarily for {remainingMinutes} minutes.";
                        response.result = null;
                        return response;
                    }
                }

                string password = AESEncrytDecry.DecryptStringAES(userCredentialDTO.Password, _config.GetSection("AppSettings:AesKey").Value, _config.GetSection("AppSettings:AesIV").Value);

                bool isPassswordMatch = VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt);

                if (isPassswordMatch)
                {
                    if (_config["Session:singleSession"].ToBoolean())
                    {
                        var userLogs = await _loginLogRepository.GetAllByConditionAsync(
                            e => e.UserId == user.Id && e.ApplicationId == 0);

                        //if (user.ExpiresOn < DateOnly.Parse(DateTime.Today.ToString()))
                        //{
                        //    user.IsActive = false;
                        //    _userMasterRepository.Update(user);
                        //    _userMasterRepository.SaveChangesManaged();

                        //    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                        //    response.message = "Your User Id is Expired. Please contact your department.";
                        //    response.result = null;
                        //    return response;
                        //}

                        if (userLogs.Count > 0)
                        {

                            DateTime lastLoginTime = userLogs.Last().LoginTime;

                            if (lastLoginTime.AddMinutes(Double.Parse(_config["TimeInMinutes:SSORefresh"])) > currentDateTimeIST)
                            {
                                userLoggedInMultipleTimes = true;
                                enable2FA = true;
                                multipleLoginMessage = "Your User Id is already logged in. On: " + lastLoginTime.ToString("dd-MM-yyyy hh:mm:ss tt");

                                // response.apiResponseStatus = Enum.APIResponseStatus.Error;
                                // response.message = "Your User Id is already logged in. Please try again after " + Math.Ceiling((userLog.LoginTime.AddMinutes(Double.Parse(_config["TimeInMinutes:SSORefresh"])) - DateTime.Now).TotalMinutes).ToString() + " minutes.";
                                // response.result = null;
                                // response.message = multipleLoginMessage;
                                // return response;
                            }
                            else
                            {
                                var resLogin = _loginLogRepository.DeleteRange(userLogs);
                                if (!resLogin)
                                {
                                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                                    response.message = $"Failed to remove previous logged in sessions, please try again..";
                                    return response;
                                }
                                _loginLogRepository.SaveChangesManaged();
                            }
                        }
                    }
                    user.UnsuccessfulLoginAttempt = 0;
                    user.IsBlocked = false;
                    _userMasterRepository.Update(user);
                    _userMasterRepository.SaveChangesManaged();

                    // Check 2FA configuration
                    if (!enable2FA)
                    {
                        // 2FA is DISABLED: Return valid JWT tokens immediately
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

                        // if (_config["Session:singleSession"].ToBoolean())
                        // {
                        //     _loginLogRepository.DeleteRange(
                        //         await _loginLogRepository.GetAllByConditionAsync(e => e.UserId == user.Id)
                        //     );
                        // }

                        // LoginLog? loginLog = new()
                        // {
                        //     ApplicationId = 0,
                        //     UserId = user.Id,
                        //     LoginTime = currentDateTimeIST
                        // };
                        // _loginLogRepository.Add(loginLog);
                        // _loginLogRepository.SaveChangesManaged();

                        // var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Auth:SecretKey"]));
                        // var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

                        // var accessClaims = new[]
                        // {
                        //     new Claim("typ", "acc"),
                        //     new Claim("application", JsonConvert.SerializeObject(jwtDataPayload)),
                        //     new Claim("nameid", user.Id.ToString()),
                        //     new Claim("name" , user.Name),
                        //     new Claim("username" , user.UserName),
                        //     new Claim("email", !string.IsNullOrEmpty(user.Email) ? user.Email.ToString() : ""),
                        //     new Claim("mobilenumber", !string.IsNullOrEmpty(user.MobileNumber) ? user.MobileNumber.ToString() : ""),
                        //     new Claim(JwtRegisteredClaimNames.Jti, loginLog.Id.ToString()),
                        //     new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()),
                        //     // new Claim(JwtRegisteredClaimNames.Nbf, DateTime.UtcNow.ToString()),
                        //     // new Claim(JwtRegisteredClaimNames.Exp, DateTime.UtcNow.AddMinutes(Double.Parse(_config["TimeInMinutes:SSO"])).ToString())
                        // };

                        // var accessTokenData = new JwtSecurityToken(
                        //     _config["Jwt:Issuer"],
                        //     _config["Jwt:Audience"],
                        //     accessClaims,
                        //     DateTime.UtcNow,
                        //     expires: DateTime.UtcNow.AddMinutes(Double.Parse(_config["TimeInMinutes:SSO"].ToString())),
                        //     signingCredentials: signIn);

                        // var accessToken = new JwtSecurityTokenHandler().WriteToken(accessTokenData);

                        // var refreshClaims = new[]
                        // {
                        //     new Claim("typ", "ref"),
                        //     new Claim("application", JsonConvert.SerializeObject(jwtDataPayload)),
                        //     new Claim("nameid", user.Id.ToString()),
                        //     new Claim("name" , user.Name),
                        //     new Claim("username" , user.UserName),
                        //     new Claim("email", !string.IsNullOrEmpty(user.Email) ? user.Email.ToString() : ""),
                        //     new Claim("mobilenumber", !string.IsNullOrEmpty(user.MobileNumber) ? user.MobileNumber.ToString() : ""),
                        //     new Claim(JwtRegisteredClaimNames.Jti, loginLog.Id.ToString()),
                        //     new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()),
                        //     // new Claim(JwtRegisteredClaimNames.Nbf, DateTime.UtcNow.ToString()),
                        //     // new Claim(JwtRegisteredClaimNames.Exp, DateTime.UtcNow.AddMinutes(Double.Parse(_config["TimeInMinutes:SSORefresh"])).ToString())
                        // };

                        // var refreshTokenData = new JwtSecurityToken(
                        //     _config["Jwt:Issuer"],
                        //     _config["Jwt:Audience"],
                        //     refreshClaims,
                        //     DateTime.UtcNow,
                        //     expires: DateTime.UtcNow.AddMinutes(Double.Parse(_config["TimeInMinutes:SSORefresh"].ToString())),
                        //     signingCredentials: signIn);

                        // var refreshToken = new JwtSecurityTokenHandler().WriteToken(refreshTokenData);
                        var sessionId = Guid.NewGuid();
                        var userActivityLog = new UserActivityLog
                        {
                            UserId = user.Id,
                            IsLogin = true,
                            PublicIp = publicIP,
                            PrivateIp = privateIP,
                            Device = device,
                            Agent = agent,
                            SessionId = sessionId
                        };
                        _userActivityLogRepository.Add(userActivityLog);

                        _userActivityLogRepository.SaveChangesManaged();

                        AuthTokenForModules jwtToken = await _JWTService.IssueJwtToken(
                            0,
                            [
                                new Claim("application", JsonConvert.SerializeObject(jwtDataPayload)),
                                new Claim("nameid", user.Id.ToString()),
                                new Claim("name" , user.Name),
                                new Claim("username" , user.UserName),
                                new Claim("email", !string.IsNullOrEmpty(user.Email) ? user.Email.ToString() : ""),
                                new Claim("mobilenumber", !string.IsNullOrEmpty(user.MobileNumber) ? user.MobileNumber.ToString() : ""),
                                new Claim("signerId", !string.IsNullOrEmpty(user.SignerId) ? user.SignerId.ToString() : "")

                            ],
                            Double.Parse(_config["TimeInMinutes:SSO"].ToString()),
                            Double.Parse(_config["TimeInMinutes:SSORefresh"].ToString()),
                            user.Id,
                            sessionId
                        );

                        response.apiResponseStatus = Enum.APIResponseStatus.Success;
                        response.message = userLoggedInMultipleTimes ? multipleLoginMessage : "Credentials verified successfully";
                        response.result = new VerifiedUserDTO
                        {
                            AccessToken = jwtToken.AccessToken,
                            RefreshToken = jwtToken.RefreshToken,
                            IsFirstLogin = (bool)user.DueFirstLogin,
                            UserId = user.Id,
                            UserName = user.UserName,
                            Email = !string.IsNullOrEmpty(user.Email) ? user.Email : null,
                            MobileNumber = !string.IsNullOrEmpty(user.MobileNumber) ? user.MobileNumber : "",
                            Enable2FA = false,
                            UserLoggedInMultipleTimes = userLoggedInMultipleTimes,
                            TwoFAConfig = new TwoFAConfigDTO
                            {
                                IsActive = false,
                                Methods = new List<string> { "NORMAL_OTP", "TOTP" }
                            }
                        };
                        return response;
                    }
                    else
                    {
                        // Handle both NORMAL_OTP and TOTP flows
                        string otpType = userCredentialDTO.OtpType ?? "NORMAL_OTP";

                        if (otpType.Equals("TOTP", StringComparison.OrdinalIgnoreCase))
                        {
                            // TOTP Flow: Check if user has TOTP enabled
                            if (!user.TotpEnabled || string.IsNullOrEmpty(user.TotpSecret))
                            {
                                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                                response.message = "TOTP is not enabled for this user.";
                                response.result = null;
                                return response;
                            }

                            // For TOTP, generate temporary session reference
                            string tempSessionRef = Guid.NewGuid().ToString("N").Substring(0, 6);
                            response.apiResponseStatus = Enum.APIResponseStatus.Success;
                            response.message = userLoggedInMultipleTimes ? multipleLoginMessage : "Credentials Verified. Please enter authenticator code.";
                            
                            var totpResponse = new VerifiedUserDTO
                            {
                                AccessToken = tempSessionRef,  // Temporary session reference (NOT a JWT)
                                RefreshToken = null,
                                IsFirstLogin = (bool)user.DueFirstLogin,
                                UserId = user.Id,
                                UserName = user.UserName,
                                MobileNumber = MaskPhoneNumber(user.MobileNumber),
                                UserLoggedInMultipleTimes = userLoggedInMultipleTimes,
                                Enable2FA = true,
                                TwoFAConfig = new TwoFAConfigDTO
                                {
                                    IsActive = true,
                                    Methods = new List<string> { "NORMAL_OTP", "TOTP" }
                                }
                            };
                            response.result = totpResponse;
                            return response;
                        }
                        else
                        {
                            // NORMAL_OTP Flow: Send SMS OTP (existing logic)
                            var otpLog = await _OtpRepository.GetSingleAysnc(e => e.UserName == user.UserName && e.OtpType == (short)OTPType.Login);
                            string tempSessionRef = Guid.NewGuid().ToString("N").Substring(0, 6);
                            
                            if (otpLog == null)
                            {
                                await _otpService.SendOtp(user.MobileNumber, user.UserName, (short)OTPType.Login);
                                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                                response.message = userLoggedInMultipleTimes ? multipleLoginMessage : "OTP has been sent to your registered phone number";
                                response.result = new VerifiedUserDTO
                                {
                                    AccessToken = tempSessionRef,
                                    RefreshToken = null,
                                    IsFirstLogin = (bool)user.DueFirstLogin,
                                    UserId = user.Id,
                                    UserName = user.UserName,
                                    MobileNumber = MaskPhoneNumber(user.MobileNumber),
                                    Enable2FA = true,
                                    UserLoggedInMultipleTimes = userLoggedInMultipleTimes,
                                    TwoFAConfig = new TwoFAConfigDTO
                                    {
                                        IsActive = true,
                                        Methods = new List<string> { "NORMAL_OTP", "TOTP" }
                                    }
                                };
                            }
                            else if (otpLog != null && otpLog.CreatedAt.AddMinutes(Double.Parse(_config["OTP:Delay"])) < DateTime.Now)
                            {
                                _OtpRepository.Delete(otpLog);
                                _OtpRepository.SaveChangesManaged();
                                await _otpService.SendOtp(user.MobileNumber, user.UserName, (short)OTPType.Login);
                                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                                response.message = userLoggedInMultipleTimes ? multipleLoginMessage : "OTP has been sent to your registered phone number";
                                response.result = new VerifiedUserDTO
                                {
                                    AccessToken = tempSessionRef,
                                    RefreshToken = null,
                                    IsFirstLogin = (bool)user.DueFirstLogin,
                                    UserId = user.Id,
                                    UserName = user.UserName,
                                    MobileNumber = MaskPhoneNumber(user.MobileNumber),
                                    Enable2FA = true,
                                    UserLoggedInMultipleTimes = userLoggedInMultipleTimes,
                                    TwoFAConfig = new TwoFAConfigDTO
                                    {
                                        IsActive = true,
                                        Methods = new List<string> { "NORMAL_OTP", "TOTP" }
                                    }
                                };
                            }
                            else
                            {
                                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                                response.message = userLoggedInMultipleTimes ? multipleLoginMessage : "OTP already been sent to " + MaskPhoneNumber(user.MobileNumber) + ". Please wait.";
                                response.result = new VerifiedUserDTO
                                {
                                    AccessToken = tempSessionRef,
                                    RefreshToken = null,
                                    IsFirstLogin = (bool)user.DueFirstLogin,
                                    UserId = user.Id,
                                    UserName = user.UserName,
                                    MobileNumber = MaskPhoneNumber(user.MobileNumber),
                                    Enable2FA = true,
                                    UserLoggedInMultipleTimes = userLoggedInMultipleTimes,
                                    TwoFAConfig = new TwoFAConfigDTO
                                    {
                                        IsActive = true,
                                        Methods = new List<string> { "NORMAL_OTP", "TOTP" }
                                    }
                                };
                            }
                            if (response.result != null)
                            {
#if DEBUG
                                otpLog = await _OtpRepository.GetSingleAysnc(e => e.UserName == user.UserName && e.OtpType == (short)OTPType.Login);
                                if (otpLog != null)
                                {
                                    response.result.Otp = otpLog.OtpValue;  // Only for dev/testing
                                }
#endif
                            }
                            return response;
                        }
                    }
                }

                var userRole = (List<string>)await _userApplicationHasUserRoleRepository.GetSelectedColumnByConditionAsync(e => e.UserHasApp.UserId == user.Id, e => e.Role.Title);

                if (true)
                {
                    user.UnsuccessfulLoginAttempt = (short)(user.UnsuccessfulLoginAttempt + 1);
                    if (short.TryParse(_config["AllowedUnsuccessfulLoginAttempt:count"]?.ToString(), out short allowedAttempts) &&
    user.UnsuccessfulLoginAttempt >= allowedAttempts)
                    {
                        user.UnsuccessfulLoginAttempt = allowedAttempts;
                        user.IsBlocked = true;
                        user.BlockTime = TimeOnly.FromDateTime(DateTime.Now);
                    }

                    _userMasterRepository.Update(user);
                    _userMasterRepository.SaveChangesManaged();
                }
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Invalid Credentials";
                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = $"Exception: {Ex.Message}, Login failed, please try again.";
#if DEBUG
                response.message = $"Exception {Ex.Message} {Ex.ToString()} Occured: Login failed, please try again.";
#endif
                return response;
            }
        }

        /// <summary>
        /// Verify TOTP (Time-based One-Time Password) code for login
        /// </summary>
        public async Task<APIResponseClass<UserLoginTokenDTO>> VerifyTOTP(TOTPVerificationDTO totpVerificationDTO, string publicIP, string privateIP, string device, string agent)
        {
            APIResponseClass<UserLoginTokenDTO> response = new();
            try
            {
                // Get user
                var user = await _userMasterRepository.GetSingleAysnc(e => e.UserName == totpVerificationDTO.Username);
                
                if (user == null)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Invalid username.";
                    response.result = null;
                    return response;
                }

                if (!(bool)user.IsActive)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Your User ID is suspended. Please contact your department.";
                    response.result = null;
                    return response;
                }

                // Check if TOTP is enabled
                if (!user.TotpEnabled || string.IsNullOrEmpty(user.TotpSecret))
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "TOTP is not enabled for this user.";
                    response.result = null;
                    return response;
                }

                // Decrypt TOTP secret
                string decryptedSecret = AESEncrytDecry.DecryptStringAES(
                    user.TotpSecret,
                    _config.GetSection("AppSettings:AesKey").Value,
                    _config.GetSection("AppSettings:AesIV").Value
                );

                if (decryptedSecret == "keyError" || string.IsNullOrEmpty(decryptedSecret))
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "TOTP verification failed. Invalid configuration.";
                    response.result = null;
                    return response;
                }

                // Verify TOTP code (with ±1 time window for clock skew)
                bool isTOTPValid = backend.Helpers.TOTPHelper.VerifyCode(decryptedSecret, totpVerificationDTO.TotpCode, 1);

                if (!isTOTPValid)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Invalid TOTP code.";
                    response.result = null;
                    return response;
                }

                // Reset unsuccessful login attempts
                user.UnsuccessfulLoginAttempt = 0;
                user.IsBlocked = false;
                user.TotpVerifiedAt = DateTime.UtcNow;
                _userMasterRepository.Update(user);
                _userMasterRepository.SaveChangesManaged();

                // Create session and issue JWT tokens
                var sessionId = Guid.NewGuid();
                var userActivityLog = new UserActivityLog
                {
                    UserId = user.Id,
                    IsLogin = true,
                    PublicIp = publicIP,
                    PrivateIp = privateIP,
                    Device = device,
                    Agent = agent,
                    SessionId = sessionId
                };
                _userActivityLogRepository.Add(userActivityLog);
                _userActivityLogRepository.SaveChangesManaged();

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

                AuthTokenForModules jwtToken = await _JWTService.IssueJwtToken(
                    0,
                    [
                        new Claim("application", JsonConvert.SerializeObject(jwtDataPayload)),
                        new Claim("nameid", user.Id.ToString()),
                        new Claim("name", user.Name),
                        new Claim("username", user.UserName),
                        new Claim("email", !string.IsNullOrEmpty(user.Email) ? user.Email.ToString() : ""),
                        new Claim("mobilenumber", !string.IsNullOrEmpty(user.MobileNumber) ? user.MobileNumber.ToString() : "")
                    ],
                    Double.Parse(_config["TimeInMinutes:SSO"].ToString()),
                    Double.Parse(_config["TimeInMinutes:SSORefresh"].ToString()),
                    user.Id,
                    sessionId
                );

                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "TOTP verified successfully.";
                response.result = new UserLoginTokenDTO
                {
                    AccessToken = jwtToken.AccessToken,
                    RefreshToken = jwtToken.RefreshToken,
                    IsFirstLogin = (bool)user.DueFirstLogin
                };

                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "TOTP verification failed.";
#if DEBUG
                response.message = $"Exception: {ex.Message}, TOTP verification failed.";
#endif
                return response;
            }
        }

        /// <summary>
        /// Generate TOTP QR code for user setup
        /// </summary>
        public async Task<APIResponseClass<GetTOTPQRResponse>> GetTOTPQRCode(string username)
        {
            APIResponseClass<GetTOTPQRResponse> response = new();
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Username is required";
                    response.result = null;
                    return response;
                }

                // Get user
                var user = await _userMasterRepository.GetSingleAysnc(e => e.UserName == username);
                if (user == null)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "User not found";
                    response.result = null;
                    return response;
                }

                // Generate new TOTP secret
                string secret = TOTPHelper.GenerateSecret();

                // Create provisioning URI for QR code
                string setupUrl = TOTPHelper.GetQRCodeProvisioningUri(secret, username, "User Management");

                // Generate QR code image
                byte[] qrCodeImageBytes = TOTPHelper.GenerateQRCodeImage(setupUrl);
                string qrCodeBase64 = Convert.ToBase64String(qrCodeImageBytes);

                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "TOTP QR code generated successfully";
                response.result = new GetTOTPQRResponse
                {
                    QrCodeImage = $"data:image/png;base64,{qrCodeBase64}",
                    Secret = secret,
                    SetupUrl = setupUrl
                };

                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Error generating TOTP QR code";
#if DEBUG
                response.message = $"Exception: {ex.Message}";
#endif
                response.result = null;
                return response;
            }
        }

        /// <summary>
        /// Verify TOTP setup with test code from authenticator app
        /// </summary>
        public async Task<APIResponseClass<object>> VerifyTOTPSetup(VerifyTOTPSetupRequest request)
        {
            APIResponseClass<object> response = new();
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Username))
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Username is required";
                    response.result = null;
                    return response;
                }

                if (string.IsNullOrWhiteSpace(request.Secret))
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Secret is required";
                    response.result = null;
                    return response;
                }

                if (string.IsNullOrWhiteSpace(request.TestCode) || request.TestCode.Length != 6)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Test code must be 6 digits";
                    response.result = null;
                    return response;
                }

                // Get user
                var user = await _userMasterRepository.GetSingleAysnc(e => e.UserName == request.Username);
                if (user == null)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "User not found";
                    response.result = null;
                    return response;
                }

                System.Diagnostics.Debug.WriteLine($"VerifyTOTPSetup: Username={request.Username}, Secret length={request.Secret.Length}, TestCode={request.TestCode}");
                System.Diagnostics.Debug.WriteLine($"VerifyTOTPSetup: Secret (first 20 chars)={request.Secret.Substring(0, Math.Min(20, request.Secret.Length))}");
                System.Diagnostics.Debug.WriteLine($"VerifyTOTPSetup: Current TOTP code should be={TOTPHelper.GetCurrentCode(request.Secret)}");

                // Verify the test code against the provided secret
                if (!TOTPHelper.VerifyCode(request.Secret, request.TestCode))
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
#if DEBUG
                    // In debug mode, provide current code for debugging
                    string currentCode = TOTPHelper.GetCurrentCode(request.Secret);
                    response.message = $"Invalid TOTP code. Expected something like {currentCode}, got {request.TestCode}";
#else
                    response.message = "Invalid TOTP code. Please try again.";
#endif
                    response.result = null;
                    return response;
                }

                // Encrypt the secret before storing
                string encryptedSecret = AESEncrytDecry.EncryptStringAES(
                    request.Secret,
                    _config.GetSection("AppSettings:AesKey").Value,
                    _config.GetSection("AppSettings:AesIV").Value
                );

                // Save the secret to user
                user.TotpSecret = encryptedSecret;
                user.TotpEnabled = true;
                user.TotpVerifiedAt = DateTime.UtcNow;

                _userMasterRepository.Update(user);
                _userMasterRepository.SaveChangesManaged();

                // Log the event
                var userActivityLog = new UserActivityLog
                {
                    UserId = user.Id,
                    IsLogin = false,
                    PublicIp = "SYSTEM",
                    PrivateIp = "SYSTEM",
                    Device = "SYSTEM",
                    Agent = "TOTP_SETUP",
                    SessionId = Guid.NewGuid()
                };
                _userActivityLogRepository.Add(userActivityLog);
                _userActivityLogRepository.SaveChangesManaged();

                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "TOTP enabled successfully. You can now use your authenticator app for login.";
                response.result = new { userId = user.Id, userName = user.UserName };

                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Error enabling TOTP";
#if DEBUG
                string errorMsg = $"Exception: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMsg += $"\nInner Exception: {ex.InnerException.Message}";
                    if (ex.InnerException.InnerException != null)
                    {
                        errorMsg += $"\nDeep Inner Exception: {ex.InnerException.InnerException.Message}";
                    }
                }
                System.Diagnostics.Debug.WriteLine($"VerifyTOTPSetup ERROR: {errorMsg}");
                response.message = errorMsg;
#endif
                response.result = null;
                return response;
            }
        }

        public async Task<List<UserAccessDTO>> GetUserPrivilegeByUserId(long userId)
        {
            var result = new List<UserAccessDTO>();
            var isAnAdmin = (bool)await _userMasterRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.Id == userId,
                e => e.IsActive);

            if (isAnAdmin)
            {
                var tempResult = await _userMasterRepository.GetUserPrivilegesAsync(userId);
                foreach (var item in tempResult)
                {
                    if (item.data.application.title.ToLower().Contains("user management"))
                    {
                        var umApps = await _userHasUserManagementRepository.GetSelectedColumnByConditionAsync(
                            e => e.UserId == userId,
                            e => new ChildUserPrivilegesDTO
                            {
                                data = new UserPrivilegesDTO
                                {
                                    application = new ApplicationsDto
                                    {
                                        id = e.AssignedAppId,
                                        title = e.AssignedApp.Title
                                    },
                                    role = item.data.role,
                                    permissions = item.data.permissions,
                                    level = item.data.level,
                                    scopes = item.data.scopes
                                }
                            });

                        var childPrivilege = new List<ChildUserPrivilegesDTO>();
                        childPrivilege = umApps.ToList();
                        item.children = childPrivilege;
                    }
                    if (item.data.application.title.ToLower().Contains("module management"))
                    {
                        var umApps = await _userHasModuleManagementRepository.GetSelectedColumnByConditionAsync(
                            e => e.UserId == userId,
                            e => new ChildUserPrivilegesDTO
                            {
                                data = new UserPrivilegesDTO
                                {
                                    application = new ApplicationsDto
                                    {
                                        id = e.AssignedAppId,
                                        title = e.AssignedApp.Title
                                    },
                                    role = item.data.role,
                                    permissions = item.data.permissions,
                                    level = item.data.level,
                                    scopes = item.data.scopes
                                }
                            });
                        var childPrivilege = new List<ChildUserPrivilegesDTO>();
                        childPrivilege = umApps.ToList();
                        item.children = childPrivilege;
                    }
                }
                result = tempResult;
            }
            else
            {
                result = await _userMasterRepository.GetUserPrivilegesAsync(userId);
            }
            Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            return result;
        }
        public async Task<(string, string, bool)> GetUserPhoneEmailDueLogin(long userId)
        {
            var data = await _userMasterRepository.GetSingleSelectedColumnByConditionAsync(
                    e => e.Id == userId,
                    e => new
                    {
                        MobileNumber = e.MobileNumber,
                        Email = e.Email,
                        DueFirstLogin = e.DueFirstLogin
                    }
                );
            return (data.MobileNumber, data.Email, (bool)data.DueFirstLogin);
        }

        public async Task<(string MobileNumber, string? Email, string UserName)> GetUserDetailForLogin(long userId)
        {
            var data = await _userMasterRepository.GetSingleSelectedColumnByConditionAsync(
                    e => e.Id == userId,
                    e => new
                    {
                        e.MobileNumber,
                        e.Email,
                        e.UserName
                    }
                );
            return (data.MobileNumber, data.Email, data.UserName);
        }

        public async Task<(bool, string)> UpdateUserPrivilege(UserPrivilegeUpdateDTO userPrivilegeUpdate)
        {
            var res = await _userMasterRepository.ModifyUserPrivileges(userPrivilegeUpdate);

            if(res.Item1 == true)
            {
                
                    var queueNameToPrivilegesMap = new Dictionary<string, List<RegisterUserPrivilegeDTO>>();
                    foreach (var access in userPrivilegeUpdate.UserPrivileges)
                    {
                        if (!queueNameToPrivilegesMap.TryGetValue(access.application.title, out var privileges))
                        {
                            queueNameToPrivilegesMap[access.application.title] = new List<RegisterUserPrivilegeDTO>();
                        }

                    var scopeList = (await Task.WhenAll(
                        access.scopes.Select(async s => new ScopeValue
                        {
                            Id = s.id,
                            Name = ExtractValue(s.name),
                            Value = ExtractCode(s.name),
                            ParentScope = (await _scopeRepository
                                .GetParentScopeValuesAsync(ExtractCode(s.name), access.level.id))
                                .FirstOrDefault()
                        })
                    )).ToList();

                    var privilege = new RegisterUserPrivilegeDTO
                        {
                            AppId = access.application.id,
                            AppName = access.application.title,
                            RoleId = access.role.id,
                            RoleName = access.role.title,
                            Permissions = access.permissions?
                                .Select(p => new PermissionAndScope
                                {
                                    Id = p.id,
                                    Name = p.name
                                })
                                .ToList(),
                            LevelId = access.level.id,
                            LevelName = access.level.title,
                            Scopes = scopeList,
                            IsAdmin = access.UserManagementEnabled,
                        };

                        queueNameToPrivilegesMap[access.application.title].Add(privilege);
                    }
                    var user = await _userMasterRepository.GetSingleAysnc(u => u.Id == userPrivilegeUpdate.userId);

                var userData = new UserDetailsWithPrivilegesDTO
                    {
                        Id = userPrivilegeUpdate.userId,
                        Name = user.Name,
                        UserName = user.UserName,
                        Email = user.Email,
                        Designation = user.Designation,
                        HrmsId = user.HrmsId,
                        MobileNumber = user.MobileNumber,
                        EffectiveFrom =user.EffectiveFrom.ToString("yyyy-MM-dd"),
                        ExpiresOn = user.ExpiresOn.ToString("yyyy-MM-dd"),
                        IsActive = user.IsActive
                    };


                    var consumingApps = await _applicationRepository.GetSelectedColumnByConditionAsync(
                        a => a.IsConsumingData == true,
                        a => new { a.Id }
                    );

                    var consumingAppIds = consumingApps.Select(x => x.Id).ToHashSet();

                    foreach (var queueName in queueNameToPrivilegesMap.Keys)
                    {
                        var privileges = queueNameToPrivilegesMap[queueName];

                        if (privileges.Any(p => consumingAppIds.Contains(p.AppId)))
                        {
                            userData.Privileges = privileges;
                            //await _rabbitMQService.PublishMessage($"{queueName}-GETUSERS", userData);
                            // throughy database queue for reliable message delivery and to handle cases where the consuming service might be down

                            try
                            {
                                var message_id = Guid.NewGuid();
                                _messageQueueRepository.Add(new MessageQueue
                                {
                                    UniqueId = message_id,
                                    QueueName = $"UM_{queueName}_USER".ToLower(),
                                    MessageBody = JsonConvert.SerializeObject(userData),
                                    CreatedAt = DateTime.Now
                                });
                                _messageQueueRepository.SaveChangesManaged();
                                await _mQueueProcessingService.ProcessQueueAsync($"UM_{queueName}_USER".ToLower());
                            }
                            catch (Exception ex)
                            {
                                throw;
                            }

                        }

                    }

                    //foreach (var queueName in queueNameToPrivilegesMap.Keys)
                    //{
                    //    userData.Privileges = queueNameToPrivilegesMap[queueName];
                    //    await _rabbitMQService.PublishMessage($"{queueName}-GETUSERS", userData);
                    //}
                    queueNameToPrivilegesMap.Clear();
                }
            
            return res;
        }
        public async Task<string> GetKeyofApplicationOfUserByRoleId(int roleId)
        {
            return await _roleRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.Id == roleId,
                e => e.Application.Key
                );
        }
        public async Task<object> GetDataForAdminManagement(SearchDataForAdminManagementDTO searchDataForAdminManagementDTO)
        {
            var data = await _userMasterRepository.GetDataForAdminManagement(searchDataForAdminManagementDTO.Scope, searchDataForAdminManagementDTO.LevelId);
            return data;
        }
        public async Task<(bool, string)> ManageAdmin(AdminManagementDTO data)
        {
            return await _userMasterRepository.ManageAdmin(data.UserIds, data.ScopeId, data.AppId, data.IsSingleAdmin);
        }

        public async Task<bool> AddApplicationToActivityLog(long userId, int applicationId)
        {
            try
            {
                var latestLog = _userActivityLogRepository
                    .GetFiltered(log => log.UserId == userId)
                    .OrderByDescending(log => log.ActivityTime)
                    .FirstOrDefault();

                if (latestLog == null || !latestLog.IsLogin)
                    return false;

                var hasLogoutAfter = _userActivityLogRepository
                    .GetFiltered(log => log.UserId == userId && log.ActivityTime > latestLog.ActivityTime && !log.IsLogin)
                    .Any();

                if (hasLogoutAfter)
                    return false;

                var appList = latestLog.Applications?.ToList() ?? new List<long>();
                appList.Add(applicationId);
                latestLog.Applications = appList.ToArray();
                _userActivityLogRepository.Update(latestLog);
                _userActivityLogRepository.SaveChangesManaged();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<UserProfileDTO> GetUserProfileData(string userName)
        {
            var rows = await _userMasterRepository.GetUserProfileData(userName);
            var dto = new UserProfileDTO();

            if (!rows.Any())
                return dto;

            var userGroups = rows
                .GroupBy(x => new { x.Id, x.UserName, x.Name, x.Designation, x.MobileNumber, x.Email, x.SignerID });

            dto.Users = userGroups.Select(g => new UserProfileDetailsDTO
            {
                Id = g.Key.Id,
                UserName = g.Key.UserName,
                Name = g.Key.Name,
                Designation = g.Key.Designation,
                MobileNumber = g.Key.MobileNumber,
                Email = g.Key.Email,
                SignerID = g.Key.SignerID.Trim(),
                Level = g.Select( x => x.Level).ToList()
            }).ToList();

            return dto;
        }

        public async Task<APIResponseClass<OTPResponseDTO>> SendOTPForEmailChange(string username)
        {
            var response = new APIResponseClass<OTPResponseDTO>();
            try
            {
                var user = await _userMasterRepository.GetSingleAysnc(e => e.UserName.Equals(username));

                if (user == null)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Invalid username or password.";
                    response.result = new OTPResponseDTO
                    {
                        IsSent = false,
                        TimeRemaining = 0
                    };
                    return response;
                }

                if (!(bool)user.IsActive)
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.message = "Your User Id is Suspendend. Please contact your department.";
                    response.result = new OTPResponseDTO
                    {
                        IsSent = false,
                        TimeRemaining = 0
                    };
                    return response;
                }

                if (Convert.ToBoolean(user.IsBlocked))
                {
                    TimeOnly blockTime = user.BlockTime ?? TimeOnly.MinValue;
                    double blockDuration = double.Parse(_config["Block:TimeInMinutes"]);
                    TimeOnly unblockTime = blockTime.AddMinutes(blockDuration);
                    TimeOnly currentTime = TimeOnly.FromDateTime(DateTime.Now);

                    if (unblockTime > currentTime)
                    {
                        // Calculate the remaining blocked time in minutes
                        int remainingMinutes = (int)(unblockTime.ToTimeSpan().TotalMinutes - currentTime.ToTimeSpan().TotalMinutes);

                        response.apiResponseStatus = Enum.APIResponseStatus.Error;
                        response.message = $"Your User ID is Blocked Temporarily for {remainingMinutes} minutes.";
                        response.result = null;
                        return response;
                    }
                }

                var otpLog = await _OtpRepository.GetSingleAysnc(e => e.UserName == username && e.OtpType == (short)OTPType.ChangeEmail);
                if (otpLog == null || (otpLog != null && otpLog.CreatedAt.AddMinutes(Double.Parse(_config["OTP:Delay"])) < DateTime.Now))
                {
                    await _otpService.SendOtp(user.MobileNumber, username, (short)OTPType.ChangeEmail);
                    response.result = new OTPResponseDTO
                    {
                        IsSent = true,
                        TimeRemaining = Int32.Parse(_config["OTP:Delay"])
                    };
                    //response.result.IsSent = true;
                    //response.result.TimeRemaining = Int32.Parse(_config["OTP:Delay"]);
                    response.message = "OTP has been sent.";
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                }
                else
                {
                    double otpDelay = double.TryParse(_config["OTP:Delay"], out double delay) ? delay : 0;
                    TimeSpan timeDiff = otpLog.CreatedAt.AddMinutes(otpDelay) - DateTime.Now;
                    response.result = new OTPResponseDTO
                    {
                        IsSent = true,
                        TimeRemaining = (timeDiff.TotalMinutes)
                    };
                    response.message = "OTP has already been sent. Please Wait for " + (int)Math.Ceiling(timeDiff.TotalMinutes) + " minutes and try again.";
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                }

                return response;
            }
            catch (Exception Ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = Ex.Message;
                return response;
            }
        }

        public async Task<bool> UpadteUserEmail(UpdateEmailDTO updateEmailDTO)
        {
            var otp = await _OtpRepository.GetSingleAysnc(e => e.UserName == updateEmailDTO.UserName && e.OtpType == (short)OTPType.ChangeEmail);
            if(otp.OtpValue == updateEmailDTO.OTP)
            {
                _OtpRepository.Delete(otp);
                _OtpRepository.SaveChangesManaged();
                var user = await _userMasterRepository.GetSingleAysnc(e => e.UserName == updateEmailDTO.UserName);
                user.Email = updateEmailDTO.Email;
                if (_userMasterRepository.Update(user))
                {
                    _userMasterRepository.SaveChangesManaged();
                    return true;
                }
            }  
            return false;
            
        }

        public async Task<bool> UpdateSignerIdAsync(string userName, string signerId)
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException("User name must not be empty.", nameof(userName));

            if (string.IsNullOrWhiteSpace(signerId))
                throw new ArgumentException("Signer ID must not be empty.", nameof(signerId));

            bool isUpdated = await _userMasterRepository.UpdateSignerIdByUserNameAsync(userName, signerId);

            if (!isUpdated)
                throw new Exception($"No active user found with user name '{userName}', or update failed.");

            return isUpdated;
        }
    }

}
