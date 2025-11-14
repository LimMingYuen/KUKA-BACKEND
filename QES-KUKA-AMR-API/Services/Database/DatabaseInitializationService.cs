using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;

namespace QES_KUKA_AMR_API.Services.Database;

/// <summary>
/// Service for initializing and seeding the database for production deployment
/// </summary>
public class DatabaseInitializationService : IDatabaseInitializationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DatabaseInitializationService> _logger;

    public DatabaseInitializationService(
        ApplicationDbContext dbContext,
        ILogger<DatabaseInitializationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Ensuring database is created and migrated...");

        try
        {
            // Apply any pending migrations
            var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken);

            if (pendingMigrations.Any())
            {
                _logger.LogInformation("Applying {Count} pending migrations", pendingMigrations.Count());
                await _dbContext.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Migrations applied successfully");
            }
            else
            {
                _logger.LogInformation("Database is up to date, no pending migrations");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring database creation");
            throw;
        }
    }

    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Clearing all data from database...");

        try
        {
            // Use ExecuteSqlRaw to truncate tables properly
            // This avoids "ghost rows" by resetting identity seeds

            // Disable all foreign key constraints explicitly for each table
            _logger.LogInformation("Disabling foreign key constraints...");
            await _dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE WorkflowDiagrams NOCHECK CONSTRAINT ALL;",
                cancellationToken);

            // Truncate tables in order (child tables first)
            _logger.LogInformation("Truncating WorkflowDiagrams...");
            await _dbContext.Database.ExecuteSqlRawAsync(
                "TRUNCATE TABLE WorkflowDiagrams",
                cancellationToken);

            // Re-enable all foreign key constraints
            _logger.LogInformation("Re-enabling foreign key constraints...");
            await _dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE WorkflowDiagrams WITH CHECK CHECK CONSTRAINT ALL;",
                cancellationToken);

            _logger.LogInformation("All data cleared successfully");

            // Verify counts
            var counts = new
            {
                WorkflowDiagrams = await _dbContext.WorkflowDiagrams.CountAsync(cancellationToken)
            };

            _logger.LogInformation("Row counts after clear: {@Counts}", counts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing database data");
            throw;
        }
    }

    public async Task SeedInitialDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding initial production data...");

        try
        {
            // Seed Roles
            await SeedRolesAsync(cancellationToken);
            
            // Seed Pages
            await SeedPagesAsync(cancellationToken);
            
            // Seed RolePermissions (Admin gets all pages)
            await SeedRolePermissionsAsync(cancellationToken);

            _logger.LogInformation("Initial data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding initial data");
            throw;
        }
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        if (!await _dbContext.Roles.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Seeding Roles...");
            
            var adminRole = new QES_KUKA_AMR_API.Data.Entities.Role
            {
                Id = 1,
                Name = "Admin",
                RoleCode = "ADMIN",
                IsProtected = true
            };

            _dbContext.Roles.Add(adminRole);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Admin role created");
        }
        else
        {
            _logger.LogInformation("Roles already exist, skipping");
        }
    }

    private async Task SeedPagesAsync(CancellationToken cancellationToken)
    {
        if (!await _dbContext.Pages.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Seeding Pages...");
            
            var pages = new[]
            {
                new QES_KUKA_AMR_API.Data.Entities.Page { PageName = "Dashboard", PagePath = "/Index", CreatedAt = DateTime.UtcNow },
                new QES_KUKA_AMR_API.Data.Entities.Page { PageName = "Workflow Management", PagePath = "/WorkflowManagement", CreatedAt = DateTime.UtcNow },
                new QES_KUKA_AMR_API.Data.Entities.Page { PageName = "Workflow Monitor", PagePath = "/WorkflowMonitor", CreatedAt = DateTime.UtcNow },
                new QES_KUKA_AMR_API.Data.Entities.Page { PageName = "Workflow Trigger", PagePath = "/WorkflowTrigger", CreatedAt = DateTime.UtcNow },
                new QES_KUKA_AMR_API.Data.Entities.Page { PageName = "Queue Monitor", PagePath = "/QueueMonitor", CreatedAt = DateTime.UtcNow },
                new QES_KUKA_AMR_API.Data.Entities.Page { PageName = "Mission History", PagePath = "/MissionHistory", CreatedAt = DateTime.UtcNow },
                new QES_KUKA_AMR_API.Data.Entities.Page { PageName = "Mission Configuration", PagePath = "/MissionConfiguration", CreatedAt = DateTime.UtcNow },
                new QES_KUKA_AMR_API.Data.Entities.Page { PageName = "Custom Mission", PagePath = "/CustomMission", CreatedAt = DateTime.UtcNow },
                new QES_KUKA_AMR_API.Data.Entities.Page { PageName = "Saved Custom Missions", PagePath = "/SavedCustomMissions", CreatedAt = DateTime.UtcNow },
                new QES_KUKA_AMR_API.Data.Entities.Page { PageName = "QR Code Management", PagePath = "/QrCode", CreatedAt = DateTime.UtcNow },
                new QES_KUKA_AMR_API.Data.Entities.Page { PageName = "Robot List", PagePath = "/RobotList", CreatedAt = DateTime.UtcNow },
                new QES_KUKA_AMR_API.Data.Entities.Page { PageName = "Area Management", PagePath = "/Area", CreatedAt = DateTime.UtcNow },
                new QES_KUKA_AMR_API.Data.Entities.Page { PageName = "Robot Utilization", PagePath = "/Analytics/RobotUtilization", CreatedAt = DateTime.UtcNow },
                new QES_KUKA_AMR_API.Data.Entities.Page { PageName = "User List", PagePath = "/UserList", CreatedAt = DateTime.UtcNow },
                new QES_KUKA_AMR_API.Data.Entities.Page { PageName = "Role & Permission Config", PagePath = "/RolePermissionConfig", CreatedAt = DateTime.UtcNow },
                new QES_KUKA_AMR_API.Data.Entities.Page { PageName = "Log Retention", PagePath = "/Settings/LogRetention", CreatedAt = DateTime.UtcNow }
            };

            _dbContext.Pages.AddRange(pages);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Seeded {Count} pages", pages.Length);
        }
        else
        {
            _logger.LogInformation("Pages already exist, skipping");
        }
    }

    private async Task SeedRolePermissionsAsync(CancellationToken cancellationToken)
    {
        if (!await _dbContext.RolePermissions.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Seeding RolePermissions...");
            
            var adminRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleCode == "ADMIN", cancellationToken);
            if (adminRole == null)
            {
                _logger.LogWarning("Admin role not found, cannot seed permissions");
                return;
            }

            var allPages = await _dbContext.Pages.ToListAsync(cancellationToken);
            var rolePermissions = allPages.Select(page => new QES_KUKA_AMR_API.Data.Entities.RolePermission
            {
                RoleId = adminRole.Id,
                PageId = page.Id
            }).ToList();

            _dbContext.RolePermissions.AddRange(rolePermissions);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Admin role assigned to {Count} pages", rolePermissions.Count);
        }
        else
        {
            _logger.LogInformation("RolePermissions already exist, skipping");
        }
    }

    public async Task ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Performing full database reset...");

        try
        {
            await ClearAllDataAsync(cancellationToken);
            await SeedInitialDataAsync(cancellationToken);

            _logger.LogInformation("Database reset completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database reset");
            throw;
        }
    }
}
