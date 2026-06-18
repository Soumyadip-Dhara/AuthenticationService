namespace UserManagement.BAL.Services
{
    using System.Text;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using UserManagement.BAL.Interfaces;

    public class CertificateEmailService : ICertificateEmailService
    {
        private readonly IConfiguration _configuration;

        public CertificateEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendCertificateEmailAsync(
            string[] recipientEmail,
            string serialNo,
            bool isExpiry,
            IFormFile? attachedFile)
        {
            try
            {
                using var httpClient = new HttpClient();

                string subject = isExpiry
                    ? "Certificate Expired"
                    : "Certificate Issued";

                string body = isExpiry
                    ? $@"
                <html>
                  <body>
                    <p>Dear Sir/Madam,</p>
                    <p>Your certificate (Serial No: <b>{serialNo}</b>) has expired.</p>
                    <p>Please renew your certificate to continue using the services.</p>
                    <p>Best Regards,<br/>IFMS Team</p>
                  </body>
                </html>"
                    : $@"
                <html>
                  <body>
                    <p>Dear Sir/Madam,</p>
                    <p>Your certificate (Serial No: <b>{serialNo}</b>) has been issued successfully.</p>
                    <p>Please find the attached PDF.</p>
                    <p>Best Regards,<br/>IFMS Team</p>
                  </body>
                </html>";

                var form = new MultipartFormDataContent
            {
                { new StringContent(string.Join(",", recipientEmail)), "To" },
                { new StringContent(subject), "Subject" },
                { new StringContent(body), "Body" }
            };

                if (attachedFile != null && attachedFile.Length > 0)
                {
                    var fileContent = new StreamContent(attachedFile.OpenReadStream());
                    fileContent.Headers.ContentType =
                        new System.Net.Http.Headers.MediaTypeHeaderValue(attachedFile.ContentType);

                    form.Add(fileContent, "UploadedFiles", attachedFile.FileName);
                }

                var response = await httpClient.PostAsync(
                    _configuration["NotificationService:NotificationEmailApi"],
                    form
                );

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Email failed: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                // log or rethrow based on policy
                Console.WriteLine("Email error: " + ex);
                throw;
            }
        }
    }

}
