using Npgsql;
using UserManagement.DAL.Entities;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Services;

public class CertificateExpiryNotificationService : BackgroundService
{
    private readonly ILogger<CertificateExpiryNotificationService> _logger;
    private readonly string _connectionString;
    private readonly IServiceProvider _serviceProvider;

    public CertificateExpiryNotificationService(
        ILogger<CertificateExpiryNotificationService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _connectionString = configuration.GetConnectionString("UserManagementDBConnection");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Certificate Expiry Notification Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1).AddMinutes(1); 
                if (now < now.Date.AddMinutes(1)) 
                {
                    nextRun = now.Date.AddMinutes(1); 
                }

                var delay = nextRun - now;
                _logger.LogInformation("Next certificate check scheduled at {NextRun}", nextRun);

                await Task.Delay(delay, stoppingToken);

                
                await DeactivateExpiredCertificatesAsync();
                await CheckAndSendExpiryNotificationsAsync();

            }
            catch (TaskCanceledException)
            {
                
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking certificate expiry.");
            }
        }
    }

    private async Task CheckAndSendExpiryNotificationsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var _certificateEmailService = scope.ServiceProvider.GetRequiredService<ICertificateEmailService>();

        var expiringCertificates = new List<SecurityAuditCertificateDetail>();

        using (var conn = new NpgsqlConnection(_connectionString))
        {
            await conn.OpenAsync();

            string sql = @"
                SELECT id, serial_no, issue_date, expiry_date, certificate_pdf, emails
                FROM public.security_audit_certificate_details
                WHERE is_active = true
                  AND expiry_date <= NOW() + INTERVAL '15 days'
                  AND expiry_date > NOW();";

            using (var cmd = new NpgsqlCommand(sql, conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    expiringCertificates.Add(new SecurityAuditCertificateDetail
                    {
                        Id = reader.GetInt64(0),
                        SerialNo = reader.GetString(1),
                        IssueDate = reader.GetDateTime(2),
                        ExpiryDate = reader.GetDateTime(3),
                        Emails = reader["emails"] as string[]
                    });
                }
            }
        }

        foreach (var dto in expiringCertificates)
        {
            if (dto.Emails != null && dto.Emails.Any())
            {
               
                    await _certificateEmailService.SendCertificateEmailAsync(dto.Emails, dto.SerialNo, true, null);
                    

                
            }
        }
    }

    private async Task DeactivateExpiredCertificatesAsync()
    {
        using (var conn = new NpgsqlConnection(_connectionString))
        {
            await conn.OpenAsync();

            string sql = @"
            UPDATE public.security_audit_certificate_details
            SET is_active = false
            WHERE is_active = true
              AND expiry_date <= NOW();";

            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                int rowsUpdated = await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("Deactivated {Count} expired certificates.", rowsUpdated);
            }
        }
    }


}

