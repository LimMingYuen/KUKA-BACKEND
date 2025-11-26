using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;

namespace QES_KUKA_AMR_API.Services.Permissions;

public interface IPermissionCheckService
{
    Task<bool> UserCanAccessPageAsync(int userId, string pagePath, CancellationToken cancellationToken = default);
    Task<List<string>> GetUserAllowedPagePathsAsync(int userId, CancellationToken cancellationToken = default);
}

public class PermissionCheckService : IPermissionCheckService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<PermissionCheckService> _logger;

    public PermissionCheckService(
        ApplicationDbContext dbContext,
        ILogger<PermissionCheckService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Check if a user can access a specific page.
    /// Permission check priority:
    /// 1. If user is SuperAdmin, allow all access
    /// 2. If UserPermission exists for user+page, use it
    /// 3. If user has ANY role with permission to page, allow access
    /// 4. Default: deny access
    /// </summary>
    public async Task<bool> UserCanAccessPageAsync(int userId, string pagePath, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePagePath(pagePath);

        // Get user with roles
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("User {UserId} not found for permission check", userId);
            return false;
        }

        // 1. Check if user is SuperAdmin - bypass all permission checks
        if (user.IsSuperAdmin)
        {
            _logger.LogDebug("User {UserId} is SuperAdmin - access granted to {PagePath}", userId, pagePath);
            return true;
        }

        // Get page
        var page = await _dbContext.Pages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PagePath == normalizedPath, cancellationToken);

        if (page is null)
        {
            _logger.LogWarning("Page {PagePath} not found for permission check", pagePath);
            return false;
        }

        // 2. Check user-specific permission override
        var userPermission = await _dbContext.UserPermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PageId == page.Id, cancellationToken);

        if (userPermission is not null)
        {
            _logger.LogDebug("User {UserId} has explicit permission for {PagePath}: {CanAccess}",
                userId, pagePath, userPermission.CanAccess);
            return userPermission.CanAccess;
        }

        // 3. Check role-based permissions
        // Parse user roles from JSON
        var userRoles = System.Text.Json.JsonSerializer.Deserialize<List<string>>(user.RolesJson ?? "[]") ?? new List<string>();

        if (userRoles.Count == 0)
        {
            _logger.LogDebug("User {UserId} has no roles - access denied to {PagePath}", userId, pagePath);
            return false;
        }

        // Get role IDs for user's role codes
        var roleIds = await _dbContext.Roles
            .AsNoTracking()
            .Where(r => userRoles.Contains(r.RoleCode))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
        {
            _logger.LogDebug("User {UserId} has role codes but no matching roles found - access denied to {PagePath}",
                userId, pagePath);
            return false;
        }

        // Check if ANY of the user's roles has permission to this page
        var hasRolePermission = await _dbContext.RolePermissions
            .AsNoTracking()
            .AnyAsync(rp => roleIds.Contains(rp.RoleId) && rp.PageId == page.Id && rp.CanAccess, cancellationToken);

        _logger.LogDebug("User {UserId} role-based permission for {PagePath}: {CanAccess}",
            userId, pagePath, hasRolePermission);

        return hasRolePermission;
    }

    /// <summary>
    /// Get all page paths that a user is allowed to access.
    /// This is useful for frontend to filter navigation menus and routes.
    /// </summary>
    public async Task<List<string>> GetUserAllowedPagePathsAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Get user with roles
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("User {UserId} not found for getting allowed pages", userId);
            return new List<string>();
        }

        // SuperAdmin has access to all pages
        if (user.IsSuperAdmin)
        {
            _logger.LogDebug("User {UserId} is SuperAdmin - returning all pages", userId);
            return await _dbContext.Pages
                .AsNoTracking()
                .Select(p => p.PagePath)
                .ToListAsync(cancellationToken);
        }

        var allowedPageIds = new HashSet<int>();

        // Get pages from user-specific permissions (where CanAccess = true)
        var userPermissionPageIds = await _dbContext.UserPermissions
            .AsNoTracking()
            .Where(up => up.UserId == userId && up.CanAccess)
            .Select(up => up.PageId)
            .ToListAsync(cancellationToken);

        foreach (var pageId in userPermissionPageIds)
        {
            allowedPageIds.Add(pageId);
        }

        // Get pages from user-specific permissions that explicitly deny access
        var deniedPageIds = await _dbContext.UserPermissions
            .AsNoTracking()
            .Where(up => up.UserId == userId && !up.CanAccess)
            .Select(up => up.PageId)
            .ToListAsync(cancellationToken);

        // Parse user roles from JSON
        var userRoles = System.Text.Json.JsonSerializer.Deserialize<List<string>>(user.RolesJson ?? "[]") ?? new List<string>();

        if (userRoles.Count > 0)
        {
            // Get role IDs for user's role codes
            var roleIds = await _dbContext.Roles
                .AsNoTracking()
                .Where(r => userRoles.Contains(r.RoleCode))
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            if (roleIds.Count > 0)
            {
                // Get pages from role permissions (where CanAccess = true)
                var rolePermissionPageIds = await _dbContext.RolePermissions
                    .AsNoTracking()
                    .Where(rp => roleIds.Contains(rp.RoleId) && rp.CanAccess)
                    .Select(rp => rp.PageId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                foreach (var pageId in rolePermissionPageIds)
                {
                    // Don't add if explicitly denied by user permission
                    if (!deniedPageIds.Contains(pageId))
                    {
                        allowedPageIds.Add(pageId);
                    }
                }
            }
        }

        // Convert page IDs to page paths
        var allowedPages = await _dbContext.Pages
            .AsNoTracking()
            .Where(p => allowedPageIds.Contains(p.Id))
            .Select(p => p.PagePath)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("User {UserId} has access to {Count} pages", userId, allowedPages.Count);

        return allowedPages;
    }

    private static string NormalizePagePath(string pagePath)
    {
        return (pagePath ?? string.Empty).Trim().ToLowerInvariant();
    }
}
