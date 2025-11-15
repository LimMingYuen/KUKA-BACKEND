using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Options;
using QES_KUKA_AMR_API.Services;
using QES_KUKA_AMR_API.Services.Analytics;
using QES_KUKA_AMR_API.Services.Areas;
using QES_KUKA_AMR_API.Services.Login;
using QES_KUKA_AMR_API.Services.Missions;
using QES_KUKA_AMR_API.Services.MissionTypes;
using QES_KUKA_AMR_API.Services.ResumeStrategies;
using QES_KUKA_AMR_API.Services.RobotTypes;
using QES_KUKA_AMR_API.Services.SavedCustomMissions;
using QES_KUKA_AMR_API.Services.ShelfDecisionRules;
using QES_KUKA_AMR_API.Services.Workflows;
using log4net;

var builder = WebApplication.CreateBuilder(args);

var logsPath = Path.Combine(AppContext.BaseDirectory, "Logs");
Directory.CreateDirectory(logsPath);
GlobalContext.Properties["LogDirectory"] = logsPath;

builder.Logging.AddLog4Net("log4net.config");

// Add services to the container.

// Add CORS support for web frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
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
builder.Services.AddHttpClient();
builder.Services.AddScoped<ILoginServiceClient, LoginServiceClient>();
builder.Services.AddScoped<IMissionTypeService, MissionTypeService>();
builder.Services.AddScoped<IRobotTypeService, RobotTypeService>();
builder.Services.AddScoped<IShelfDecisionRuleService, ShelfDecisionRuleService>();
builder.Services.AddScoped<IResumeStrategyService, ResumeStrategyService>();
builder.Services.AddScoped<IAreaService, AreaService>();
builder.Services.AddScoped<ISavedCustomMissionService, SavedCustomMissionService>();
builder.Services.AddScoped<IRobotAnalyticsService, RobotAnalyticsService>();

// WorkflowAnalyticsService needs HttpClient configured with base address
builder.Services.AddHttpClient<IWorkflowAnalyticsService, WorkflowAnalyticsService>(client =>
{
    // Configure base address to call own API (default port 5109)
    client.BaseAddress = new Uri("http://localhost:5109/");
});

builder.Services.AddScoped<IJobStatusClient, JobStatusClient>();



builder.Services.AddScoped<IMissionListClient, MissionListClient>();
builder.Services.AddScoped<ISavedMissionScheduleService, SavedMissionScheduleService>();
builder.Services.AddScoped<IWorkflowScheduleService, WorkflowScheduleService>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

// Mission Queue Services

builder.Services.AddHostedService<SavedMissionSchedulerBackgroundService>();
builder.Services.AddHostedService<WorkflowSchedulerBackgroundService>();

// Log Cleanup Services
builder.Services.Configure<LogCleanupOptions>(
    builder.Configuration.GetSection(LogCleanupOptions.SectionName));
builder.Services.AddScoped<LogCleanupService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Swagger always enabled (no environment check needed)
app.UseSwagger();
app.UseSwaggerUI();

// HTTPS redirection disabled for HTTP-only IIS deployment
// app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
