using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Auth;
using QES_KUKA_AMR_API.Options;
using QES_KUKA_AMR_API.Services.Users;
using QES_KUKA_AMR_API.Services.Permissions;

namespace QES_KUKA_AMR_API.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserService _userService;
    private readonly IPermissionCheckService _permissionCheckService;
    private readonly ITemplatePermissionCheckService _templatePermissionCheckService;
    private readonly ApplicationDbContext _dbContext;
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserService userService,
        IPermissionCheckService permissionCheckService,
        ITemplatePermissionCheckService templatePermissionCheckService,
        ApplicationDbContext dbContext,
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider,
        ILogger<AuthService> logger)
    {
        _userService = userService;
        _permissionCheckService = permissionCheckService;
        _templatePermissionCheckService = templatePermissionCheckService;
        _dbContext = dbContext;
        _jwtOptions = jwtOptions.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<InternalLoginResponse> LoginAsync(
        InternalLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userService.GetByUsernameAsync(request.Username, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Login failed: User '{Username}' not found", request.Username);
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password for user '{Username}'", request.Username);
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        // Generate tokens
        var token = GenerateJwtToken(user);
        var expiresAt = _timeProvider.GetUtcNow().AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);

        // Generate and store refresh token
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id, cancellationToken);

        // Get allowed pages for the user
        var allowedPages = await _permissionCheckService.GetUserAllowedPagePathsAsync(user.Id, cancellationToken);

        // Get allowed templates for the user
        var allowedTemplates = await _templatePermissionCheckService.GetUserAllowedTemplateIdsAsync(user.Id, cancellationToken);

        _logger.LogInformation("User '{Username}' logged in successfully", user.Username);

        return new InternalLoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt.UtcDateTime,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAt = refreshToken.ExpiresUtc,
            User = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Nickname = user.Nickname,
                IsSuperAdmin = user.IsSuperAdmin,
                Roles = user.Roles,
                AllowedPages = allowedPages,
                AllowedTemplates = allowedTemplates
            }
        };
    }

    public async Task<InternalLoginResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Username = request.Username,
            PasswordHash = passwordHash,
            Nickname = request.Nickname,
            IsSuperAdmin = false,
            Roles = request.Roles ?? new List<string>(),
            CreateApp = "QES-KUKA-AMR-API"
        };

        try
        {
            var createdUser = await _userService.CreateAsync(user, cancellationToken);

            var token = GenerateJwtToken(createdUser);
            var expiresAt = _timeProvider.GetUtcNow().AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);

            // Generate and store refresh token
            var refreshToken = await GenerateAndStoreRefreshTokenAsync(createdUser.Id, cancellationToken);

            // Get allowed pages for the user
            var allowedPages = await _permissionCheckService.GetUserAllowedPagePathsAsync(createdUser.Id, cancellationToken);

            // Get allowed templates for the user
            var allowedTemplates = await _templatePermissionCheckService.GetUserAllowedTemplateIdsAsync(createdUser.Id, cancellationToken);

            _logger.LogInformation("User '{Username}' registered successfully", createdUser.Username);

            return new InternalLoginResponse
            {
                Token = token,
                ExpiresAt = expiresAt.UtcDateTime,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAt = refreshToken.ExpiresUtc,
                User = new UserInfo
                {
                    Id = createdUser.Id,
                    Username = createdUser.Username,
                    Nickname = createdUser.Nickname,
                    IsSuperAdmin = createdUser.IsSuperAdmin,
                    Roles = createdUser.Roles,
                    AllowedPages = allowedPages,
                    AllowedTemplates = allowedTemplates
                }
            };
        }
        catch (UserConflictException ex)
        {
            _logger.LogWarning("Registration failed: {Message}", ex.Message);
            throw new InvalidOperationException(ex.Message);
        }
    }

    public Task<UserInfo?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtOptions.SecretKey);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);
            var username = jwtToken.Claims.First(x => x.Type == ClaimTypes.Name).Value;
            var isSuperAdmin = bool.Parse(jwtToken.Claims.First(x => x.Type == "isSuperAdmin").Value);
            var roles = jwtToken.Claims
                .Where(x => x.Type == ClaimTypes.Role)
                .Select(x => x.Value)
                .ToList();

            return Task.FromResult<UserInfo?>(new UserInfo
            {
                Id = userId,
                Username = username,
                IsSuperAdmin = isSuperAdmin,
                Roles = roles
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Token validation failed: {Message}", ex.Message);
            return Task.FromResult<UserInfo?>(null);
        }
    }

    public async Task<VerifyAdminResponse> VerifyAdminAsync(
        VerifyAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userService.GetByUsernameAsync(request.Username, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Admin verification failed: User '{Username}' not found", request.Username);
            return new VerifyAdminResponse
            {
                IsValid = false,
                Message = "Invalid credentials"
            };
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Admin verification failed: Invalid password for user '{Username}'", request.Username);
            return new VerifyAdminResponse
            {
                IsValid = false,
                Message = "Invalid credentials"
            };
        }

        // Check if user is SuperAdmin
        if (!user.IsSuperAdmin)
        {
            _logger.LogWarning("Admin verification failed: User '{Username}' is not a SuperAdmin", request.Username);
            return new VerifyAdminResponse
            {
                IsValid = false,
                Message = "User is not authorized as admin"
            };
        }

        _logger.LogInformation("Admin verification successful for user '{Username}'", user.Username);

        return new VerifyAdminResponse
        {
            IsValid = true,
            AdminUsername = user.Username,
            Message = "Admin verification successful"
        };
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtOptions.SecretKey);

        var claims = new List<Claim>
        {
            new("id", user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("isSuperAdmin", user.IsSuperAdmin.ToString())
        };

        // Add roles as claims
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = _timeProvider.GetUtcNow().AddMinutes(_jwtOptions.AccessTokenExpirationMinutes).UtcDateTime,
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Refresh the access token using a valid refresh token.
    /// Implements token rotation - old refresh token is revoked and new one is issued.
    /// </summary>
    public async Task<InternalLoginResponse> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var storedToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (storedToken == null)
        {
            _logger.LogWarning("Refresh token not found");
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        if (!storedToken.IsActive)
        {
            _logger.LogWarning("Refresh token is not active (expired or revoked) for user {UserId}", storedToken.UserId);

            // If the token was already used (has a replacement), this might be a token reuse attack
            if (storedToken.IsRevoked && !string.IsNullOrEmpty(storedToken.ReplacedByToken))
            {
                _logger.LogWarning("Possible token reuse attack detected for user {UserId}", storedToken.UserId);
                // Revoke all tokens for this user as a security measure
                await RevokeAllUserTokensAsync(storedToken.UserId, "PossibleTokenReuseAttack", cancellationToken);
            }

            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var user = storedToken.User;
        if (user == null)
        {
            _logger.LogWarning("User not found for refresh token");
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        // Revoke the old refresh token (token rotation)
        storedToken.RevokedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        storedToken.RevokedReason = "Replaced";

        // Generate new refresh token
        var newRefreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id, cancellationToken);

        // Link the old token to the new one
        storedToken.ReplacedByToken = newRefreshToken.Token;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Generate new access token
        var accessToken = GenerateJwtToken(user);
        var expiresAt = _timeProvider.GetUtcNow().AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);

        // Get allowed pages for the user
        var allowedPages = await _permissionCheckService.GetUserAllowedPagePathsAsync(user.Id, cancellationToken);

        // Get allowed templates for the user
        var allowedTemplates = await _templatePermissionCheckService.GetUserAllowedTemplateIdsAsync(user.Id, cancellationToken);

        _logger.LogInformation("Token refreshed successfully for user '{Username}'", user.Username);

        return new InternalLoginResponse
        {
            Token = accessToken,
            ExpiresAt = expiresAt.UtcDateTime,
            RefreshToken = newRefreshToken.Token,
            RefreshTokenExpiresAt = newRefreshToken.ExpiresUtc,
            User = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Nickname = user.Nickname,
                IsSuperAdmin = user.IsSuperAdmin,
                Roles = user.Roles,
                AllowedPages = allowedPages,
                AllowedTemplates = allowedTemplates
            }
        };
    }

    /// <summary>
    /// Revoke a refresh token (used during logout).
    /// </summary>
    public async Task RevokeTokenAsync(
        string refreshToken,
        string reason = "Logout",
        CancellationToken cancellationToken = default)
    {
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (storedToken == null)
        {
            _logger.LogWarning("Attempted to revoke non-existent refresh token");
            return; // Silently ignore - token might already be gone
        }

        if (storedToken.IsRevoked)
        {
            _logger.LogInformation("Refresh token already revoked");
            return;
        }

        storedToken.RevokedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        storedToken.RevokedReason = reason;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refresh token revoked for user {UserId}: {Reason}", storedToken.UserId, reason);
    }

    /// <summary>
    /// Revoke all refresh tokens for a user (used for security purposes).
    /// </summary>
    public async Task RevokeAllUserTokensAsync(
        int userId,
        string reason = "Security",
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedUtc = now;
            token.RevokedReason = reason;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Revoked {Count} refresh tokens for user {UserId}: {Reason}",
            activeTokens.Count, userId, reason);
    }

    /// <summary>
    /// Generate a cryptographically secure refresh token and store it in the database.
    /// </summary>
    private async Task<RefreshToken> GenerateAndStoreRefreshTokenAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var refreshToken = new RefreshToken
        {
            Token = GenerateRefreshTokenString(),
            UserId = userId,
            CreatedUtc = now,
            ExpiresUtc = now.AddDays(_jwtOptions.RefreshTokenExpirationDays)
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return refreshToken;
    }

    /// <summary>
    /// Generate a cryptographically secure random string for refresh token.
    /// </summary>
    private static string GenerateRefreshTokenString()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
