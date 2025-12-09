using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.OrganizationIds;

public interface IOrganizationIdService
{
    Task<List<OrganizationId>> GetAsync(CancellationToken cancellationToken = default);
    Task<OrganizationId?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OrganizationId> CreateAsync(OrganizationId organizationId, CancellationToken cancellationToken = default);
    Task<OrganizationId?> UpdateAsync(int id, OrganizationId organizationId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<OrganizationId?> ToggleStatusAsync(int id, CancellationToken cancellationToken = default);
}

public class OrganizationIdService : IOrganizationIdService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<OrganizationIdService> _logger;

    public OrganizationIdService(ApplicationDbContext dbContext, ILogger<OrganizationIdService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<OrganizationId>> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrganizationIds
            .AsNoTracking()
            .OrderBy(o => o.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrganizationId?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrganizationIds
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<OrganizationId> CreateAsync(OrganizationId organizationId, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeOrganizationId(organizationId);

        var exists = await _dbContext.OrganizationIds
            .AsNoTracking()
            .AnyAsync(o => o.ActualValue == normalized.ActualValue, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Cannot create organization ID. Actual value {ActualValue} already exists.", normalized.ActualValue);
            throw new OrganizationIdConflictException($"Organization ID with actual value '{normalized.ActualValue}' already exists.");
        }

        normalized.CreatedUtc = DateTime.UtcNow;
        normalized.UpdatedUtc = normalized.CreatedUtc;

        _dbContext.OrganizationIds.Add(normalized);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return normalized;
    }

    public async Task<OrganizationId?> UpdateAsync(int id, OrganizationId organizationId, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.OrganizationIds.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var normalized = NormalizeOrganizationId(organizationId);

        var duplicateExists = await _dbContext.OrganizationIds
            .AsNoTracking()
            .AnyAsync(o => o.Id != id && o.ActualValue == normalized.ActualValue, cancellationToken);

        if (duplicateExists)
        {
            _logger.LogWarning("Cannot update organization ID {Id}. Actual value {ActualValue} already exists.", id, normalized.ActualValue);
            throw new OrganizationIdConflictException($"Organization ID with actual value '{normalized.ActualValue}' already exists.");
        }

        existing.DisplayName = normalized.DisplayName;
        existing.ActualValue = normalized.ActualValue;
        existing.Description = normalized.Description;
        existing.IsActive = normalized.IsActive;
        existing.UpdatedUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.OrganizationIds
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        // Prevent deletion of active organization IDs
        if (entity.IsActive)
        {
            _logger.LogWarning("Cannot delete active organization ID {Id} ('{DisplayName}'). Set to inactive first.",
                id, entity.DisplayName);
            throw new OrganizationIdValidationException(
                "Cannot delete an active organization ID. Please set it to inactive first.");
        }

        _dbContext.OrganizationIds.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<OrganizationId?> ToggleStatusAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.OrganizationIds
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.IsActive = !entity.IsActive;
        entity.UpdatedUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static OrganizationId NormalizeOrganizationId(OrganizationId source)
    {
        source.DisplayName = source.DisplayName?.Trim() ?? string.Empty;
        source.ActualValue = NormalizeActualValue(source.ActualValue);

        if (!string.IsNullOrWhiteSpace(source.Description))
        {
            source.Description = source.Description.Trim();
        }

        return source;
    }

    private static string NormalizeActualValue(string actualValue)
    {
        return (actualValue ?? string.Empty)
            .Trim()
            .Replace(' ', '_')
            .ToUpperInvariant();
    }
}

public class OrganizationIdConflictException : Exception
{
    public OrganizationIdConflictException(string message) : base(message)
    {
    }
}

public class OrganizationIdValidationException : Exception
{
    public OrganizationIdValidationException(string message) : base(message)
    {
    }
}
