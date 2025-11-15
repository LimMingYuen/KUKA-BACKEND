using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Services.Users;

public interface IUserService
{
    Task<List<User>> GetAsync(CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> UpdateAsync(int id, User user, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class UserService : IUserService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UserService> _logger;
    private readonly TimeProvider _timeProvider;

    public UserService(
        ApplicationDbContext dbContext,
        ILogger<UserService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<List<User>> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = NormalizeUsername(username);
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == normalizedUsername, cancellationToken);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(user);

        var exists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Username == normalized.Username, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Cannot create user. Username {Username} already exists.", normalized.Username);
            throw new UserConflictException($"User with username '{normalized.Username}' already exists.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        normalized.CreateTime = now;
        normalized.LastUpdateTime = now;

        _dbContext.Users.Add(normalized);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return normalized;
    }

    public async Task<User?> UpdateAsync(int id, User user, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var normalized = Normalize(user);
        var duplicateExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id != id && u.Username == normalized.Username, cancellationToken);

        if (duplicateExists)
        {
            _logger.LogWarning("Cannot update user {Id}. Username {Username} already exists.", id, normalized.Username);
            throw new UserConflictException($"User with username '{normalized.Username}' already exists.");
        }

        existing.Username = normalized.Username;
        existing.Nickname = normalized.Nickname;
        existing.IsSuperAdmin = normalized.IsSuperAdmin;
        existing.Roles = normalized.Roles;
        existing.LastUpdateTime = _timeProvider.GetUtcNow().UtcDateTime;
        existing.LastUpdateBy = normalized.LastUpdateBy;
        existing.LastUpdateApp = normalized.LastUpdateApp;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _dbContext.Users.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static User Normalize(User source)
    {
        source.Username = NormalizeUsername(source.Username);

        if (!string.IsNullOrWhiteSpace(source.Nickname))
        {
            source.Nickname = source.Nickname.Trim();
        }

        if (!string.IsNullOrWhiteSpace(source.CreateBy))
        {
            source.CreateBy = source.CreateBy.Trim();
        }

        if (!string.IsNullOrWhiteSpace(source.CreateApp))
        {
            source.CreateApp = source.CreateApp.Trim();
        }

        if (!string.IsNullOrWhiteSpace(source.LastUpdateBy))
        {
            source.LastUpdateBy = source.LastUpdateBy.Trim();
        }

        if (!string.IsNullOrWhiteSpace(source.LastUpdateApp))
        {
            source.LastUpdateApp = source.LastUpdateApp.Trim();
        }

        return source;
    }

    private static string NormalizeUsername(string username)
    {
        return (username ?? string.Empty).Trim().ToLowerInvariant();
    }
}

public class UserConflictException : Exception
{
    public UserConflictException(string message) : base(message)
    {
    }
}
