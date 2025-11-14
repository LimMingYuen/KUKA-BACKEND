using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Services;

namespace QES_KUKA_AMR_API.Tests;

public class MissionTypeApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"MissionTypes_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registrations
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>))
                .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Remove hosted services to keep tests lean
            var hostedServices = services
                .Where(d =>
                    d.ServiceType == typeof(IHostedService) &&
                    d.ImplementationType is not null &&
                    (d.ImplementationType == typeof(QueueProcessorBackgroundService) ||
                     d.ImplementationType == typeof(MissionSubmitterBackgroundService) ||
                     d.ImplementationType == typeof(JobStatusPollerBackgroundService)))
                .ToList();

            foreach (var descriptor in hostedServices)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            // Ensure database is created for each test run
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
}
