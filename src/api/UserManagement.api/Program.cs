using UserManagement.DAL.Repositories.MQueue;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;


using UserManagement.Background_Worker;
using UserManagement.BAL;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.BAL.Services;
using UserManagement.BAL.Services.Master;
using UserManagement.BAL.Services.MQueue;
using UserManagement.DAL;
using UserManagement.DAL.Interfaces;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.DAL.Interfaces.MQueue;
using UserManagement.DAL.Repositories;
using UserManagement.DAL.Repositories.Master;
using UserManagement.DAL.Repositories.MQueue;
using UserManagement.Extensions;
using UserManagement.Middlewares;
using UserManagement.RbbitMQ;
using UserManagement.Throttling;
using UserManagement.Utils;
using UserManagement.Utils.Interfaces;
using UserMangement.BAL.Interfaces.MQueue;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsProduction())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(5008); // Listens on all network interfaces for port 5007
        options.AddServerHeader = false; // Removes 'Server: Kestrel'

    });
}else{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(5001); // Allow access from LAN on port 5001
        options.AddServerHeader = false;
    });
}


//Database Connection
builder.Services.AddDbContext<UserManagementDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("UserManagementDBConnection"),
    //options => options.CommandTimeout(999)                   
    options => options.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null)
), ServiceLifetime.Transient);

//CTS DATABASE CONNECTION
builder.Services.AddDbContext<CTSDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CommonLogDBConnection"),
    //options => options.CommandTimeout(999)                   
    options => options.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null)
), ServiceLifetime.Transient);

//RABBITMQ
builder.Services
   .AddRabbitMQ(builder.Configuration)
   .AddMessageProcessing();

//Automapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddAutoMapper(typeof(Program));

//Repositories

builder.Services.AddTransient<ITempHrmRepository, TempHrmRepository>();
builder.Services.AddTransient<ILevelRelationshipRepository, LevelRelationshipRepository>();
builder.Services.AddTransient<IRoleHasPermissionRepository, RoleHasPermissionRepository>();
builder.Services.AddTransient<IRoleRelationshipRepository, RoleRelationshipRepository>();
builder.Services.AddTransient<IPermissionRepository, PermissionRepository>();
builder.Services.AddTransient<ILevelMasterRepository, LevelMasterRepository>();
builder.Services.AddTransient<ILevelRepository, LevelRepository>();
builder.Services.AddTransient<IScopeRepository, ScopeRepository>();
builder.Services.AddTransient<IScopeRelationshipRepository, ScopeRelationshipRepository>();
builder.Services.AddTransient<IApplicationHasRoleRepository, ApplicationHasRoleRepository>();
builder.Services.AddTransient<IApplicationHasLevelRepository, ApplicationHasLevelRepository>();
builder.Services.AddTransient<IRoleRepository, RoleRepository>();
builder.Services.AddTransient<IApplicationRepository, ApplicationRepository>();
builder.Services.AddTransient<IUserMasterRepository, UserMasterRepository>();
builder.Services.AddTransient<IUserHasApplicationRepository, UserHasApplicationRepository>();
builder.Services.AddTransient<IUserApplicationHasUserRoleRepository, UserApplicationHasUserRoleRepository>();
builder.Services.AddTransient<IUserRoleHasUserPermissionRepository, UserRoleHasUserPermissionRepository>();
builder.Services.AddTransient<IUserRoleHasUserLevelRepository, UserRoleHasUserLevelRepository>();
builder.Services.AddTransient<IUserLevelHasUserScopeRepository, UserLevelHasUserScopeRepository>();
builder.Services.AddTransient<IUserRoleHasOwnAppRepository, UserRoleHasOwnAppRepository>();
builder.Services.AddTransient<IUserHasUserManagementRepository, UserHasUserManagementRepository>();
builder.Services.AddTransient<IUserHasModuleManagementRepository, UserHasModuleManagementRepository>();
builder.Services.AddTransient<ILevelHasAllowedRoleRepository, LevelHasAllowedRoleRepository>();
builder.Services.AddTransient<IBlockedIPAddressesRepository, BlockedIPAddressesRepository>();
builder.Services.AddTransient<IPasswordChangeLogRepository, PasswordChangeLogRepository>();
builder.Services.AddTransient<IMigrationRepository, MigrationRepository>();
builder.Services.AddTransient<IOtpRepository, OtpRepository>();
builder.Services.AddTransient<INoticeRepository, NoticeRepository>();
builder.Services.AddScoped<IUserActivityLogRepository, UserActivityLogRepository>();
builder.Services.AddScoped<IAuditCertificateRepository, AuditCertificateRepository>();
builder.Services.AddScoped<IHashIntegrityRepository, HashIntegrityRepository>();
builder.Services.AddTransient<IMasterServiceRepository, MasterServiceRepository>();
builder.Services.AddTransient<IMessageQueueRepository, MessageQueueRepository>();
builder.Services.AddTransient<IConsumeFailedLogRepository, ConsumeFailedLogRepository>();
builder.Services.AddTransient<IConsumeLogRepository, ConsumeLogRepository>();
builder.Services.AddTransient<IMessageQueueFailedLogsRepository, MessageQueueFailedLogsRepository>();
builder.Services.AddTransient<IConsumedAcknowledgementLogRepository, ConsumedAcknowledgementLogRepository>();
builder.Services.AddTransient<IPublishedAcknowledgementLogRepository, PublishedAcknowledgementLogRepository>();
builder.Services.AddTransient<IUserRoleScopeAppContextRepository, UserRoleScopeAppContextRepository>();
builder.Services.AddTransient<IOtpLogRepository, OtpLogRepository>();
builder.Services.AddTransient<IRabbitMQLogsRepository, RabbitMQLogsRepository>();
builder.Services.AddTransient<IAppScopeRepository, AppScopeRepository>();


//Services
builder.Services.AddTransient<ITempHrmService, TempHrmService>();
builder.Services.AddTransient<IScopeService, ScopeService>();
builder.Services.AddTransient<ILevelRelationshipService, LevelRelationshipService>();
builder.Services.AddTransient<IRoleHasPermissionService, RoleHasPermissionService>();
builder.Services.AddTransient<IRoleRelationshipService, RoleRelationshipService>();
builder.Services.AddTransient<IPermissionService, PermissionService>();
builder.Services.AddTransient<ILevelService, LevelService>();
builder.Services.AddTransient<IClaimService, ClaimService>();
builder.Services.AddTransient<IRoleService, RoleService>();
builder.Services.AddTransient<IApplicationService, ApplicationService>();
builder.Services.AddTransient<IUserService, UserMasterService>();

builder.Services.AddTransient<IIpBlockingService, IpBlockingService>();
builder.Services.AddTransient<IRabbitMQPublisherService, RabbitMQPublisherService>();
builder.Services.AddScoped<IUserActivityLogService, UserActivityLogService>();
builder.Services.AddTransient<INotificationService, NotificationService>();
builder.Services.AddTransient<IOTPService, OTPService>();
builder.Services.AddTransient<IDashboardService, DashboardService>();
builder.Services.AddTransient<IMigrationService, MigrationService>();
builder.Services.AddTransient<INoticeService, NoticeService>();

builder.Services.AddHostedService<AuditLogCleanupService>();
builder.Services.AddScoped<IAuditCertificateService, AuditCertificateService>();
builder.Services.AddScoped<ICertificateEmailService, CertificateEmailService>();
builder.Services.AddTransient<IServiceManagementService, ServiceManagementService>();
builder.Services.AddTransient<IMQueueProcessingService, MQueueProcessingService>();
builder.Services.AddTransient<IRabbitMqService, RabbitMqService>();
builder.Services.AddTransient<ILogsService, LogsService>();


builder.Services.AddHostedService<CertificateExpiryNotificationService>();
//BackGround Worker
builder.Services.AddHostedService<MigrationWorker>();

builder.Services.AddHostedService<NoticeExpiryWorker>();




// Blaclisted IP cleanup
builder.Services.AddBlacklistCleanupService();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

// Add Memory Cache for Introspection
builder.Services.AddMemoryCache();

builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        // Note the IDP address
        options.SetIssuer("https://wbifms.gov.in/");

        // Configure Introspection
        options.UseIntrospection()
               .SetClientId("usermanagement-api")
               .SetClientSecret("usermanagement-api-secret");

        // Register the System.Net.Http integration (required for introspection).
        options.UseSystemNetHttp()
               .SetProductInformation(typeof(Program).Assembly)
               .ConfigureHttpClientHandler(handler =>
               {
                   handler.ServerCertificateCustomValidationCallback =
                       HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
               });

        // Register the ASP.NET Core host.
        options.UseAspNetCore();
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// HSTS
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(30);
    //options.ExcludedHosts.Add("example.com");
    //options.ExcludedHosts.Add("www.example.com");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}
else
{
    app.UseHttpsRedirection();
    app.UseHsts();
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("X-AspNet-Version");
        context.Response.Headers.Remove("X-AspNetMvc-Version");
        await next();
    });
}

// app.UseAntiXssMiddleware();


//Register Rate Limiting Service
app.UseRateLimitingMiddleware();

string frontEndUrl = builder.Configuration.GetSection("AllowedOrigins").Value ?? "https://ifms.wb.gov.in";
// Console.WriteLine("CORS Enabled: " + frontEndUrl);


if(app.Environment.IsDevelopment())
{
    // In development, allow all origins
    app.UseCors(builder => builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
}
else
{
    // In production, restrict to specific origin
    app.UseCors(builder => builder
        .WithOrigins(frontEndUrl)  // Allow only this origin
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
}
//app.UseCors(builder => builder
//    .WithOrigins(frontEndUrl)  // Allow only this origin
//    // .AllowAnyOrigin()
//    .AllowAnyMethod()
//    .AllowAnyHeader()
//    .AllowCredentials()
//    );  // Enable credentials if needed

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public class LocalIssuerRoutingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri != null && request.RequestUri.Host.Equals("wbifms.gov.in", StringComparison.OrdinalIgnoreCase))
        {
            var builder = new UriBuilder(request.RequestUri)
            {
                // Note: Make sure this is the IP of your IDP. Your Demo API used 10.176.100.17.
                Host = "10.176.100.17",
                Port = 5001
            };
            request.RequestUri = builder.Uri;
        }
        return await base.SendAsync(request, cancellationToken);
    }
}

public class RoutingHandlerFilter : Microsoft.Extensions.Http.IHttpMessageHandlerBuilderFilter
{
    public Action<Microsoft.Extensions.Http.HttpMessageHandlerBuilder> Configure(Action<Microsoft.Extensions.Http.HttpMessageHandlerBuilder> next)
    {
        return builder =>
        {
            next(builder);
            builder.AdditionalHandlers.Add(new LocalIssuerRoutingHandler());
        };
    }
}
