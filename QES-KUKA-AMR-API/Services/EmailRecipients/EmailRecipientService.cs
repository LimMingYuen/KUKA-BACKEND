using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.EmailRecipients;

/// <summary>
/// Service interface for managing email recipients.
/// </summary>
public interface IEmailRecipientService
{
    Task<List<EmailRecipient>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EmailRecipient?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<EmailRecipient> CreateAsync(EmailRecipient recipient, string? createdBy = null, CancellationToken cancellationToken = default);
    Task<EmailRecipient?> UpdateAsync(int id, EmailRecipient recipient, string? updatedBy = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service implementation for managing email recipients.
/// </summary>
public class EmailRecipientService : IEmailRecipientService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<EmailRecipientService> _logger;
    private readonly TimeProvider _timeProvider;

    public EmailRecipientService(
        ApplicationDbContext dbContext,
        ILogger<EmailRecipientService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<List<EmailRecipient>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.EmailRecipients
            .AsNoTracking()
            .OrderBy(r => r.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EmailRecipient?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EmailRecipients
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EmailRecipient> CreateAsync(
        EmailRecipient recipient,
        string? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(recipient);

        // Check for duplicate email address
        var exists = await _dbContext.EmailRecipients
            .AsNoTracking()
            .AnyAsync(r => r.EmailAddress.ToLower() == normalized.EmailAddress.ToLower(), cancellationToken);

        if (exists)
        {
            throw new EmailRecipientConflictException(
                $"Email recipient with address '{normalized.EmailAddress}' already exists.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        normalized.CreatedUtc = now;
        normalized.UpdatedUtc = now;
        normalized.CreatedBy = createdBy;
        normalized.UpdatedBy = createdBy;

        _dbContext.EmailRecipients.Add(normalized);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Email recipient created: {Id} - {Email} by {User}",
            normalized.Id, normalized.EmailAddress, createdBy ?? "system");

        return normalized;
    }

    /// <inheritdoc />
    public async Task<EmailRecipient?> UpdateAsync(
        int id,
        EmailRecipient recipient,
        string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.EmailRecipients
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (existing is null)
        {
            return null;
        }

        var normalized = Normalize(recipient);

        // Check for duplicate email address (excluding current record)
        var duplicateExists = await _dbContext.EmailRecipients
            .AsNoTracking()
            .AnyAsync(r => r.Id != id && r.EmailAddress.ToLower() == normalized.EmailAddress.ToLower(), cancellationToken);

        if (duplicateExists)
        {
            throw new EmailRecipientConflictException(
                $"Email recipient with address '{normalized.EmailAddress}' already exists.");
        }

        existing.EmailAddress = normalized.EmailAddress;
        existing.DisplayName = normalized.DisplayName;
        existing.Description = normalized.Description;
        existing.NotificationTypes = normalized.NotificationTypes;
        existing.IsActive = normalized.IsActive;
        existing.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        existing.UpdatedBy = updatedBy;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Email recipient updated: {Id} - {Email} by {User}",
            existing.Id, existing.EmailAddress, updatedBy ?? "system");

        return existing;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.EmailRecipients
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _dbContext.EmailRecipients.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Email recipient deleted: {Id} - {Email}",
            entity.Id, entity.EmailAddress);

        return true;
    }

    private static EmailRecipient Normalize(EmailRecipient source)
    {
        return new EmailRecipient
        {
            Id = source.Id,
            EmailAddress = source.EmailAddress?.Trim().ToLowerInvariant() ?? string.Empty,
            DisplayName = source.DisplayName?.Trim() ?? string.Empty,
            Description = source.Description?.Trim(),
            NotificationTypes = source.NotificationTypes?.Trim() ?? "MissionError,JobQueryError",
            IsActive = source.IsActive,
            CreatedUtc = source.CreatedUtc,
            UpdatedUtc = source.UpdatedUtc,
            CreatedBy = source.CreatedBy,
            UpdatedBy = source.UpdatedBy
        };
    }
}

/// <summary>
/// Exception thrown when attempting to create/update an email recipient with a duplicate email address.
/// </summary>
public class EmailRecipientConflictException : Exception
{
    public EmailRecipientConflictException(string message) : base(message) { }
}
