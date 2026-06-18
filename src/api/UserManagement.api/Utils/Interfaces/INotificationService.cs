using UserManagement.Models.DTO;

namespace UserManagement.Utils.Interfaces
{
    public interface INotificationService
    {
        public Task<(bool result, string message, int status)> SendEmailUsingQueue(EmailPayload emailPayload);
        public Task<(bool result, string message, int status)> SendSmsUsingQueue(SmsPayload smsPayload);
    }
}
