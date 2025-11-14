using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.RobotTypes;

public interface IRobotTypeService
{
    Task<List<RobotType>> GetAsync(CancellationToken cancellationToken = default);
    Task<RobotType?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<RobotType> CreateAsync(RobotType robotType, CancellationToken cancellationToken = default);
    Task<RobotType?> UpdateAsync(int id, RobotType robotType, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class RobotTypeService : IRobotTypeService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RobotTypeService> _logger;

    public RobotTypeService(ApplicationDbContext dbContext, ILogger<RobotTypeService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<RobotType>> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.RobotTypes
            .AsNoTracking()
            .OrderBy(rt => rt.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<RobotType?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RobotTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Id == id, cancellationToken);
    }

    public async Task<RobotType> CreateAsync(RobotType robotType, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRobotType(robotType);

        var exists = await _dbContext.RobotTypes
            .AsNoTracking()
            .AnyAsync(rt => rt.ActualValue == normalized.ActualValue, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Cannot create robot type. Actual value {ActualValue} already exists.", normalized.ActualValue);
            throw new RobotTypeConflictException($"Robot type with actual value '{normalized.ActualValue}' already exists.");
        }

        normalized.CreatedUtc = DateTime.UtcNow;
        normalized.UpdatedUtc = normalized.CreatedUtc;

        _dbContext.RobotTypes.Add(normalized);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return normalized;
    }

    public async Task<RobotType?> UpdateAsync(int id, RobotType robotType, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.RobotTypes.FirstOrDefaultAsync(rt => rt.Id == id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var normalized = NormalizeRobotType(robotType);

        var duplicateExists = await _dbContext.RobotTypes
            .AsNoTracking()
            .AnyAsync(rt => rt.Id != id && rt.ActualValue == normalized.ActualValue, cancellationToken);

        if (duplicateExists)
        {
            _logger.LogWarning("Cannot update robot type {Id}. Actual value {ActualValue} already exists.", id, normalized.ActualValue);
            throw new RobotTypeConflictException($"Robot type with actual value '{normalized.ActualValue}' already exists.");
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
        var entity = await _dbContext.RobotTypes
            .FirstOrDefaultAsync(rt => rt.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _dbContext.RobotTypes.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static RobotType NormalizeRobotType(RobotType source)
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

public class RobotTypeConflictException : Exception
{
    public RobotTypeConflictException(string message) : base(message)
    {
    }
}
