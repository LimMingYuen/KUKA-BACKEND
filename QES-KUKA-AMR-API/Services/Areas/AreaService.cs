using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.Areas;

public interface IAreaService
{
    Task<List<Area>> GetAsync(CancellationToken cancellationToken = default);
    Task<Area?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Area> CreateAsync(Area area, CancellationToken cancellationToken = default);
    Task<Area?> UpdateAsync(int id, Area area, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class AreaService : IAreaService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AreaService> _logger;

    public AreaService(ApplicationDbContext dbContext, ILogger<AreaService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<Area>> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Areas
            .AsNoTracking()
            .OrderBy(a => a.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Area?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Areas
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Area> CreateAsync(Area area, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(area);

        var exists = await _dbContext.Areas
            .AsNoTracking()
            .AnyAsync(a => a.ActualValue == normalized.ActualValue, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Cannot create area. Actual value {ActualValue} already exists.", normalized.ActualValue);
            throw new AreaConflictException($"Area with actual value '{normalized.ActualValue}' already exists.");
        }

        normalized.CreatedUtc = DateTime.UtcNow;
        normalized.UpdatedUtc = normalized.CreatedUtc;

        _dbContext.Areas.Add(normalized);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return normalized;
    }

    public async Task<Area?> UpdateAsync(int id, Area area, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Areas.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var normalized = Normalize(area);
        var duplicateExists = await _dbContext.Areas
            .AsNoTracking()
            .AnyAsync(a => a.Id != id && a.ActualValue == normalized.ActualValue, cancellationToken);

        if (duplicateExists)
        {
            _logger.LogWarning("Cannot update area {Id}. Actual value {ActualValue} already exists.", id, normalized.ActualValue);
            throw new AreaConflictException($"Area with actual value '{normalized.ActualValue}' already exists.");
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
        var entity = await _dbContext.Areas.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _dbContext.Areas.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static Area Normalize(Area source)
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

public class AreaConflictException : Exception
{
    public AreaConflictException(string message) : base(message)
    {
    }
}
