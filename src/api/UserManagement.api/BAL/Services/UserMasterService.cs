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
        private readonly IUserService _userService;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserHasApplicationRepository _userHasApplicationRepository;
        private readonly IUserApplicationHasUserRoleRepository _userApplicationHasUserRoleRepository;
        private readonly IUserRoleHasUserLevelRepository _userRoleHasUserLevelRepository;
        private readonly IUserRoleHasUserPermissionRepository _userRoleHasUserPermissionRepository;
        private readonly IUserLevelHasUserScopeRepository _userLevelHasUserScopeRepository;
        private readonly IScopeRepository _scopeRepository;
        private readonly IRabbitMQPublisherService _rabbitMQService;
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

        public UserMasterService(IMapper mapper, IUserMasterRepository userMasterRepository, IApplicationRepository applicationRepository, IClaimService claimService, IConfiguration config, IRoleRepository roleRepository,
            IUserHasApplicationRepository userHasApplicationRepository, IUserApplicationHasUserRoleRepository userHasUserRoleRepository, IUserHasUserManagementRepository userHasUserManagementRepository,
            IUserHasModuleManagementRepository userHasModuleManagement,
            IUserRoleHasUserLevelRepository userRoleHasUserLevelRepository, IUserRoleHasUserPermissionRepository userRoleHasUserPermissionRepository, IScopeRepository scopeRepository, IRabbitMQPublisherService rabbitMQService,
            IUserLevelHasUserScopeRepository userLevelHasUserScopeRepository, INotificationService notificationService, IPasswordChangeLogRepository passwordChangeLogRepository, IOtpRepository OtpRepository, IOTPService otpService, IUserActivityLogRepository userActivityLogRepository, IMessageQueueRepository messageQueueRepository, IMQueueProcessingService mQueueProcessingService, IAppScopeRepository appScopeRepository)
        {
            _userMasterRepository = userMasterRepository;

            _claimService = claimService;
            _config = config;
            _mapper = mapper;
            _applicationRepository = applicationRepository;
            _userHasApplicationRepository = userHasApplicationRepository;
            _userApplicationHasUserRoleRepository = userHasUserRoleRepository;
            _userRoleHasUserLevelRepository = userRoleHasUserLevelRepository;
            _userRoleHasUserPermissionRepository = userRoleHasUserPermissionRepository;
            _userLevelHasUserScopeRepository = userLevelHasUserScopeRepository;
            _scopeRepository = scopeRepository;
            _rabbitMQService = rabbitMQService;
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
