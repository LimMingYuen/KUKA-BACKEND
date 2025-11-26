using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;

namespace QES_KUKA_AMR_API.Services.Permissions;

public interface ITemplatePermissionCheckService
{
    Task<bool> UserCanAccessTemplateAsync(int userId, int savedMissionId, CancellationToken cancellationToken = default);
    Task<List<int>> GetUserAllowedTemplateIdsAsync(int userId, CancellationToken cancellationToken = default);
}

public class TemplatePermissionCheckService : ITemplatePermissionCheckService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TemplatePermissionCheckService> _logger;

    public TemplatePermissionCheckService(
        ApplicationDbContext dbContext,
        ILogger<TemplatePermissionCheckService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Check if a user can access a specific template.
    /// Permission check priority:
    /// 1. If user is SuperAdmin, allow all access
    /// 2. If UserTemplatePermission exists for user+template, use it
    /// 3. If user has ANY role with permission to template, allow access
    /// 4. Default: deny access
    /// </summary>
    public async Task<bool> UserCanAccessTemplateAsync(int userId, int savedMissionId, CancellationToken cancellationToken = default)
    {
        // Get user
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("User {UserId} not found for template permission check", userId);
            return false;
        }

        // 1. Check if user is SuperAdmin - bypass all permission checks
        if (user.IsSuperAdmin)
        {
            _logger.LogDebug("User {UserId} is SuperAdmin - access granted to template {TemplateId}", userId, savedMissionId);
            return true;
        }

        // 2. Check user-specific permission override
        var userPermission = await _dbContext.UserTemplatePermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(up => up.UserId == userId && up.SavedCustomMissionId == savedMissionId, cancellationToken);

        if (userPermission is not null)
        {
            _logger.LogDebug("User {UserId} has explicit permission for template {TemplateId}: {CanAccess}",
                userId, savedMissionId, userPermission.CanAccess);
            return userPermission.CanAccess;
        }

        // 3. Check role-based permissions
        var userRoles = System.Text.Json.JsonSerializer.Deserialize<List<string>>(user.RolesJson ?? "[]") ?? new List<string>();

        if (userRoles.Count == 0)
        {
            _logger.LogDebug("User {UserId} has no roles - access denied to template {TemplateId}", userId, savedMissionId);
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
            _logger.LogDebug("User {UserId} has role codes but no matching roles found - access denied to template {TemplateId}",
                userId, savedMissionId);
            return false;
        }

        // Check if ANY of the user's roles has permission to this template
        var hasRolePermission = await _dbContext.RoleTemplatePermissions
            .AsNoTracking()
            .AnyAsync(rtp => roleIds.Contains(rtp.RoleId) && rtp.SavedCustomMissionId == savedMissionId && rtp.CanAccess, cancellationToken);

        _logger.LogDebug("User {UserId} role-based permission for template {TemplateId}: {CanAccess}",
            userId, savedMissionId, hasRolePermission);

        return hasRolePermission;
    }

    /// <summary>
    /// Get all template IDs that a user is allowed to access.
    /// This is useful for frontend to filter Mission Control templates.
    /// </summary>
    public async Task<List<int>> GetUserAllowedTemplateIdsAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Get user
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("User {UserId} not found for getting allowed templates", userId);
            return new List<int>();
        }

        // SuperAdmin has access to all templates
        if (user.IsSuperAdmin)
        {
            _logger.LogDebug("User {UserId} is SuperAdmin - returning all templates", userId);
            return await _dbContext.SavedCustomMissions
                .AsNoTracking()
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);
        }

        var allowedTemplateIds = new HashSet<int>();

        // Get templates from user-specific permissions (where CanAccess = true)
        var userPermissionTemplateIds = await _dbContext.UserTemplatePermissions
            .AsNoTracking()
            .Where(up => up.UserId == userId && up.CanAccess)
            .Select(up => up.SavedCustomMissionId)
            .ToListAsync(cancellationToken);

        foreach (var templateId in userPermissionTemplateIds)
        {
            allowedTemplateIds.Add(templateId);
        }

        // Get templates from user-specific permissions that explicitly deny access
        var deniedTemplateIds = await _dbContext.UserTemplatePermissions
            .AsNoTracking()
            .Where(up => up.UserId == userId && !up.CanAccess)
            .Select(up => up.SavedCustomMissionId)
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
                // Get templates from role permissions (where CanAccess = true)
                var rolePermissionTemplateIds = await _dbContext.RoleTemplatePermissions
                    .AsNoTracking()
                    .Where(rtp => roleIds.Contains(rtp.RoleId) && rtp.CanAccess)
                    .Select(rtp => rtp.SavedCustomMissionId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                foreach (var templateId in rolePermissionTemplateIds)
                {
                    // Don't add if explicitly denied by user permission
                    if (!deniedTemplateIds.Contains(templateId))
                    {
                        allowedTemplateIds.Add(templateId);
                    }
                }
            }
        }

        _logger.LogDebug("User {UserId} has access to {Count} templates", userId, allowedTemplateIds.Count);

        return allowedTemplateIds.ToList();
    }
}
