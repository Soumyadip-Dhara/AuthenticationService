using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Drawing.Printing;
using System.Security;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.DAL.Repositories.Master;
using UserManagement.Helper;
using UserManagement.Models.DTO;
using UserManagement.RbbitMQ.Constant;
using UserManagement.Utils;
using UserManagement.Utils.Interfaces;
using UserMangement.BAL.Interfaces.MQueue;

namespace UserManagement.BAL.Services.Master
{
    public class ServiceManagementService : IServiceManagementService
    {
        private readonly IClaimService _claimService;
        private readonly IMapper _mapper;
        private readonly IMasterServiceRepository _masterServiceRepository;
        private readonly IApplicationService _applicationService;
        private readonly IMQueueProcessingService _mQueueProcessingService;
        private readonly IConfiguration _configuration;

        public ServiceManagementService(IClaimService claimService, IMapper mapper, IMasterServiceRepository masterServiceRepository, IApplicationService applicationService, IMQueueProcessingService mQueueProcessingService, IConfiguration configuration)
        {
            _claimService = claimService;
            _mapper = mapper;
            _masterServiceRepository = masterServiceRepository;
            _applicationService = applicationService;
            _mQueueProcessingService = mQueueProcessingService;
            _configuration = configuration;
        }

        public async Task<List<MasterServicesDTO>> GetServicesWithPermissions()
        {
            var services = await _masterServiceRepository.GetServicesWithPermissionsAsync();
            return services ?? new List<MasterServicesDTO>();
        }


        public async Task<bool> EnableDisableServiceAsync(EnableServiceRequestDTO request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.ServiceId <= 0 || request.AppId <= 0)
                throw new ArgumentException("Invalid service or application id");

            var userId = _claimService.GetUserId();

            var moduleDetails = await _applicationService.FetchModuleDetails(request.AppId);
            var serviceDetails = await GetServiceByIdAsync(request.ServiceId);

            var response = await _masterServiceRepository.EnableDisableServiceAsync(
                request.ServiceId,
                request.AppId,
                request.Enabled,
                userId
            );

            if (!response.Item1)
                return false;



            if (moduleDetails == null || string.IsNullOrWhiteSpace(moduleDetails.Email))
                return true; // email is non-critical


            // CONDITIONAL LOGIC

            var isEnabled = request.Enabled;

            var subject = isEnabled
                ? $"{serviceDetails.ServiceName} Enabled Successfully"
                : $"{serviceDetails.ServiceName} Disabled Successfully";

            var statusClass = isEnabled ? "status-enabled" : "status-disabled";
            var statusText = isEnabled ? "enabled" : "disabled";

            var secretSection = isEnabled
                ? $"""
                   <p>
                     A new <strong>client secret</strong> has been generated for the utility
                     service module you manage.
                   </p>

                   <div class="secret-box">
                     <div class="secret-value">
                       {response.Item2}
                     </div>
                   </div>
                   """
                : $"""
                   <p>
                     The service has been disabled and can no longer be accessed by this application.
                     The disabled secret key:
                   </p>

                   <div class="secret-box">
                     <div class="secret-value">
                     {response.Item2}
                     </div>
                   </div>
                   """;

            var emailPayload = new EmailRequest
            {
                To = new List<string> { moduleDetails.Email },
                Subject = subject,
                HtmlBody = $@"
                <!DOCTYPE html>
                <html lang=""en"">
                <head>
                  <meta charset=""UTF-8"" />
                  <title>Service Status Notification</title>
                  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
                  <style>
                    body {{
                      margin: 0;
                      padding: 0;
                      background-color: #f4f6f8;
                      font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"",
                        Roboto, Helvetica, Arial, sans-serif;
                    }}

                    .email-wrapper {{
                      width: 100%;
                      padding: 40px 0;
                    }}

                    .email-container {{
                      max-width: 600px;
                      margin: 0 auto;
                      background-color: #ffffff;
                      border-radius: 12px;
                      overflow: hidden;
                      box-shadow: 0 10px 30px rgba(0, 0, 0, 0.08);
                    }}

                    .header {{
                      background: #4f46e5;
                      color: #ffffff;
                      padding: 24px 32px;
                      font-size: 20px;
                      font-weight: 600;
                    }}

                    .content {{
                      padding: 32px;
                      color: #1f2937;
                      font-size: 15px;
                      line-height: 1.6;
                    }}

                    .status-enabled {{
                      background-color: #ecfdf5;
                      border-left: 4px solid #10b981;
                      color: #065f46;
                      padding: 16px;
                      border-radius: 8px;
                      margin-bottom: 24px;
                    }}

                    .status-disabled {{
                      background-color: #fef2f2;
                      border-left: 4px solid #ef4444;
                      color: #7f1d1d;
                      padding: 16px;
                      border-radius: 8px;
                      margin-bottom: 24px;
                    }}

                    .secret-box {{
                      background-color: #f9fafb;
                      border: 1px solid #e5e7eb;
                      border-radius: 10px;
                      padding: 20px;
                      margin: 24px 0;
                    }}

                    .secret-value {{
                      font-family: Courier, monospace;
                      font-size: 14px;
                      word-break: break-all;
                      background-color: #ffffff;
                      padding: 12px;
                      border-radius: 6px;
                      border: 1px dashed #d1d5db;
                    }}

                    .footer {{
                      padding: 20px 32px;
                      background-color: #f9fafb;
                      font-size: 12px;
                      color: #6b7280;
                      text-align: center;
                    }}
                  </style>
                </head>

                <body>
                  <div class=""email-wrapper"">
                    <div class=""email-container"">
                      <div class=""header"">
                        {serviceDetails.ServiceName} – Status Update
                      </div>

                      <div class=""content"">
                        <p>Hello Admin,</p>

                        <div class=""{statusClass}"">
                          {serviceDetails.ServiceName} has been <strong>{statusText}</strong> for application
                          <strong>{moduleDetails.Title}</strong>.
                        </div>

                        {secretSection}

                        <p>
                          If you did not request this change, contact the system administrator immediately.
                        </p>

                        <p>— {serviceDetails.ServiceName} Team</p>
                      </div>

                      <div class=""footer"">
                        © 2026 {serviceDetails.ServiceName}. All rights reserved.<br />
                        This is an automated message. Do not reply.
                      </div>
                    </div>
                  </div>
                </body>
                </html>"
            };

            //var emailHelper = new SendEmailHelper(_configuration);
            //await emailHelper.SendEmailAsync(emailPayload);
            await _mQueueProcessingService.ProcessQueueAsync(MessageQueueConstants.DOCUMENT_STROAGE_SEND_CLIENT_SECRET);

            return true;
        }

        public async Task<ServiceDetails?> GetServiceByIdAsync(long serviceId)
        {
            return (await _masterServiceRepository.GetSelectedColumnByConditionAsync(
                    e => e.Id == serviceId,
                    e => new ServiceDetails
                    {
                        ServiceName = e.ServiceName
                    }))
                .SingleOrDefault();
        }


        public async Task<bool> AddServiceAsync(AddServiceRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.ServiceName))
                throw new Exception("Service name cannot be empty");
            //var createdBy =  _claimService.GetUserId();
            //return await _masterServiceRepository.AddServiceAsync(request);

            var service = new Service
            {
                ServiceName = request.ServiceName,
                CreatedBy = _claimService.GetUserId()
                // CreatedAt handled automatically
            };

            _masterServiceRepository.Add(service);

            _masterServiceRepository.SaveChangesManaged();

            return true;
        }

    }
}
