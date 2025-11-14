using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.MissionTypes;

public interface IMissionTypeService
{
    Task<List<MissionType>> GetAsync(CancellationToken cancellationToken = default);
    Task<MissionType?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<MissionType> CreateAsync(MissionType missionType, CancellationToken cancellationToken = default);
    Task<MissionType?> UpdateAsync(int id, MissionType missionType, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class MissionTypeService : IMissionTypeService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<MissionTypeService> _logger;

    public MissionTypeService(ApplicationDbContext dbContext, ILogger<MissionTypeService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<MissionType>> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionTypes
            .AsNoTracking()
            .OrderBy(mt => mt.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<MissionType?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MissionTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(mt => mt.Id == id, cancellationToken);
    }

    public async Task<MissionType> CreateAsync(MissionType missionType, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeMissionType(missionType);

        var exists = await _dbContext.MissionTypes
            .AsNoTracking()
            .AnyAsync(mt => mt.ActualValue == normalized.ActualValue, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Cannot create mission type. Actual value {ActualValue} already exists.", normalized.ActualValue);
            throw new MissionTypeConflictException($"Mission type with actual value '{normalized.ActualValue}' already exists.");
        }

        normalized.CreatedUtc = DateTime.UtcNow;
        normalized.UpdatedUtc = normalized.CreatedUtc;

        _dbContext.MissionTypes.Add(normalized);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return normalized;
    }

    public async Task<MissionType?> UpdateAsync(int id, MissionType missionType, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.MissionTypes.FirstOrDefaultAsync(mt => mt.Id == id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var normalized = NormalizeMissionType(missionType);

        var duplicateExists = await _dbContext.MissionTypes
            .AsNoTracking()
            .AnyAsync(mt => mt.Id != id && mt.ActualValue == normalized.ActualValue, cancellationToken);

        if (duplicateExists)
        {
            _logger.LogWarning("Cannot update mission type {Id}. Actual value {ActualValue} already exists.", id, normalized.ActualValue);
            throw new MissionTypeConflictException($"Mission type with actual value '{normalized.ActualValue}' already exists.");
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
        var entity = await _dbContext.MissionTypes
            .FirstOrDefaultAsync(mt => mt.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _dbContext.MissionTypes.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static MissionType NormalizeMissionType(MissionType source)
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

public class MissionTypeConflictException : Exception
{
    public MissionTypeConflictException(string message) : base(message)
    {
    }
}
