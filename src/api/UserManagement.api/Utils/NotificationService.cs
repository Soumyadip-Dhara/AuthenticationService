using System.Net.Http.Json;
using System.Text.Json;
using UserManagement.Models.DTO;
using UserManagement.Utils.Interfaces;

namespace UserManagement.Utils
{
    public class NotificationService : INotificationService
    {
        IConfiguration _configuration;
        public NotificationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<(bool result, string message, int status)> SendEmailUsingQueue(EmailPayload emailPayload)
        {
            using var httpClient = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            });

            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 5.0; Windows 98; DigExt)");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var response = await httpClient.PostAsJsonAsync(
                _configuration["NotificationService:NotificationApiUrl"].ToString() + "/api/Email/SendEmailUsingQueue",
                emailPayload);

            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to send email: {response.StatusCode} - {responseContent}");
            }

            try
            {
                var jsonResponse = JsonSerializer.Deserialize<ResponseDto>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return (jsonResponse?.Result ?? false, jsonResponse?.Message ?? "Unknown error", jsonResponse?.Status ?? (int)response.StatusCode);
            }
            catch (JsonException ex)
            {
                throw new Exception("Failed to parse response from server.", ex);
            }
        }

        public async Task<(bool result, string message, int status)> SendSmsUsingQueue(SmsPayload smsPayload)
        {
            using var httpClient = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            });

            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 5.0; Windows 98; DigExt)");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var response = await httpClient.PostAsJsonAsync(
                _configuration["NotificationService:NotificationApiUrl"].ToString() +"/api/Sms/SendSmsUsingQueue",
                smsPayload);

            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to send SMS: {response.StatusCode} - {responseContent}");
            }

            try
            {
                var jsonResponse = JsonSerializer.Deserialize<ResponseDto>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return (jsonResponse?.Result ?? false, jsonResponse?.Message ?? "Unknown error", jsonResponse?.Status ?? (int)response.StatusCode);
            }
            catch (JsonException ex)
            {
                throw new Exception("Failed to parse response from server.", ex);
            }
        }
        public class ResponseDto
        {
            public bool Result { get; set; }
            public string Message { get; set; }
            public int Status { get; set; }
        }
    }
}
