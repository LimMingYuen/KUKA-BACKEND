using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.TemplateCategories;

public interface ITemplateCategoryService
{
    Task<List<TemplateCategory>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TemplateCategory?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TemplateCategory> CreateAsync(TemplateCategory category, CancellationToken cancellationToken = default);
    Task<TemplateCategory?> UpdateAsync(int id, TemplateCategory category, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<Dictionary<int, int>> GetTemplateCountsAsync(CancellationToken cancellationToken = default);
}

public class TemplateCategoryService : ITemplateCategoryService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TemplateCategoryService> _logger;
    private readonly TimeProvider _timeProvider;

    public TemplateCategoryService(
        ApplicationDbContext dbContext,
        ILogger<TemplateCategoryService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Get all template categories ordered by DisplayOrder then Name
    /// </summary>
    public async Task<List<TemplateCategory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.TemplateCategories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get a template category by ID
    /// </summary>
    public async Task<TemplateCategory?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TemplateCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <summary>
    /// Create a new template category
    /// </summary>
    public async Task<TemplateCategory> CreateAsync(TemplateCategory category, CancellationToken cancellationToken = default)
    {
        var normalizedName = category.Name?.Trim() ?? string.Empty;

        // Check for duplicate name
        var exists = await _dbContext.TemplateCategories
            .AsNoTracking()
            .AnyAsync(c => c.Name == normalizedName, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Cannot create category. Name '{Name}' already exists.", normalizedName);
            throw new TemplateCategoryConflictException($"Category with name '{normalizedName}' already exists.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        category.Name = normalizedName;
        category.Description = category.Description?.Trim();
        category.CreatedUtc = now;
        category.UpdatedUtc = now;

        _dbContext.TemplateCategories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created template category '{Name}' with ID {Id}", category.Name, category.Id);

        return category;
    }

    /// <summary>
    /// Update an existing template category
    /// </summary>
    public async Task<TemplateCategory?> UpdateAsync(int id, TemplateCategory category, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.TemplateCategories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (existing is null)
        {
            return null;
        }

        var normalizedName = category.Name?.Trim() ?? string.Empty;

        // Check for duplicate name (excluding self)
        var duplicateExists = await _dbContext.TemplateCategories
            .AsNoTracking()
            .AnyAsync(c => c.Id != id && c.Name == normalizedName, cancellationToken);

        if (duplicateExists)
        {
            _logger.LogWarning("Cannot update category {Id}. Name '{Name}' already exists.", id, normalizedName);
            throw new TemplateCategoryConflictException($"Category with name '{normalizedName}' already exists.");
        }

        existing.Name = normalizedName;
        existing.Description = category.Description?.Trim();
        existing.DisplayOrder = category.DisplayOrder;
        existing.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated template category {Id} to name '{Name}'", id, existing.Name);

        return existing;
    }

    /// <summary>
    /// Delete a template category. Templates in this category become Uncategorized.
    /// </summary>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TemplateCategories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        // Templates in this category will have CategoryId set to null automatically
        // due to ON DELETE SET NULL in the FK relationship
        _dbContext.TemplateCategories.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted template category {Id} '{Name}'. Templates moved to Uncategorized.", id, entity.Name);

        return true;
    }

    /// <summary>
    /// Get template counts per category
    /// </summary>
    public async Task<Dictionary<int, int>> GetTemplateCountsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SavedCustomMissions
            .Where(m => m.CategoryId != null)
            .GroupBy(m => m.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, cancellationToken);
    }
}

/// <summary>
/// Exception thrown when a category with the same name already exists
/// </summary>
public class TemplateCategoryConflictException : Exception
{
    public TemplateCategoryConflictException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when category validation fails
/// </summary>
public class TemplateCategoryValidationException : Exception
{
    public TemplateCategoryValidationException(string message) : base(message) { }
}
