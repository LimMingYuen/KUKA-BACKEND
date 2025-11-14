namespace QES_KUKA_AMR_API_Simulator.Models
{
    /// <summary>
    /// Represents a refresh token for JWT authentication auto-renewal
    /// </summary>
    public class RefreshToken
    {
        /// <summary>
        /// Unique token identifier (GUID)
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Username associated with this refresh token
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// UTC timestamp when the token expires
        /// </summary>
        public DateTime ExpiresUtc { get; set; }

        /// <summary>
        /// UTC timestamp when the token was created
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// Whether this token has been revoked (e.g., on logout)
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// UTC timestamp when the token was revoked (if applicable)
        /// </summary>
        public DateTime? RevokedUtc { get; set; }

        /// <summary>
        /// Checks if the refresh token is currently valid (not expired and not revoked)
        /// </summary>
        public bool IsValid => !IsRevoked && DateTime.UtcNow < ExpiresUtc;
    }
}
