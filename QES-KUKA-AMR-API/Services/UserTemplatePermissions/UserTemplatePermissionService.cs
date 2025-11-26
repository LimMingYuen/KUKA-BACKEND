using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.UserTemplatePermission;

namespace QES_KUKA_AMR_API.Services.UserTemplatePermissions;

public interface IUserTemplatePermissionService
{
    Task<List<UserTemplatePermission>> GetAsync(CancellationToken cancellationToken = default);
    Task<UserTemplatePermission?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<UserTemplatePermission>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<UserTemplatePermission>> GetByTemplateIdAsync(int templateId, CancellationToken cancellationToken = default);
    Task<UserTemplatePermission> CreateAsync(UserTemplatePermission permission, CancellationToken cancellationToken = default);
    Task<UserTemplatePermission?> UpdateAsync(int id, UserTemplatePermission permission, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<int> BulkSetPermissionsAsync(int userId, List<UserTemplatePermissionSet> permissions, CancellationToken cancellationToken = default);
}

public class UserTemplatePermissionService : IUserTemplatePermissionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UserTemplatePermissionService> _logger;
    private readonly TimeProvider _timeProvider;

    public UserTemplatePermissionService(
        ApplicationDbContext dbContext,
        ILogger<UserTemplatePermissionService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<List<UserTemplatePermission>> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserTemplatePermissions
            .AsNoTracking()
            .Include(utp => utp.User)
            .Include(utp => utp.SavedCustomMission)
            .OrderBy(utp => utp.UserId)
            .ThenBy(utp => utp.SavedCustomMissionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserTemplatePermission?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserTemplatePermissions
            .AsNoTracking()
            .Include(utp => utp.User)
            .Include(utp => utp.SavedCustomMission)
            .FirstOrDefaultAsync(utp => utp.Id == id, cancellationToken);
    }

    public async Task<List<UserTemplatePermission>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserTemplatePermissions
            .AsNoTracking()
            .Include(utp => utp.SavedCustomMission)
            .Where(utp => utp.UserId == userId)
            .OrderBy(utp => utp.SavedCustomMissionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserTemplatePermission>> GetByTemplateIdAsync(int templateId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserTemplatePermissions
            .AsNoTracking()
            .Include(utp => utp.User)
            .Where(utp => utp.SavedCustomMissionId == templateId)
            .OrderBy(utp => utp.UserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserTemplatePermission> CreateAsync(UserTemplatePermission permission, CancellationToken cancellationToken = default)
    {
        // Check if user-template combination already exists
        var exists = await _dbContext.UserTemplatePermissions
            .AsNoTracking()
            .AnyAsync(utp => utp.UserId == permission.UserId && utp.SavedCustomMissionId == permission.SavedCustomMissionId, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Cannot create user template permission. UserId {UserId} and TemplateId {TemplateId} combination already exists.",
                permission.UserId, permission.SavedCustomMissionId);
            throw new UserTemplatePermissionConflictException(
                $"User template permission for UserId '{permission.UserId}' and TemplateId '{permission.SavedCustomMissionId}' already exists.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        permission.CreatedUtc = now;

        _dbContext.UserTemplatePermissions.Add(permission);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return permission;
    }

    public async Task<UserTemplatePermission?> UpdateAsync(int id, UserTemplatePermission permission, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.UserTemplatePermissions.FirstOrDefaultAsync(utp => utp.Id == id, cancellationToken);
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
        var entity = await _dbContext.UserTemplatePermissions.FirstOrDefaultAsync(utp => utp.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _dbContext.UserTemplatePermissions.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> BulkSetPermissionsAsync(
        int userId,
        List<UserTemplatePermissionSet> permissions,
        CancellationToken cancellationToken = default)
    {
        // Get existing permissions for this user
        var existingPermissions = await _dbContext.UserTemplatePermissions
            .Where(utp => utp.UserId == userId)
            .ToListAsync(cancellationToken);

        int modifiedCount = 0;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var permissionSet in permissions)
        {
            var existing = existingPermissions.FirstOrDefault(ep => ep.SavedCustomMissionId == permissionSet.SavedCustomMissionId);

            if (existing is null)
            {
                // Create new permission
                var newPermission = new UserTemplatePermission
                {
                    UserId = userId,
                    SavedCustomMissionId = permissionSet.SavedCustomMissionId,
                    CanAccess = permissionSet.CanAccess,
                    CreatedUtc = now
                };
                _dbContext.UserTemplatePermissions.Add(newPermission);
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
            _logger.LogInformation("Bulk set {Count} template permissions for UserId {UserId}", modifiedCount, userId);
        }

        return modifiedCount;
    }
}

public class UserTemplatePermissionConflictException : Exception
{
    public UserTemplatePermissionConflictException(string message) : base(message)
    {
    }
}
