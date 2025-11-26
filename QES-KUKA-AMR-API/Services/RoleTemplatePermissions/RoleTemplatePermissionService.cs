using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.RoleTemplatePermission;

namespace QES_KUKA_AMR_API.Services.RoleTemplatePermissions;

public interface IRoleTemplatePermissionService
{
    Task<List<RoleTemplatePermission>> GetAsync(CancellationToken cancellationToken = default);
    Task<RoleTemplatePermission?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<RoleTemplatePermission>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default);
    Task<List<RoleTemplatePermission>> GetByTemplateIdAsync(int templateId, CancellationToken cancellationToken = default);
    Task<RoleTemplatePermission> CreateAsync(RoleTemplatePermission permission, CancellationToken cancellationToken = default);
    Task<RoleTemplatePermission?> UpdateAsync(int id, RoleTemplatePermission permission, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<int> BulkSetPermissionsAsync(int roleId, List<RoleTemplatePermissionSet> permissions, CancellationToken cancellationToken = default);
    Task<RoleTemplatePermissionMatrix> GetPermissionMatrixAsync(CancellationToken cancellationToken = default);
}

public class RoleTemplatePermissionService : IRoleTemplatePermissionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RoleTemplatePermissionService> _logger;
    private readonly TimeProvider _timeProvider;

    public RoleTemplatePermissionService(
        ApplicationDbContext dbContext,
        ILogger<RoleTemplatePermissionService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<List<RoleTemplatePermission>> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.RoleTemplatePermissions
            .AsNoTracking()
            .Include(rtp => rtp.Role)
            .Include(rtp => rtp.SavedCustomMission)
            .OrderBy(rtp => rtp.RoleId)
            .ThenBy(rtp => rtp.SavedCustomMissionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<RoleTemplatePermission?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RoleTemplatePermissions
            .AsNoTracking()
            .Include(rtp => rtp.Role)
            .Include(rtp => rtp.SavedCustomMission)
            .FirstOrDefaultAsync(rtp => rtp.Id == id, cancellationToken);
    }

    public async Task<List<RoleTemplatePermission>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RoleTemplatePermissions
            .AsNoTracking()
            .Include(rtp => rtp.SavedCustomMission)
            .Where(rtp => rtp.RoleId == roleId)
            .OrderBy(rtp => rtp.SavedCustomMissionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RoleTemplatePermission>> GetByTemplateIdAsync(int templateId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RoleTemplatePermissions
            .AsNoTracking()
            .Include(rtp => rtp.Role)
            .Where(rtp => rtp.SavedCustomMissionId == templateId)
            .OrderBy(rtp => rtp.RoleId)
            .ToListAsync(cancellationToken);
    }

    public async Task<RoleTemplatePermission> CreateAsync(RoleTemplatePermission permission, CancellationToken cancellationToken = default)
    {
        // Check if role-template combination already exists
        var exists = await _dbContext.RoleTemplatePermissions
            .AsNoTracking()
            .AnyAsync(rtp => rtp.RoleId == permission.RoleId && rtp.SavedCustomMissionId == permission.SavedCustomMissionId, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Cannot create role template permission. RoleId {RoleId} and TemplateId {TemplateId} combination already exists.",
                permission.RoleId, permission.SavedCustomMissionId);
            throw new RoleTemplatePermissionConflictException(
                $"Role template permission for RoleId '{permission.RoleId}' and TemplateId '{permission.SavedCustomMissionId}' already exists.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        permission.CreatedUtc = now;

        _dbContext.RoleTemplatePermissions.Add(permission);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return permission;
    }

    public async Task<RoleTemplatePermission?> UpdateAsync(int id, RoleTemplatePermission permission, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.RoleTemplatePermissions.FirstOrDefaultAsync(rtp => rtp.Id == id, cancellationToken);
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
        var entity = await _dbContext.RoleTemplatePermissions.FirstOrDefaultAsync(rtp => rtp.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _dbContext.RoleTemplatePermissions.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> BulkSetPermissionsAsync(
        int roleId,
        List<RoleTemplatePermissionSet> permissions,
        CancellationToken cancellationToken = default)
    {
        // Get existing permissions for this role
        var existingPermissions = await _dbContext.RoleTemplatePermissions
            .Where(rtp => rtp.RoleId == roleId)
            .ToListAsync(cancellationToken);

        int modifiedCount = 0;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var permissionSet in permissions)
        {
            var existing = existingPermissions.FirstOrDefault(ep => ep.SavedCustomMissionId == permissionSet.SavedCustomMissionId);

            if (existing is null)
            {
                // Create new permission
                var newPermission = new RoleTemplatePermission
                {
                    RoleId = roleId,
                    SavedCustomMissionId = permissionSet.SavedCustomMissionId,
                    CanAccess = permissionSet.CanAccess,
                    CreatedUtc = now
                };
                _dbContext.RoleTemplatePermissions.Add(newPermission);
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
            _logger.LogInformation("Bulk set {Count} template permissions for RoleId {RoleId}", modifiedCount, roleId);
        }

        return modifiedCount;
    }

    public async Task<RoleTemplatePermissionMatrix> GetPermissionMatrixAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new TemplateRoleInfo
            {
                Id = r.Id,
                Name = r.Name,
                RoleCode = r.RoleCode
            })
            .ToListAsync(cancellationToken);

        var templates = await _dbContext.SavedCustomMissions
            .AsNoTracking()
            .OrderBy(m => m.MissionName)
            .Select(m => new TemplateInfo
            {
                Id = m.Id,
                MissionName = m.MissionName,
                Description = m.Description
            })
            .ToListAsync(cancellationToken);

        var permissions = await _dbContext.RoleTemplatePermissions
            .AsNoTracking()
            .Where(rtp => rtp.CanAccess) // Only include granted permissions
            .ToListAsync(cancellationToken);

        var permissionDict = new Dictionary<string, bool>();
        foreach (var permission in permissions)
        {
            var key = $"{permission.RoleId}_{permission.SavedCustomMissionId}";
            permissionDict[key] = permission.CanAccess;
        }

        return new RoleTemplatePermissionMatrix
        {
            Roles = roles,
            Templates = templates,
            Permissions = permissionDict
        };
    }
}

public class RoleTemplatePermissionConflictException : Exception
{
    public RoleTemplatePermissionConflictException(string message) : base(message)
    {
    }
}
