using log4net;
using QES_KUKA_AMR_WEB.Services;
using QES_KUKA_AMR_WEB.Middleware;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;

var builder = WebApplication.CreateBuilder(args);

// Initialize log4net
var logsPath = Path.Combine(AppContext.BaseDirectory, "Logs");
Directory.CreateDirectory(logsPath);
GlobalContext.Properties["LogDirectory"] = logsPath;

builder.Logging.AddLog4Net("log4net.config");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Add services to the container.
builder.Services.AddRazorPages();

// Add session support with configurable timeout
var sessionTimeoutHours = builder.Configuration.GetValue<int>("Authentication:SessionIdleTimeoutHours", 8);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(sessionTimeoutHours);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add HttpClient for API calls
builder.Services.AddHttpClient();

// Register TokenRefreshService for JWT auto-refresh
builder.Services.AddScoped<ITokenRefreshService, TokenRefreshService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

// Add token refresh middleware (must be after UseSession, before UseAuthorization)
app.UseTokenRefresh();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
