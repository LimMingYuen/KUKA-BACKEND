using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.ResumeStrategies;

public interface IResumeStrategyService
{
    Task<List<ResumeStrategy>> GetAsync(CancellationToken cancellationToken = default);
    Task<ResumeStrategy?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ResumeStrategy> CreateAsync(ResumeStrategy strategy, CancellationToken cancellationToken = default);
    Task<ResumeStrategy?> UpdateAsync(int id, ResumeStrategy strategy, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class ResumeStrategyService : IResumeStrategyService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ResumeStrategyService> _logger;

    public ResumeStrategyService(ApplicationDbContext dbContext, ILogger<ResumeStrategyService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<ResumeStrategy>> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ResumeStrategies
            .AsNoTracking()
            .OrderBy(rs => rs.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<ResumeStrategy?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ResumeStrategies
            .AsNoTracking()
            .FirstOrDefaultAsync(rs => rs.Id == id, cancellationToken);
    }

    public async Task<ResumeStrategy> CreateAsync(ResumeStrategy strategy, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(strategy);

        var exists = await _dbContext.ResumeStrategies
            .AsNoTracking()
            .AnyAsync(rs => rs.ActualValue == normalized.ActualValue, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Cannot create resume strategy. Actual value {ActualValue} already exists.", normalized.ActualValue);
            throw new ResumeStrategyConflictException($"Resume strategy with actual value '{normalized.ActualValue}' already exists.");
        }

        normalized.CreatedUtc = DateTime.UtcNow;
        normalized.UpdatedUtc = normalized.CreatedUtc;

        _dbContext.ResumeStrategies.Add(normalized);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return normalized;
    }

    public async Task<ResumeStrategy?> UpdateAsync(int id, ResumeStrategy strategy, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ResumeStrategies.FirstOrDefaultAsync(rs => rs.Id == id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var normalized = Normalize(strategy);

        var duplicateExists = await _dbContext.ResumeStrategies
            .AsNoTracking()
            .AnyAsync(rs => rs.Id != id && rs.ActualValue == normalized.ActualValue, cancellationToken);

        if (duplicateExists)
        {
            _logger.LogWarning("Cannot update resume strategy {Id}. Actual value {ActualValue} already exists.", id, normalized.ActualValue);
            throw new ResumeStrategyConflictException($"Resume strategy with actual value '{normalized.ActualValue}' already exists.");
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
        var entity = await _dbContext.ResumeStrategies.FirstOrDefaultAsync(rs => rs.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        // Prevent deletion of active resume strategies
        if (entity.IsActive)
        {
            _logger.LogWarning("Cannot delete active resume strategy {Id} ('{DisplayName}'). Set to inactive first.",
                id, entity.DisplayName);
            throw new ResumeStrategyValidationException(
                "Cannot delete an active resume strategy. Please set it to inactive first.");
        }

        _dbContext.ResumeStrategies.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ResumeStrategy Normalize(ResumeStrategy source)
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

public class ResumeStrategyConflictException : Exception
{
    public ResumeStrategyConflictException(string message) : base(message)
    {
    }
}

public class ResumeStrategyValidationException : Exception
{
    public ResumeStrategyValidationException(string message) : base(message)
    {
    }
}
