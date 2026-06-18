using UserManagement.Models.DTO;

namespace UserManagement.Helper
{
    public class SendEmailHelper
    {
        private readonly IConfiguration _configuration;

        public SendEmailHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(EmailRequest request)
        {
            if (!request.To.Any())
                throw new ArgumentException("At least one recipient is required");

            using var httpClient = new HttpClient();

            using var form = new MultipartFormDataContent
            {
                { new StringContent(string.Join(",", request.To)), "To" },
                { new StringContent(request.Subject), "Subject" },
                { new StringContent(request.HtmlBody), "Body" }
            };

            if (request.Cc?.Any() == true)
                form.Add(new StringContent(string.Join(",", request.Cc)), "Cc");

            if (request.Bcc?.Any() == true)
                form.Add(new StringContent(string.Join(",", request.Bcc)), "Bcc");

            if (request.Attachments != null)
            {
                foreach (var file in request.Attachments.Where(f => f.Length > 0))
                {
                    var fileContent = new StreamContent(file.OpenReadStream());
                    fileContent.Headers.ContentType =
                        new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

                    form.Add(fileContent, "UploadedFiles", file.FileName);
                }
            }
            var emailServiceApi = _configuration["NotificationService:NotificationApiUrl"] + "/api/Email/SendEmail";
            var response = await httpClient.PostAsync(emailServiceApi, form);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Email failed: {(int)response.StatusCode} - {error}"
                );
            }
        }

    }
}
