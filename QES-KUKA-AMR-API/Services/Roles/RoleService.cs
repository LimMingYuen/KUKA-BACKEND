using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.Roles;

public interface IRoleService
{
    Task<List<Role>> GetAsync(CancellationToken cancellationToken = default);
    Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Role?> GetByRoleCodeAsync(string roleCode, CancellationToken cancellationToken = default);
    Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default);
    Task<Role?> UpdateAsync(int id, Role role, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class RoleService : IRoleService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RoleService> _logger;
    private readonly TimeProvider _timeProvider;

    public RoleService(
        ApplicationDbContext dbContext,
        ILogger<RoleService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<List<Role>> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Role?> GetByRoleCodeAsync(string roleCode, CancellationToken cancellationToken = default)
    {
        var normalizedRoleCode = NormalizeRoleCode(roleCode);
        return await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoleCode == normalizedRoleCode, cancellationToken);
    }

    public async Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(role);

        var exists = await _dbContext.Roles
            .AsNoTracking()
            .AnyAsync(r => r.RoleCode == normalized.RoleCode, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Cannot create role. RoleCode {RoleCode} already exists.", normalized.RoleCode);
            throw new RoleConflictException($"Role with code '{normalized.RoleCode}' already exists.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        normalized.CreatedUtc = now;
        normalized.UpdatedUtc = now;

        _dbContext.Roles.Add(normalized);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return normalized;
    }

    public async Task<Role?> UpdateAsync(int id, Role role, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var normalized = Normalize(role);
        var duplicateExists = await _dbContext.Roles
            .AsNoTracking()
            .AnyAsync(r => r.Id != id && r.RoleCode == normalized.RoleCode, cancellationToken);

        if (duplicateExists)
        {
            _logger.LogWarning("Cannot update role {Id}. RoleCode {RoleCode} already exists.", id, normalized.RoleCode);
            throw new RoleConflictException($"Role with code '{normalized.RoleCode}' already exists.");
        }

        existing.Name = normalized.Name;
        existing.RoleCode = normalized.RoleCode;
        existing.IsProtected = normalized.IsProtected;
        existing.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        if (entity.IsProtected)
        {
            _logger.LogWarning("Cannot delete protected role {Id} with code {RoleCode}.", id, entity.RoleCode);
            throw new RoleProtectedException($"Cannot delete protected role '{entity.RoleCode}'.");
        }

        _dbContext.Roles.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static Role Normalize(Role source)
    {
        source.Name = source.Name?.Trim() ?? string.Empty;
        source.RoleCode = NormalizeRoleCode(source.RoleCode);

        return source;
    }

    private static string NormalizeRoleCode(string roleCode)
    {
        return (roleCode ?? string.Empty)
            .Trim()
            .Replace(' ', '_')
            .ToUpperInvariant();
    }
}

public class RoleConflictException : Exception
{
    public RoleConflictException(string message) : base(message)
    {
    }
}

public class RoleProtectedException : Exception
{
    public RoleProtectedException(string message) : base(message)
    {
    }
}
