using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.IdP.Services
{
    public class SmsPayload
    {
        public string MobileNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ResponseDto
    {
        public bool Result { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Status { get; set; }
    }

    public interface INotificationService
    {
        Task<(bool result, string message, int status)> SendSmsUsingQueue(SmsPayload smsPayload);
    }

    public class NotificationService : INotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IConfiguration configuration, ILogger<NotificationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(bool result, string message, int status)> SendSmsUsingQueue(SmsPayload smsPayload)
        {
            var isEnabled = _configuration.GetValue<bool>("NotificationService:Enabled");
            if (!isEnabled)
            {
                _logger.LogInformation("[Development Mode - SMS Blocked] Mobile: {MobileNumber}, Message: {Message}", 
                    smsPayload.MobileNumber, smsPayload.Message);
                return (true, "SMS sending is disabled in development environment.", 200);
            }

            using var httpClient = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            });

            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 5.0; Windows 98; DigExt)");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var apiUrl = _configuration["NotificationService:NotificationApiUrl"]?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                throw new Exception("NotificationService:NotificationApiUrl configuration is missing or empty.");
            }

            var url = apiUrl.TrimEnd('/') + "/api/sms//SendSmsUsingQueue";

            _logger.LogInformation("Sending SMS using queue to {MobileNumber} via {Url}", smsPayload.MobileNumber, url);

            var response = await httpClient.PostAsJsonAsync(url, smsPayload);

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
    }
}
