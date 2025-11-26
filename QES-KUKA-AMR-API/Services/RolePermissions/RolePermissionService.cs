using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.RolePermission;

namespace QES_KUKA_AMR_API.Services.RolePermissions;

public interface IRolePermissionService
{
    Task<List<RolePermission>> GetAsync(CancellationToken cancellationToken = default);
    Task<RolePermission?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<RolePermission>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default);
    Task<List<RolePermission>> GetByPageIdAsync(int pageId, CancellationToken cancellationToken = default);
    Task<RolePermission> CreateAsync(RolePermission permission, CancellationToken cancellationToken = default);
    Task<RolePermission?> UpdateAsync(int id, RolePermission permission, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<int> BulkSetPermissionsAsync(int roleId, List<RolePagePermissionSet> permissions, CancellationToken cancellationToken = default);
    Task<RolePermissionMatrix> GetPermissionMatrixAsync(CancellationToken cancellationToken = default);
}

public class RolePermissionService : IRolePermissionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RolePermissionService> _logger;
    private readonly TimeProvider _timeProvider;

    public RolePermissionService(
        ApplicationDbContext dbContext,
        ILogger<RolePermissionService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<List<RolePermission>> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.RolePermissions
            .AsNoTracking()
            .Include(rp => rp.Role)
            .Include(rp => rp.Page)
            .OrderBy(rp => rp.RoleId)
            .ThenBy(rp => rp.PageId)
            .ToListAsync(cancellationToken);
    }

    public async Task<RolePermission?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RolePermissions
            .AsNoTracking()
            .Include(rp => rp.Role)
            .Include(rp => rp.Page)
            .FirstOrDefaultAsync(rp => rp.Id == id, cancellationToken);
    }

    public async Task<List<RolePermission>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RolePermissions
            .AsNoTracking()
            .Include(rp => rp.Page)
            .Where(rp => rp.RoleId == roleId)
            .OrderBy(rp => rp.PageId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RolePermission>> GetByPageIdAsync(int pageId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RolePermissions
            .AsNoTracking()
            .Include(rp => rp.Role)
            .Where(rp => rp.PageId == pageId)
            .OrderBy(rp => rp.RoleId)
            .ToListAsync(cancellationToken);
    }

    public async Task<RolePermission> CreateAsync(RolePermission permission, CancellationToken cancellationToken = default)
    {
        // Check if role-page combination already exists
        var exists = await _dbContext.RolePermissions
            .AsNoTracking()
            .AnyAsync(rp => rp.RoleId == permission.RoleId && rp.PageId == permission.PageId, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Cannot create role permission. RoleId {RoleId} and PageId {PageId} combination already exists.",
                permission.RoleId, permission.PageId);
            throw new RolePermissionConflictException(
                $"Role permission for RoleId '{permission.RoleId}' and PageId '{permission.PageId}' already exists.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        permission.CreatedUtc = now;

        _dbContext.RolePermissions.Add(permission);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return permission;
    }

    public async Task<RolePermission?> UpdateAsync(int id, RolePermission permission, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.RolePermissions.FirstOrDefaultAsync(rp => rp.Id == id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        existing.CanAccess = permission.CanAccess;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.RolePermissions.FirstOrDefaultAsync(rp => rp.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _dbContext.RolePermissions.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> BulkSetPermissionsAsync(
        int roleId,
        List<RolePagePermissionSet> permissions,
        CancellationToken cancellationToken = default)
    {
        // Get existing permissions for this role
        var existingPermissions = await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);

        int modifiedCount = 0;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var permissionSet in permissions)
        {
            var existing = existingPermissions.FirstOrDefault(ep => ep.PageId == permissionSet.PageId);

            if (existing is null)
            {
                // Create new permission
                var newPermission = new RolePermission
                {
                    RoleId = roleId,
                    PageId = permissionSet.PageId,
                    CanAccess = permissionSet.CanAccess,
                    CreatedUtc = now
                };
                _dbContext.RolePermissions.Add(newPermission);
                modifiedCount++;
            }
            else if (existing.CanAccess != permissionSet.CanAccess)
            {
                // Update existing permission
                existing.CanAccess = permissionSet.CanAccess;
                modifiedCount++;
            }
        }

        if (modifiedCount > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Bulk set {Count} permissions for RoleId {RoleId}", modifiedCount, roleId);
        }

        return modifiedCount;
    }

    public async Task<RolePermissionMatrix> GetPermissionMatrixAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleInfo
            {
                Id = r.Id,
                Name = r.Name,
                RoleCode = r.RoleCode
            })
            .ToListAsync(cancellationToken);

        var pages = await _dbContext.Pages
            .AsNoTracking()
            .OrderBy(p => p.PagePath)
            .Select(p => new PageInfo
            {
                Id = p.Id,
                PagePath = p.PagePath,
                PageName = p.PageName
            })
            .ToListAsync(cancellationToken);

        var permissions = await _dbContext.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.CanAccess) // Only include granted permissions
            .ToListAsync(cancellationToken);

        var permissionDict = new Dictionary<string, bool>();
        foreach (var permission in permissions)
        {
            var key = $"{permission.RoleId}_{permission.PageId}";
            permissionDict[key] = permission.CanAccess;
        }

        return new RolePermissionMatrix
        {
            Roles = roles,
            Pages = pages,
            Permissions = permissionDict
        };
    }
}

public class RolePermissionConflictException : Exception
{
    public RolePermissionConflictException(string message) : base(message)
    {
    }
}
