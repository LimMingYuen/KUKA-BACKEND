using System.IO;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Options;
using QES_KUKA_AMR_API.Services;
using QES_KUKA_AMR_API.Services.Analytics;
using QES_KUKA_AMR_API.Services.Areas;
using QES_KUKA_AMR_API.Services.OrganizationIds;
using QES_KUKA_AMR_API.Services.Auth;
using QES_KUKA_AMR_API.Services.Login;
using QES_KUKA_AMR_API.Services.Missions;
using QES_KUKA_AMR_API.Services.MissionTypes;
using QES_KUKA_AMR_API.Services.ResumeStrategies;
using QES_KUKA_AMR_API.Services.RobotTypes;
using QES_KUKA_AMR_API.Services.Roles;
using QES_KUKA_AMR_API.Converters;
using QES_KUKA_AMR_API.Services.SavedCustomMissions;
using QES_KUKA_AMR_API.Services.ShelfDecisionRules;
using QES_KUKA_AMR_API.Services.TemplateCategories;
using QES_KUKA_AMR_API.Services.Users;
using QES_KUKA_AMR_API.Services.WorkflowNodeCodes;
using QES_KUKA_AMR_API.Services.Pages;
using QES_KUKA_AMR_API.Services.RolePermissions;
using QES_KUKA_AMR_API.Services.UserPermissions;
using QES_KUKA_AMR_API.Services.Permissions;
using QES_KUKA_AMR_API.Services.RoleTemplatePermissions;
using QES_KUKA_AMR_API.Services.UserTemplatePermissions;
using QES_KUKA_AMR_API.Services.Queue;
using QES_KUKA_AMR_API.Services.RobotMonitoring;
using QES_KUKA_AMR_API.Services.Sync;
using QES_KUKA_AMR_API.Services.Schedule;
using QES_KUKA_AMR_API.Services.RobotRealtime;
using QES_KUKA_AMR_API.Hubs;
using QES_KUKA_AMR_API.Services.Licensing;
using QES_KUKA_AMR_API.Middleware;
using log4net;

var builder = WebApplication.CreateBuilder(args);

var logsPath = Path.Combine(AppContext.BaseDirectory, "Logs");
Directory.CreateDirectory(logsPath);
GlobalContext.Properties["LogDirectory"] = logsPath;

builder.Logging.AddLog4Net("log4net.config");

// Add services to the container.

// Add CORS support for web frontend (including SignalR)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:5109", "http://localhost:5003", "http://localhost:8004", "http://172.16.112.193:8004")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Required for SignalR
    });
});

// Configure JWT Authentication
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();
if (jwtOptions == null || string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
{
    throw new InvalidOperationException("JWT configuration is missing or invalid");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize DateTime values as UTC with 'Z' suffix (ISO 8601)
        // This ensures JavaScript correctly interprets them as UTC and converts to local time
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new UtcNullableDateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QES KUKA AMR API",
        Version = "v1"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Input the JWT token issued by the Login endpoint.",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("The connection string 'DefaultConnection' was not found.");
    }

    options.UseSqlServer(connectionString);
});
builder.Services.Configure<LoginServiceOptions>(
    builder.Configuration.GetSection(LoginServiceOptions.SectionName));
builder.Services.Configure<MissionServiceOptions>(
    builder.Configuration.GetSection(MissionServiceOptions.SectionName));
builder.Services.Configure<QrCodeServiceOptions>(
    builder.Configuration.GetSection(QrCodeServiceOptions.SectionName));
builder.Services.Configure<MapZoneServiceOptions>(
    builder.Configuration.GetSection(MapZoneServiceOptions.SectionName));
builder.Services.Configure<MobileRobotServiceOptions>(
    builder.Configuration.GetSection(MobileRobotServiceOptions.SectionName));
builder.Services.Configure<MissionListServiceOptions>(
    builder.Configuration.GetSection(MissionListServiceOptions.SectionName));

builder.Services.Configure<AmrServiceOptions>(
    builder.Configuration.GetSection(AmrServiceOptions.SectionName));

// License Configuration
builder.Services.Configure<LicenseOptions>(
    builder.Configuration.GetSection("License"));

// License Services (LicenseStateService must be singleton to track state across requests)
builder.Services.AddSingleton<ILicenseStateService, LicenseStateService>();
builder.Services.AddScoped<IMachineFingerprintService, MachineFingerprintService>();
builder.Services.AddScoped<ILicenseValidationService, LicenseValidationService>();
builder.Services.AddScoped<IRobotLicenseService, RobotLicenseService>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<ILoginServiceClient, LoginServiceClient>();
builder.Services.AddScoped<IMissionTypeService, MissionTypeService>();
builder.Services.AddScoped<IRobotTypeService, RobotTypeService>();
builder.Services.AddScoped<IShelfDecisionRuleService, ShelfDecisionRuleService>();
builder.Services.AddScoped<IResumeStrategyService, ResumeStrategyService>();
builder.Services.AddScoped<IAreaService, AreaService>();
builder.Services.AddScoped<IOrganizationIdService, OrganizationIdService>();
builder.Services.AddScoped<ISavedCustomMissionService, SavedCustomMissionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPageService, PageService>();
builder.Services.AddScoped<IRolePermissionService, RolePermissionService>();
builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();
builder.Services.AddScoped<IPermissionCheckService, PermissionCheckService>();
builder.Services.AddScoped<ITemplatePermissionCheckService, TemplatePermissionCheckService>();
builder.Services.AddScoped<IRoleTemplatePermissionService, RoleTemplatePermissionService>();
builder.Services.AddScoped<IUserTemplatePermissionService, UserTemplatePermissionService>();
builder.Services.AddScoped<IRobotAnalyticsService, RobotAnalyticsService>();
builder.Services.AddScoped<IWorkflowNodeCodeService, WorkflowNodeCodeService>();
builder.Services.AddScoped<IRobotMonitoringService, RobotMonitoringService>();
builder.Services.AddScoped<ITemplateCategoryService, TemplateCategoryService>();

// Authentication Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IExternalApiTokenService, ExternalApiTokenService>();

// WorkflowAnalyticsService needs HttpClient configured with base address
builder.Services.AddHttpClient<IWorkflowAnalyticsService, WorkflowAnalyticsService>(client =>
{
    // Configure base address to call own API (default port 5109)
    client.BaseAddress = new Uri("http://localhost:5109/");
});

builder.Services.AddScoped<IJobStatusClient, JobStatusClient>();
builder.Services.AddScoped<IRobotRealtimeClient, RobotRealtimeClient>();

builder.Services.AddScoped<IMissionListClient, MissionListClient>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

// Log Cleanup Services
builder.Services.Configure<LogCleanupOptions>(
    builder.Configuration.GetSection(LogCleanupOptions.SectionName));
builder.Services.AddScoped<LogCleanupService>();
builder.Services.AddHostedService<LogCleanupHostedService>();

// File Storage Configuration
builder.Services.Configure<FileStorageOptions>(
    builder.Configuration.GetSection(FileStorageOptions.SectionName));

// Auto-Sync Services
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddHostedService<AutoSyncHostedService>();

// Workflow Scheduling Services
builder.Services.AddScoped<IWorkflowScheduleService, WorkflowScheduleService>();
builder.Services.AddHostedService<WorkflowSchedulerHostedService>();

// Mission Queue Services
builder.Services.AddScoped<IMissionQueueService, MissionQueueService>();
builder.Services.AddScoped<IRobotSelectionService, RobotSelectionService>();
builder.Services.AddScoped<IJobOptimizationService, JobOptimizationService>();
builder.Services.AddHostedService<QueueProcessorService>();

// SignalR for real-time updates
builder.Services.AddSignalR();
builder.Services.AddSingleton<IQueueNotificationService, QueueNotificationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Swagger always enabled (no environment check needed)
app.UseSwagger();
app.UseSwaggerUI();

// HTTPS redirection disabled for HTTP-only IIS deployment
// app.UseHttpsRedirection();

app.UseCors();

// Request/Response logging middleware - logs all API requests with parameters
app.UseRequestResponseLogging();

// License validation at startup
using (var scope = app.Services.CreateScope())
{
    var licenseValidationService = scope.ServiceProvider.GetRequiredService<ILicenseValidationService>();
    var licenseStateService = app.Services.GetRequiredService<ILicenseStateService>();
    var fingerprintService = scope.ServiceProvider.GetRequiredService<IMachineFingerprintService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var licenseResult = await licenseValidationService.ValidateLicenseAsync();

    if (!licenseResult.IsValid)
    {
        licenseStateService.SetLimitedMode(true);
        logger.LogWarning("=====================================");
        logger.LogWarning("LICENSE VALIDATION FAILED");
        logger.LogWarning("Error Code: {Code}", licenseResult.ErrorCode);
        logger.LogWarning("Error Message: {Message}", licenseResult.ErrorMessage);
        logger.LogWarning("=====================================");
        logger.LogWarning("Machine ID: {MachineId}", fingerprintService.GetDisplayFingerprint());
        logger.LogWarning("=====================================");
        logger.LogWarning("Application running in LIMITED MODE");
        logger.LogWarning("Only license management endpoints are available.");
        logger.LogWarning("=====================================");
        Console.WriteLine();
        Console.WriteLine("=====================================");
        Console.WriteLine("LICENSE REQUIRED");
        Console.WriteLine($"Machine ID: {fingerprintService.GetDisplayFingerprint()}");
        Console.WriteLine("=====================================");
    }
    else
    {
        logger.LogInformation("License validated successfully for: {Customer}",
            licenseResult.LicenseInfo?.CustomerName);
    }
}

// License enforcement middleware - blocks requests when unlicensed
app.UseLicenseEnforcement();

// Serve uploaded files from external folder (for map images, etc.)
var fileStorageOptions = builder.Configuration.GetSection(FileStorageOptions.SectionName).Get<FileStorageOptions>();
if (fileStorageOptions != null && !string.IsNullOrEmpty(fileStorageOptions.UploadsPath))
{
    // Ensure the uploads directory exists
    var uploadsPath = fileStorageOptions.UploadsPath;
    Directory.CreateDirectory(uploadsPath);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads"
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs for real-time updates
app.MapHub<QueueHub>("/hubs/queue");

// Seed database with default admin user
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbInitializer.SeedAsync(context);
}

app.Run();

public partial class Program;
