using AutoMapper;
using RabbitMQ.Client;
using System.Collections.Generic;
using System.Security.Cryptography;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL.Interfaces;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.DAL.Repositories;
using UserManagement.DAL.Repositories.Master;
using UserManagement.Models.DTO;
using UserManagement.Utils;
using UserManagement.Utils.Interfaces;
using static Dapper.SqlMapper;
namespace UserManagement.BAL.Services.Master
{
    public class ApplicationService : IApplicationService
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserHasApplicationRepository _userHasApplicationRepository;
        private readonly IUserHasUserManagementRepository _userHasUserManagementRepository;
        private readonly IUserHasModuleManagementRepository _userHasModuleManagementRepository;
        private readonly IRabbitMQPublisherService _rabbitMQPublisherService;
        private readonly IUserApplicationHasUserRoleRepository _userApplicationHasUserRoleRepository;
        private readonly IClaimService _claimService;
        private readonly IUserMasterRepository _userMasterRepository;

        public ApplicationService(IApplicationRepository ApplicationRepository, IRoleRepository roleRepository, IUserHasApplicationRepository userHasApplicationRepository, IClaimService claimService, IUserMasterRepository userMasterRepository, IUserHasUserManagementRepository userHasUserManagementRepository, IUserHasModuleManagementRepository userHasModuleManagementRepository, IRabbitMQPublisherService rabbitMQService, IUserApplicationHasUserRoleRepository userApplicationHasUserRoleRepository)
        {
            _applicationRepository = ApplicationRepository;
            _roleRepository = roleRepository;
            _userHasApplicationRepository = userHasApplicationRepository;
            _userMasterRepository = userMasterRepository;
            _claimService = claimService;
            _userHasUserManagementRepository = userHasUserManagementRepository;
            _userHasModuleManagementRepository = userHasModuleManagementRepository;
            _rabbitMQPublisherService = rabbitMQService;
            _userApplicationHasUserRoleRepository = userApplicationHasUserRoleRepository;

        }
        public async Task<List<ApplicationGetDTO>> GetAllApplications()
        {
            var applications = (List<ApplicationGetDTO>)await _applicationRepository.GetSelectedColumnAsync(
                e => new ApplicationGetDTO
                {
                    Title = e.Title,
                    Id = e.Id,
                    url = e.Url,
                    //LogoUrl = e.LogoUrl,
                    LogoUrl = null,
                    Email = e.ApplicationAdminEmail,
                    Mobile = e.ApplicationAdminMobileNumber,
                    CreatedAt = e.CreatedAt,
                    CreatedBy = e.CreatedByNavigation.Name,
                    IsMaintenance = e.IsUnderMaintenance,
                    IsActive = e.IsActive == true,
                    IsConsumingData = e.IsConsumingData,
                    BaseUrl = e.BaseUrl,
                    IsMultiAdminDisallowed = e.IsMultiAdminDisallowed,
                    IsUseUserManagement = e.IsUseUserManagement
                }
            );
            //// Fetch the data first without including the 'CreatedBy' field
            //var applications = await _applicationRepository.GetSelectedColumnAsync(
            //    e => new
            //    {
            //        e.Title,
            //        e.Id,
            //        e.Url,
            //        e.LogoUrl,
            //        e.ApplicationAdminEmail,
            //        e.ApplicationAdminMobileNumber,
            //        e.CreatedAt,
            //        e.CreatedBy
            //    }
            //);

            //// Populate the 'CreatedBy' field asynchronously for each application
            //var result = new List<ApplicationGetDTO>();

            //foreach (var app in applications)
            //{
            //    var createdBy = await _userMasterRepository.GetSingleSelectedColumnByConditionAsync(
            //        x => x.Id == app.CreatedBy,
            //        x => x.Name
            //    );


            //    result.Add(new ApplicationGetDTO
            //    {
            //        Title = app.Title,
            //        Id = app.Id,
            //        url = app.Url,
            //        LogoUrl = app.LogoUrl,
            //        Email = app.ApplicationAdminEmail,
            //        Mobile = app.ApplicationAdminMobileNumber,
            //        CreatedAt = app.CreatedAt,
            //        CreatedBy = createdBy
            //    });

            //}
            return applications;
        }

        public async Task<List<ApplicationGetDTO>> GetApplicationByRoleName(string roleName)
        {

            var res = await _roleRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.Title == roleName,
                e => new ApplicationGetDTO
                {
                    Title = e.Application.Title,
                    Id = e.Application.Id,
                    url = e.Application.Url,
                    LogoUrl = e.Application.LogoUrl,
                    CreatedAt = e.Application.CreatedAt,
                    CreatedBy = e.Application.CreatedByNavigation.Name
                });
            var result = new List<ApplicationGetDTO>();
            result.Add(res);
            return result;
        }

        public async Task<string> GetApplicationNameById(int applictionId)
        {
            return await _applicationRepository.GetSingleSelectedColumnByConditionAsync(entity => entity.Id == applictionId, entity => entity.Title);
        }
        public async Task<List<ApplicationGetDTO>> GetApplicationByUserIdForMM()
        {
            var applications = (List<ApplicationGetDTO>)await _userHasModuleManagementRepository.GetSelectedColumnByConditionAsync(
                e => e.UserId == _claimService.GetUserId(),
                e => new ApplicationGetDTO
                {
                    Id = e.AssignedAppId,
                    Title = e.AssignedApp.Title,
                    url = e.AssignedApp.Url,
                    Email = e.AssignedApp.ApplicationAdminEmail,
                    Mobile = e.AssignedApp.ApplicationAdminMobileNumber,
                    LogoUrl = e.AssignedApp.LogoUrl,
                    CreatedBy = e.AssignedApp.CreatedByNavigation.Name,
                    CreatedAt = e.AssignedApp.CreatedAt,
                    IsMaintenance = e.AssignedApp.IsUnderMaintenance,
                    IsActive = e.AssignedApp.IsActive == true
                });
            //var result = new List<ApplicationGetDTO>();

            //foreach (var app in applications)
            //{
            //    var createdBy = await _userMasterRepository.GetSingleSelectedColumnByConditionAsync(
            //        x => x.Id == app.CreatedBy,
            //        x => x.Name
            //    );


            //    result.Add(new ApplicationGetDTO
            //    {
            //        Title = app.Title,
            //        Id = app.Id,
            //        url = app.url,
            //        LogoUrl = app.LogoUrl,
            //        Email = app.Email,
            //        Mobile = app.Mobile,
            //        CreatedAt = app.CreatedAt,
            //        CreatedBy = createdBy
            //    });

            //}
            return applications;
        }

        public async Task<List<ApplicationGetDTO>> GetApplicationsByNames(string[] applicationNames)
        {
            var applications = (List<ApplicationGetDTO>)await _applicationRepository.GetSelectedColumnByConditionAsync(
                entity => applicationNames.Contains(entity.Title.ToLower()),
                entity => new ApplicationGetDTO
                {
                    Title = entity.Title,
                    Id = entity.Id,
                    url = entity.Url,
                    LogoUrl = entity.LogoUrl,
                    CreatedAt = entity.CreatedAt,
                    CreatedBy = entity.CreatedByNavigation.Name
                }
             );
            // Populate the 'CreatedBy' field asynchronously for each application
            //var result = new List<ApplicationGetDTO>();

            //foreach (var app in applications)
            //{
            //    var createdBy = await _userMasterRepository.GetSingleSelectedColumnByConditionAsync(
            //        x => x.Id == app.CreatedBy,
            //        x => x.Name
            //    );

            //    result.Add(new ApplicationGetDTO
            //    {
            //        Title = app.Title,
            //        Id = app.Id,
            //        url = app.url,
            //        LogoUrl = app.LogoUrl,
            //        CreatedAt = app.CreatedAt,
            //        CreatedBy = createdBy
            //    });

            //}
            return applications;
        }

        public async Task<string> GetApplicationUrl(int applicationId)
        {
            return await _applicationRepository.GetSingleSelectedColumnByConditionAsync(entity => entity.Id == applicationId, entity => entity.Url);
        }

        public async Task<string> GetApplicationKey(int applicationId)
        {
            return await _applicationRepository.GetSingleSelectedColumnByConditionAsync(entity => entity.Id == applicationId, entity => entity.Key);
        }

        // new

        public async Task<(bool, string)> CreateApplication(ApplicationCreateDTO application, string photoPath)
        {
            string base64Image = "data:image/png;base64,";
            if (System.IO.File.Exists(photoPath))
            {
                // Read the file as a byte array
                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(photoPath);

                // Convert to Base64
                base64Image += Convert.ToBase64String(fileBytes);
            }
            var app = await _applicationRepository.GetAllByConditionAsync(e => e.Title == application.Title || e.Url == application.Url);
            if (app.Count == 0)
            {
                var key = GenerateSecretKey();
                var res = await _applicationRepository.CreateApplication(application, base64Image, _claimService.GetUserId(), key);
                if (res.Item1)
                {
                    var emailTask = new EmailPayload
                    {
                        Email = application.Email,
                        Subject = "Application On Boarding",
                        Body = $@"
                            <html>
                                <body>
                                    <h1>Welcome to IFMS!</h1>
                                    <p>Dear {application.Email},</p>
                                    <p>We are excited to have you on board!</p>
                                    <p>Your unique secret key for the application <strong>{application.Title}</strong> is: <strong>{key}</strong>.</p>
                                    <p>To consume your application’s user details, please use the following Queue Name: <strong>{application.Title}-GETUSERS</strong>.</p>
                                    <p>For role consumption, the Queue Name is: <strong>{application.Title}-GETROLES</strong>.</p>
                                    <p>To access permission details, please refer to the Queue Name: <strong>{application.Title}-GETPERMISSIONS</strong>.</p>
                                    <p>For level information, use the Queue Name: <strong>{application.Title}-GETLEVELS</strong>.</p>
                                    <p>Lastly, for scope access, the Queue Name is: <strong>{application.Title}-GETSCOPES</strong>.</p>
                                    <p><em>We kindly ask you to keep this key secure, as it is essential for verifying the JWT signature. Please ensure that the queue names are used exclusively for consuming user details and do not share them with anyone.</em></p>
                                    <p>Thank you for being a part of our journey!</p>
                                    <p>Warm regards,<br/>The IFMS Team</p>
                                </body>
                            </html>"
                    };
                    var smsTask = new SmsDTO //TODO: Call the proper message template
                    {
                        Message = "",
                        PhoneNumber = application.Mobile,
                        TemplateId = ""
                    };
                    await _rabbitMQPublisherService.PublishMessage("emailQueue", emailTask);
                    await _rabbitMQPublisherService.PublishMessage("smsQueue", smsTask);
                }
                return res;

            }
            return (false, "Application already exists");
        }

        public async Task<(bool, string, string)> UpdateApplication(ApplicationUpdateDTO application, string photoPath)
        {

            if (!int.TryParse(application.Id, out int appId))
            {
                return (false, "Invalid Application ID", "");
            }
            var app = await _applicationRepository.GetSingleAysnc(e => e.Id == appId);
            string base64Image = "";
            if (photoPath != "")
            {
                if (System.IO.File.Exists(photoPath))
                {
                    base64Image = "data:image/png;base64,";
                    // Read the file as a byte array
                    byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(photoPath);

                    // Convert to Base64
                    base64Image += Convert.ToBase64String(fileBytes);
                }
                app.LogoUrl = base64Image;
            }
            app.Title = application.Title;
            app.Url = application.Url;
            app.ApplicationAdminEmail = application.Email;
            app.ApplicationAdminMobileNumber = application.Mobile;
            app.UpdatedBy = _claimService.GetUserId();
            app.UpdatedAt = DateTime.Now;
            app.IsUnderMaintenance = application.IsMaintenance;
            app.IsConsumingData = application.IsConsumingData;
            app.IsMultiAdminDisallowed = application.IsMultiAdminDisallowed;
            app.BaseUrl = application.BaseUrl;
            app.IsUseUserManagement = application.IsUseUserManagement;

            if (app != null)
            {
                if (_applicationRepository.Update(app))
                {
                    _applicationRepository.SaveChangesManaged();
                    return (true, "Application Details Updated", base64Image);
                }
                return (false, "Application Details Updation Failed", "");

            }
            return (false, "Application doesn't exists", "");
        }

        public async Task<(string, bool)> DeleteApplication(int AppId)
        {
            var res = await _applicationRepository.DeleteApplication(AppId);
            return res;
        }

        public async Task<List<ApplicationGetDTO>> GetApplicationsByIds(int[] ids)
        {
            var r = await _applicationRepository.GetAllAsync();
            List<ApplicationGetDTO> result = (List<ApplicationGetDTO>)await _applicationRepository.GetSelectedColumnAsync(
                 e => new ApplicationGetDTO
                 {
                     Title = e.Title,
                     Id = e.Id
                 }
             );
            return result;
        }

        public static string GenerateSecretKey(int keySize = 512)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] key = new byte[keySize / 8];
                rng.GetBytes(key);
                return Convert.ToBase64String(key);
            }
        }
        public async Task<List<ApplicationFetchDTO>> GetApplicationsByUserId(long userId)
        {
            var res = new List<ApplicationFetchDTO>();
            string[] userRoles = _claimService.GetRoles();
            if (Array.Exists(userRoles, roleName => roleName.ToLower().Contains("state admin")))
            {
                res = (List<ApplicationFetchDTO>)await _applicationRepository.GetSelectedColumnAsync(
                    e => new ApplicationFetchDTO
                    {
                        AppId = e.Id,
                        ApplicationName = e.Title,
                        LogoUrl = e.LogoUrl
                    });
            }
            else
            {
                res = (List<ApplicationFetchDTO>)await _userHasApplicationRepository.GetSelectedColumnByConditionAsync(
                        e => e.UserId == userId,
                        e => new ApplicationFetchDTO
                        {
                            AppId = e.AppId,
                            ApplicationName = e.App.Title,
                            LogoUrl = e.App.LogoUrl
                        });
            }

            return res;
        }
        public async Task<List<ApplicationFetchDTO>> GetApplicationsForAppAccessByUserId(long userId)
        {
            // Fetch all apps first
            var res = (List<ApplicationFetchDTO>)await _userApplicationHasUserRoleRepository
                .GetSelectedColumnByConditionAsync(
                    e =>
                        e.UserHasApp.UserId == userId
                        && e.Role.IsOperational == true
                        && e.UserHasApp.App.IsActive == true,

                    e => new ApplicationFetchDTO
                    {
                        AppId = e.AppId,
                        ApplicationName = e.App.Title,
                        LogoUrl = e.App.LogoUrl ?? "",
                        IsMaintenance = e.App.IsUnderMaintenance,
                        IsActive = e.App.IsActive == true,
                        IsUseUserManagement = e.App.IsUseUserManagement == true
                    });

            var distinctApplications = res
                .GroupBy(app => app.AppId)
                .Select(g => g.First())
                .ToList();

            // Your UM app id
            long userManagementAppId = 1;

            // Check whether any NON-UM app supports UM
            bool hasAnyUserManagementEnabledApp = distinctApplications.Any(x =>
                x.AppId != userManagementAppId
                && x.IsUseUserManagement);

            if (distinctApplications.Count() == 1 && distinctApplications[0].AppId == 1)  // IF USER HAS ONLY UM APPS
            {
                hasAnyUserManagementEnabledApp = true;
            }
            

            // If no app supports UM -> remove UM app
            if (!hasAnyUserManagementEnabledApp)
            {
                distinctApplications = distinctApplications
                    .Where(x => x.AppId != userManagementAppId)
                    .ToList();
            }

            return distinctApplications;
        }
        public async Task<List<ApplicationGetDTO>> GetApplicationsByUserIdForUM(long userId)
        {
            var res = new List<ApplicationGetDTO>();

            var data = (List<ApplicationGetDTO>)await _userHasUserManagementRepository.GetSelectedColumnByConditionAsync(
                e => e.UserId == _claimService.GetUserId() && e.AssignedAppId != 1,
                e => new ApplicationGetDTO
                {
                    Id = e.AssignedAppId,
                    Title = e.AssignedApp.Title,
                });

            if (data.Count != 0)
            {
                res = data;
            }
            return res;
        }
        public async Task<List<ApplicationGetDTO>> GetAllApplicationsForSuperAdmin()
        {
            var applications = (List<ApplicationGetDTO>)await _applicationRepository.GetSelectedColumnByConditionAsync(
                        e => e.Title.ToLower() != "user management" && e.Title.ToLower() != "module management",
                        e => new ApplicationGetDTO
                        {
                            Title = e.Title,
                            Id = e.Id,
                        }
                    );


            return applications;
        }
        public async Task<bool> GetApplicationStatusById(int appId)
        {
            return await _applicationRepository.GetSingleSelectedColumnByConditionAsync(e => e.Id == appId, e => e.IsUnderMaintenance);
        }

        public async Task<string?> GetApplicationMaintenanceMsgById(int appId)
        {
            return await _applicationRepository.GetSingleSelectedColumnByConditionAsync(e => e.Id == appId, e => e.MaintenanceMsg);
        }
        public async Task<List<ApplicationForServiceDTO>> GetAllApplicationsForService()
        {
            return  (List<ApplicationForServiceDTO>)await _applicationRepository.GetSelectedColumnAsync(
                e => new ApplicationForServiceDTO
                {
                    Title = e.Title,
                    Id = e.Id,
                    IsActive = e.IsActive == true
                }
            );
        }

        public async Task<ModuleDetails?> FetchModuleDetails(long appId)
        {
            return (await _applicationRepository.GetSelectedColumnByConditionAsync(
                    e => e.Id == appId,
                    e => new ModuleDetails
                    {
                        Title = e.Title,
                        Email = e.ApplicationAdminEmail
                    }))
                .SingleOrDefault();
        }

    }
}
