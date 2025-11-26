using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.UserPermission;

namespace QES_KUKA_AMR_API.Services.UserPermissions;

public interface IUserPermissionService
{
    Task<List<UserPermission>> GetAsync(CancellationToken cancellationToken = default);
    Task<UserPermission?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<UserPermission>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<UserPermission>> GetByPageIdAsync(int pageId, CancellationToken cancellationToken = default);
    Task<UserPermission> CreateAsync(UserPermission permission, CancellationToken cancellationToken = default);
    Task<UserPermission?> UpdateAsync(int id, UserPermission permission, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<int> BulkSetPermissionsAsync(int userId, List<UserPagePermissionSet> permissions, CancellationToken cancellationToken = default);
}

public class UserPermissionService : IUserPermissionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UserPermissionService> _logger;
    private readonly TimeProvider _timeProvider;

    public UserPermissionService(
        ApplicationDbContext dbContext,
        ILogger<UserPermissionService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<List<UserPermission>> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserPermissions
            .AsNoTracking()
            .Include(up => up.User)
            .Include(up => up.Page)
            .OrderBy(up => up.UserId)
            .ThenBy(up => up.PageId)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserPermission?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserPermissions
            .AsNoTracking()
            .Include(up => up.User)
            .Include(up => up.Page)
            .FirstOrDefaultAsync(up => up.Id == id, cancellationToken);
    }

    public async Task<List<UserPermission>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserPermissions
            .AsNoTracking()
            .Include(up => up.Page)
            .Where(up => up.UserId == userId)
            .OrderBy(up => up.PageId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserPermission>> GetByPageIdAsync(int pageId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserPermissions
            .AsNoTracking()
            .Include(up => up.User)
            .Where(up => up.PageId == pageId)
            .OrderBy(up => up.UserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserPermission> CreateAsync(UserPermission permission, CancellationToken cancellationToken = default)
    {
        // Check if user-page combination already exists
        var exists = await _dbContext.UserPermissions
            .AsNoTracking()
            .AnyAsync(up => up.UserId == permission.UserId && up.PageId == permission.PageId, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Cannot create user permission. UserId {UserId} and PageId {PageId} combination already exists.",
                permission.UserId, permission.PageId);
            throw new UserPermissionConflictException(
                $"User permission for UserId '{permission.UserId}' and PageId '{permission.PageId}' already exists.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        permission.CreatedUtc = now;

        _dbContext.UserPermissions.Add(permission);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return permission;
    }

    public async Task<UserPermission?> UpdateAsync(int id, UserPermission permission, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.UserPermissions.FirstOrDefaultAsync(up => up.Id == id, cancellationToken);
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
        var entity = await _dbContext.UserPermissions.FirstOrDefaultAsync(up => up.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _dbContext.UserPermissions.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> BulkSetPermissionsAsync(
        int userId,
        List<UserPagePermissionSet> permissions,
        CancellationToken cancellationToken = default)
    {
        // Get existing permissions for this user
        var existingPermissions = await _dbContext.UserPermissions
            .Where(up => up.UserId == userId)
            .ToListAsync(cancellationToken);

        int modifiedCount = 0;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var permissionSet in permissions)
        {
            var existing = existingPermissions.FirstOrDefault(ep => ep.PageId == permissionSet.PageId);

            if (existing is null)
            {
                // Create new permission
                var newPermission = new UserPermission
                {
                    UserId = userId,
                    PageId = permissionSet.PageId,
                    CanAccess = permissionSet.CanAccess,
                    CreatedUtc = now
                };
                _dbContext.UserPermissions.Add(newPermission);
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
            _logger.LogInformation("Bulk set {Count} permissions for UserId {UserId}", modifiedCount, userId);
        }

        return modifiedCount;
    }
}

public class UserPermissionConflictException : Exception
{
    public UserPermissionConflictException(string message) : base(message)
    {
    }
}
