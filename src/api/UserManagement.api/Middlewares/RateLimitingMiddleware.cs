using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Text.Json;
using UserManagement.Enum;
using UserManagement.Helper;
using UserManagement.Throttling;

namespace UserManagement.Middlewares
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        //private static readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());
        private readonly int _maxRequests;
        private readonly TimeSpan _timeWindow;

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _maxRequests = configuration.GetValue<int>("RateLimiting:MaxRequests", 100);
            _timeWindow = TimeSpan.FromMinutes(configuration.GetValue<double>("RateLimiting:TimeWindowMinutes", 1));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var route = context.Request.Path.ToString();
            var specificRoutes = new HashSet<string>
            {
                
            };

            if (!specificRoutes.Contains(route))
            {
                await _next(context);
                return;
            }

            var ipAddress = context.Request.Headers["Src"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (string.IsNullOrEmpty(ipAddress))
            {
                await _next(context);
                return;
            }

            
            if (BlacklistStore.BlacklistedIps.TryGetValue(ipAddress, out var blacklistExpiration))
            {
                if (blacklistExpiration > DateTime.UtcNow)
                {

                    _logger.LogWarning($"Access denied for blacklisted IP: {ipAddress}");
                    var response = new APIResponseClass<bool>();
                    response.apiResponseStatus = APIResponseStatus.Error;
                    response.message = "Access Denied.";
                    response.result = false;
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.Headers.Add("access-control-allow-origin", "*");
                    context.Response.ContentType = "application/json; charset=utf-8 ";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return; 
                }
                else
                {
                    
                    BlacklistStore.BlacklistedIps.TryRemove(ipAddress, out _);
                }
            }

            var cacheKey = $"RateLimit_{ipAddress}";
            if (!RateLimitingCache.Cache.TryGetValue(cacheKey, out RateLimitInfo rateLimitInfo))
            {
                rateLimitInfo = new RateLimitInfo { Count = 1, Timestamp = DateTime.UtcNow };
                RateLimitingCache.Cache.Set(cacheKey, rateLimitInfo, _timeWindow);
            }
            else
            {
                if (rateLimitInfo.Timestamp + _timeWindow > DateTime.UtcNow)
                {
                    if (rateLimitInfo.Count >= _maxRequests)
                    {
                        BlacklistStore.BlacklistedIps[ipAddress] = DateTime.UtcNow.AddMinutes(30);
                        _logger.LogWarning($"Rate limit exceeded for IP: {ipAddress}");
                        var response = new APIResponseClass<bool>();
                        response.apiResponseStatus = APIResponseStatus.Error;
                        response.message = $"Rate limit exceeded. Access Denied for the IP {ipAddress}";
                        response.result = false;
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        context.Response.Headers.Add("access-control-allow-origin", "*");
                        context.Response.ContentType = "application/json; charset=utf-8";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                        return;
                    }

                    rateLimitInfo.Count++;
                }
                else
                {
                    rateLimitInfo.Count = 1;
                    rateLimitInfo.Timestamp = DateTime.UtcNow;
                }

                RateLimitingCache.Cache.Set(cacheKey, rateLimitInfo, _timeWindow);
            }

            await _next(context);
        }

        private class RateLimitInfo
        {
            public int Count { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }

    public static class RateLimitingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimitingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitingMiddleware>();
        }
    }

    public class BlacklistCleanupService : BackgroundService
    {
        private readonly ILogger<BlacklistCleanupService> _logger;

        public BlacklistCleanupService(ILogger<BlacklistCleanupService> logger)
        {
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var now = DateTime.UtcNow;
                        var expiredIps = BlacklistStore.BlacklistedIps.Where(kvp => kvp.Value < now).Select(kvp => kvp.Key).ToList();

                        foreach (var expiredIp in expiredIps)
                        {
                            BlacklistStore.BlacklistedIps.TryRemove(expiredIp, out _);
                            RateLimitingCache.Cache.Remove($"RateLimit_{expiredIp}");
                            _logger.LogInformation($"Removed expired IP from blacklist: {expiredIp}");
                        }

                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); 
                    }
                    catch (TaskCanceledException ex)
                    {
                        _logger.LogInformation($"Black listed IP cleaning up process failed: {ex}");

                    }
                }
            });
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlacklistCleanupService(this IServiceCollection services)
        {
            services.AddHostedService<BlacklistCleanupService>();
            return services;
        }
    }
}
