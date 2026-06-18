using Microsoft.EntityFrameworkCore;
using UserManagement.DAL;

public class NoticeExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NoticeExpiryWorker> _logger;

    public NoticeExpiryWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<NoticeExpiryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = DateTime.Today.AddDays(1).AddMinutes(1); // 00:01 tomorrow

            var delay = nextRun - now;
            if (delay.TotalMilliseconds > 0)
            {
                _logger.LogInformation("Next Notice Worker scheduled for {NextRun}", nextRun);

                await Task.Delay(delay, stoppingToken);
                _logger.LogInformation("");

            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<UserManagementDBContext>();

                var affected = await db.Database.ExecuteSqlRawAsync(@"
                    UPDATE public.notice
                    SET is_active = false,
                        updated_at = now()
                    WHERE is_active = true
                      AND expires_at IS NOT NULL
                      AND expires_at < now();
                ");

                _logger.LogInformation("Expired notices deactivated. Rows affected: {Count}", affected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notice expiry job failed");
            }
        }
    }
}
