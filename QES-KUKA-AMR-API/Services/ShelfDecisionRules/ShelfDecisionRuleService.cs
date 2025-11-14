using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.ShelfDecisionRules;

public interface IShelfDecisionRuleService
{
    Task<List<ShelfDecisionRule>> GetAsync(CancellationToken cancellationToken = default);
    Task<ShelfDecisionRule?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ShelfDecisionRule> CreateAsync(ShelfDecisionRule rule, CancellationToken cancellationToken = default);
    Task<ShelfDecisionRule?> UpdateAsync(int id, ShelfDecisionRule rule, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class ShelfDecisionRuleService : IShelfDecisionRuleService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ShelfDecisionRuleService> _logger;

    public ShelfDecisionRuleService(ApplicationDbContext dbContext, ILogger<ShelfDecisionRuleService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<ShelfDecisionRule>> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShelfDecisionRules
            .AsNoTracking()
            .OrderBy(rule => rule.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<ShelfDecisionRule?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShelfDecisionRules
            .AsNoTracking()
            .FirstOrDefaultAsync(rule => rule.Id == id, cancellationToken);
    }

    public async Task<ShelfDecisionRule> CreateAsync(ShelfDecisionRule rule, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(rule);

        var exists = await _dbContext.ShelfDecisionRules
            .AsNoTracking()
            .AnyAsync(r => r.ActualValue == normalized.ActualValue, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Cannot create shelf decision rule. Actual value {ActualValue} already exists.", normalized.ActualValue);
            throw new ShelfDecisionRuleConflictException($"Shelf decision rule with actual value '{normalized.ActualValue}' already exists.");
        }

        normalized.CreatedUtc = DateTime.UtcNow;
        normalized.UpdatedUtc = normalized.CreatedUtc;

        _dbContext.ShelfDecisionRules.Add(normalized);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return normalized;
    }

    public async Task<ShelfDecisionRule?> UpdateAsync(int id, ShelfDecisionRule rule, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ShelfDecisionRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var normalized = Normalize(rule);
        var duplicateExists = await _dbContext.ShelfDecisionRules
            .AsNoTracking()
            .AnyAsync(r => r.Id != id && r.ActualValue == normalized.ActualValue, cancellationToken);

        if (duplicateExists)
        {
            _logger.LogWarning("Cannot update shelf decision rule {Id}. Actual value {ActualValue} already exists.", id, normalized.ActualValue);
            throw new ShelfDecisionRuleConflictException($"Shelf decision rule with actual value '{normalized.ActualValue}' already exists.");
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
        var entity = await _dbContext.ShelfDecisionRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _dbContext.ShelfDecisionRules.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ShelfDecisionRule Normalize(ShelfDecisionRule source)
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

public class ShelfDecisionRuleConflictException : Exception
{
    public ShelfDecisionRuleConflictException(string message) : base(message)
    {
    }
}
