using System.Collections.Concurrent;
using QES_KUKA_AMR_API_Simulator.Models;

namespace QES_KUKA_AMR_API_Simulator.Repositories
{
    /// <summary>
    /// In-memory repository for managing refresh tokens
    /// Thread-safe implementation using ConcurrentDictionary
    /// </summary>
    public interface IRefreshTokenRepository
    {
        /// <summary>
        /// Stores a refresh token
        /// </summary>
        void Store(RefreshToken token);

        /// <summary>
        /// Retrieves a refresh token by its token string
        /// </summary>
        RefreshToken? Get(string token);

        /// <summary>
        /// Revokes a refresh token
        /// </summary>
        bool Revoke(string token);

        /// <summary>
        /// Revokes all refresh tokens for a specific username
        /// </summary>
        int RevokeAllForUser(string username);

        /// <summary>
        /// Removes expired and revoked tokens from storage
        /// </summary>
        int CleanupExpired();
    }

    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ConcurrentDictionary<string, RefreshToken> _tokens = new();
        private readonly ILogger<RefreshTokenRepository> _logger;

        public RefreshTokenRepository(ILogger<RefreshTokenRepository> logger)
        {
            _logger = logger;
        }

        public void Store(RefreshToken token)
        {
            if (string.IsNullOrEmpty(token.Token))
            {
                throw new ArgumentException("Token cannot be null or empty", nameof(token));
            }

            _tokens[token.Token] = token;
            _logger.LogDebug("Stored refresh token for user {Username}, expires {ExpiresUtc}",
                token.Username, token.ExpiresUtc);
        }

        public RefreshToken? Get(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            return _tokens.TryGetValue(token, out var refreshToken) ? refreshToken : null;
        }

        public bool Revoke(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            if (_tokens.TryGetValue(token, out var refreshToken))
            {
                refreshToken.IsRevoked = true;
                refreshToken.RevokedUtc = DateTime.UtcNow;
                _logger.LogInformation("Revoked refresh token for user {Username}", refreshToken.Username);
                return true;
            }

            return false;
        }

        public int RevokeAllForUser(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return 0;
            }

            var count = 0;
            var now = DateTime.UtcNow;

            foreach (var kvp in _tokens.Where(t => t.Value.Username == username && !t.Value.IsRevoked))
            {
                kvp.Value.IsRevoked = true;
                kvp.Value.RevokedUtc = now;
                count++;
            }

            if (count > 0)
            {
                _logger.LogInformation("Revoked {Count} refresh tokens for user {Username}", count, username);
            }

            return count;
        }

        public int CleanupExpired()
        {
            var now = DateTime.UtcNow;
            var toRemove = _tokens
                .Where(kvp => kvp.Value.ExpiresUtc < now || kvp.Value.IsRevoked)
                .Select(kvp => kvp.Key)
                .ToList();

            var count = 0;
            foreach (var key in toRemove)
            {
                if (_tokens.TryRemove(key, out _))
                {
                    count++;
                }
            }

            if (count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired/revoked refresh tokens", count);
            }

            return count;
        }
    }
}
