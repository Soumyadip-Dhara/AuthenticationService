//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Configuration;
//using System;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using UserManagement.DAL; // Replace with your actual EF Core DbContext namespace

//public class AuditLogCleanupService : BackgroundService
//{
//    private readonly IServiceScopeFactory _scopeFactory;
//    private readonly IConfiguration _config;

//    public AuditLogCleanupService(IServiceScopeFactory scopeFactory, IConfiguration configuration)
//    {
//        _scopeFactory = scopeFactory;
//        _config = configuration;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            // Schedule for daily at configured time (default 2:00 AM)
//            TimeSpan targetTime = TimeSpan.Parse(_config["AuditLog:RunAtTime"] ?? "02:00");
//            DateTime now = DateTime.Now;
//            DateTime nextRun = now.Date.AddDays(now.TimeOfDay > targetTime ? 1 : 0).Add(targetTime);
//            TimeSpan delay = nextRun - now;

//            await Task.Delay(delay, stoppingToken);

//            using (var scope = _scopeFactory.CreateScope())
//            {
//                var db = scope.ServiceProvider.GetRequiredService<UserManagementDBContext>();

//                DateTime cutoff = DateTime.Now.AddDays(-7); // 7 days before now

//                var oldAuditLogs = db.AuditLog
//                    .Where(a => a.ChangedBy == null && a.ChangeTimestamp < cutoff);

//                db.AuditLog.RemoveRange(oldAuditLogs);
//                await db.SaveChangesAsync(stoppingToken);
//            }
//        }
//    }
//}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using UserManagement.DAL; // Replace with your actual EF Core DbContext namespace

public class AuditLogCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AuditLogCleanupService> _logger;

    public AuditLogCleanupService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AuditLogCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Schedule for daily at configured time (default 2:00 AM)
            TimeSpan targetTime = TimeSpan.Parse(_config["AuditLog:RunAtTime"] ?? "02:00");
            DateTime now = DateTime.Now;
            DateTime nextRun = now.Date.AddDays(now.TimeOfDay > targetTime ? 1 : 0).Add(targetTime);
            TimeSpan delay = nextRun - now;

            _logger.LogInformation("Next audit log cleanup scheduled for {NextRun}", nextRun);

            await Task.Delay(delay, stoppingToken);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<UserManagementDBContext>();

                DateTime cutoff = DateTime.Now.AddDays(-7); // 7 days before now

                // Count rows before deletion
                int count = await db.AuditLogs
                    .Where(a => a.ChangedBy == null && a.ChangeTimestamp < cutoff)
                    .CountAsync(stoppingToken);

                if (count > 0)
                {
                    // Direct SQL delete for efficiency
                    await db.Database.ExecuteSqlRawAsync(
                        "DELETE FROM log.audit_log WHERE changed_by IS NULL AND table_name == 'user_master' AND change_timestamp < {0}",
                        cutoff,
                        stoppingToken
                    );

                    _logger.LogInformation("Deleted {Count} audit log rows older than {Cutoff} at {Time}",
                        count, cutoff, DateTime.Now);
                }
                else
                {
                    _logger.LogInformation("No audit log rows to delete at {Time}", DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit log cleanup failed at {Time}", DateTime.Now);
            }

            // Wait 24 hours until the next run
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}

