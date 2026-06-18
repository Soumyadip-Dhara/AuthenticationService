

namespace UserManagement.BAL.Interfaces
{
    public interface ICertificateEmailService
    {
        Task SendCertificateEmailAsync(
            string[] recipientEmail,
            string serialNo,
            bool isExpiry,
            IFormFile? attachedFile
        );
    }

}
