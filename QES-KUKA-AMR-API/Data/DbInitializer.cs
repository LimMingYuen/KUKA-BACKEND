using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Data;

/// <summary>
/// Database initializer for seeding initial data.
/// Run this after migrations to create default admin user.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Seeds the database with initial data including default admin user and roles.
    /// </summary>
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Ensure database is created
        await context.Database.MigrateAsync();

        // Seed default ADMIN role first
        await SeedDefaultRoleAsync(context);

        // Check if admin user already exists
        var adminExists = await context.Users.AnyAsync(u => u.Username == "admin");

        if (!adminExists)
        {
            // BCrypt hash for password "admin"
            // Generated using: BCrypt.Net.BCrypt.HashPassword("admin", workFactor: 11)
            var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("admin", workFactor: 11);

            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = adminPasswordHash,
                Nickname = "Administrator",
                IsSuperAdmin = true,
                Roles = new List<string> { "Admin" },
                CreateTime = DateTime.UtcNow,
                CreateBy = "System",
                CreateApp = "QES-KUKA-AMR-API-DbInitializer",
                LastUpdateTime = DateTime.UtcNow,
                LastUpdateBy = "System",
                LastUpdateApp = "QES-KUKA-AMR-API-DbInitializer"
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            Console.WriteLine("✓ Default admin user created successfully");
            Console.WriteLine("  Username: admin");
            Console.WriteLine("  Password: admin");
            Console.WriteLine("  SECURITY WARNING: Please change the default password!");
        }
        else
        {
            Console.WriteLine("ℹ Admin user already exists, skipping creation");
        }
    }

    /// <summary>
    /// Alternative method to seed specific user with custom password.
    /// </summary>
    public static async Task SeedAdminUserAsync(
        ApplicationDbContext context,
        string username,
        string password,
        string? nickname = null)
    {
        var userExists = await context.Users.AnyAsync(u => u.Username == username.ToLowerInvariant());

        if (!userExists)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);

            var user = new User
            {
                Username = username.ToLowerInvariant(),
                PasswordHash = passwordHash,
                Nickname = nickname ?? username,
                IsSuperAdmin = true,
                Roles = new List<string> { "Admin" },
                CreateTime = DateTime.UtcNow,
                CreateBy = "System",
                CreateApp = "QES-KUKA-AMR-API-DbInitializer",
                LastUpdateTime = DateTime.UtcNow,
                LastUpdateBy = "System",
                LastUpdateApp = "QES-KUKA-AMR-API-DbInitializer"
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            Console.WriteLine($"✓ User '{username}' created successfully");
        }
        else
        {
            Console.WriteLine($"ℹ User '{username}' already exists, skipping creation");
        }
    }

    /// <summary>
    /// Seeds default ADMIN role if it doesn't exist.
    /// </summary>
    private static async Task SeedDefaultRoleAsync(ApplicationDbContext context)
    {
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleCode == "ADMIN");

        if (adminRole == null)
        {
            var role = new Role
            {
                Name = "Administrator",
                RoleCode = "ADMIN",
                IsProtected = true, // Prevent deletion
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            };

            context.Roles.Add(role);
            await context.SaveChangesAsync();

            Console.WriteLine("✓ Default ADMIN role created");
        }
        else
        {
            Console.WriteLine("ℹ ADMIN role already exists, skipping creation");
        }
    }
}
