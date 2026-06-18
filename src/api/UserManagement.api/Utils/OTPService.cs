using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
using UserManagement.DAL.Repositories;
using UserManagement.Helper;
using UserManagement.Models.DTO;
using UserManagement.Utils.Interfaces;

namespace UserManagement.Utils
{
    public class OTPService : IOTPService
    {
        private readonly INotificationService _notificationService;
        private readonly IOtpRepository _otpRepository;
        private readonly IOtpLogRepository _otpLogRepository;
        private readonly IConfiguration _configuration;

        public OTPService(INotificationService notificationService, IOtpRepository otpRepository, IConfiguration configuration, IOtpLogRepository otpLogRepository)
        {
            _notificationService = notificationService;
            _otpRepository = otpRepository;
            _configuration = configuration;
            _otpLogRepository = otpLogRepository;
        }
        public async Task SendOtp(string mobileNumber, string username, short type)
        {
            string otp = GenerateOTP.Generate();
            
            var privateKey = _configuration["Security:OtpPrivateKey"];

            var (hash, salt) = OtpHashHelper.GenerateOtpHash(otp, privateKey);

            var OtpHashLog = new OTPHashLogPayload
            {
                PhoneNumber = mobileNumber,
                OtpHash = hash,
                Salt = salt,
                OtpType = type,
                TimeStamp = DateTime.Now,
                Username = username
            };

            

            var smsTask = new SmsPayload
            {
                PhoneNumber = mobileNumber,
                Otp = otp,
                TemplateId = "1307161761159352614"
            };
            var smsRes = await _notificationService.SendSmsUsingQueue(smsTask);
            _otpLogRepository.Add(new OtpLog
            {
                Timestamp = DateTime.Now,
                Username = OtpHashLog.Username,
                PhoneNumber = OtpHashLog.PhoneNumber,
                OtpHash = OtpHashLog.OtpHash,
                Salt = OtpHashLog.Salt,
                OtpType = OtpHashLog.OtpType,
                SmsResponseStatus = (short)smsRes.status,
                SmsResponseMessage = smsRes.message
            });
            _otpLogRepository.SaveChangesManaged();
            if (smsRes.status == 3)
            {
                throw new Exception(smsRes.message);
            }
            var otpLog = await _otpRepository.GetAllByConditionAsync(e => e.UserName == username && e.OtpType == type);
            if (otpLog != null)
            {
                _otpRepository.DeleteRange(otpLog);
            }
            _otpRepository.Add(new Otp
            {
                CreatedAt = DateTime.Now,
                UserName = username,
                OtpValue = otp,
                OtpType = type
            });
            _otpRepository.SaveChangesManaged();
        }
    }
}

