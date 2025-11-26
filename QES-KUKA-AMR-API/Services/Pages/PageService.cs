using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.Pages;

public interface IPageService
{
    Task<List<Page>> GetAsync(CancellationToken cancellationToken = default);
    Task<Page?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Page?> GetByPathAsync(string pagePath, CancellationToken cancellationToken = default);
    Task<Page> CreateAsync(Page page, CancellationToken cancellationToken = default);
    Task<Page?> UpdateAsync(int id, Page page, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<(int Total, int New, int Updated, int Unchanged)> SyncPagesAsync(List<Page> pages, CancellationToken cancellationToken = default);
}

public class PageService : IPageService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<PageService> _logger;
    private readonly TimeProvider _timeProvider;

    public PageService(
        ApplicationDbContext dbContext,
        ILogger<PageService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<List<Page>> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Pages
            .AsNoTracking()
            .OrderBy(p => p.PagePath)
            .ToListAsync(cancellationToken);
    }

    public async Task<Page?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Pages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Page?> GetByPathAsync(string pagePath, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePagePath(pagePath);
        return await _dbContext.Pages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PagePath == normalizedPath, cancellationToken);
    }

    public async Task<Page> CreateAsync(Page page, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(page);

        var exists = await _dbContext.Pages
            .AsNoTracking()
            .AnyAsync(p => p.PagePath == normalized.PagePath, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Cannot create page. PagePath {PagePath} already exists.", normalized.PagePath);
            throw new PageConflictException($"Page with path '{normalized.PagePath}' already exists.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        normalized.CreatedUtc = now;

        _dbContext.Pages.Add(normalized);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return normalized;
    }

    public async Task<Page?> UpdateAsync(int id, Page page, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Pages.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var normalized = Normalize(page);
        var duplicateExists = await _dbContext.Pages
            .AsNoTracking()
            .AnyAsync(p => p.Id != id && p.PagePath == normalized.PagePath, cancellationToken);

        if (duplicateExists)
        {
            _logger.LogWarning("Cannot update page {Id}. PagePath {PagePath} already exists.", id, normalized.PagePath);
            throw new PageConflictException($"Page with path '{normalized.PagePath}' already exists.");
        }

        existing.PagePath = normalized.PagePath;
        existing.PageName = normalized.PageName;
        existing.PageIcon = normalized.PageIcon;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Pages.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _dbContext.Pages.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(int Total, int New, int Updated, int Unchanged)> SyncPagesAsync(
        List<Page> pages,
        CancellationToken cancellationToken = default)
    {
        int newCount = 0;
        int updatedCount = 0;
        int unchangedCount = 0;

        foreach (var page in pages)
        {
            var normalized = Normalize(page);
            var existing = await _dbContext.Pages
                .FirstOrDefaultAsync(p => p.PagePath == normalized.PagePath, cancellationToken);

            if (existing is null)
            {
                // Create new page
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                normalized.CreatedUtc = now;
                _dbContext.Pages.Add(normalized);
                newCount++;
                _logger.LogInformation("Creating new page: {PagePath}", normalized.PagePath);
            }
            else
            {
                // Check if update is needed
                bool needsUpdate = existing.PageName != normalized.PageName ||
                                   existing.PageIcon != normalized.PageIcon;

                if (needsUpdate)
                {
                    existing.PageName = normalized.PageName;
                    existing.PageIcon = normalized.PageIcon;
                    updatedCount++;
                    _logger.LogInformation("Updating existing page: {PagePath}", normalized.PagePath);
                }
                else
                {
                    unchangedCount++;
                }
            }
        }

        if (newCount > 0 || updatedCount > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        int total = pages.Count;
        _logger.LogInformation(
            "Page sync completed. Total: {Total}, New: {New}, Updated: {Updated}, Unchanged: {Unchanged}",
            total, newCount, updatedCount, unchangedCount);

        return (total, newCount, updatedCount, unchangedCount);
    }

    private static Page Normalize(Page source)
    {
        source.PagePath = NormalizePagePath(source.PagePath);
        source.PageName = source.PageName?.Trim() ?? string.Empty;
        source.PageIcon = source.PageIcon?.Trim();

        return source;
    }

    private static string NormalizePagePath(string pagePath)
    {
        return (pagePath ?? string.Empty).Trim().ToLowerInvariant();
    }
}

public class PageConflictException : Exception
{
    public PageConflictException(string message) : base(message)
    {
    }
}
