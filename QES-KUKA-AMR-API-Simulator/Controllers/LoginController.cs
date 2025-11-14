using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using QES_KUKA_AMR_API_Simulator.Auth;
using QES_KUKA_AMR_API_Simulator.Models;
using QES_KUKA_AMR_API_Simulator.Models.Login;
using QES_KUKA_AMR_API_Simulator.Repositories;

namespace QES_KUKA_AMR_API_Simulator.Controllers;

[ApiController]
[Route("api/v1/data/sys-user")]
public class LoginController : ControllerBase
{
    private readonly SimulatorJwtOptions _jwtOptions;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<LoginController> _logger;

    public LoginController(
        SimulatorJwtOptions jwtOptions,
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<LoginController> logger)
    {
        _jwtOptions = jwtOptions;
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }
    private static readonly IReadOnlyDictionary<string, string> Users =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["admin"] = "Admin",
            ["operator"] = "operator"
        };

    private static readonly LoginUserInfo AdminUser = new()
    {
        Id = 1,
        Username = "admin",
        Nickname = "admin",
        IsSuperAdmin = 1,
        Roles = new[]
        {
            new LoginRole
            {
                Id = 1,
                Name = "administrator",
                RoleCode = "administrator",
                IsProtected = 1
            }
        }
    };

    private static readonly IReadOnlyList<LoginPermission> AdminPermissions =
        new List<LoginPermission>
        {
            new() { Id = 1, Code = "pg_acc", Name = "账号管理权限", Type = 1, PermissionGroup = string.Empty, PermissionClass = "account", UiSign = "accountmanager" },
            new() { Id = 2, Code = "pg_bdmgr", Name = "仓库管理访问权限", Type = 1, PermissionGroup = string.Empty, PermissionClass = "browse", UiSign = "buildingmanager" },
            new() { Id = 3, Code = "pg_mpmgr", Name = "地图管理访问权限", Type = 1, PermissionGroup = string.Empty, PermissionClass = "browse", UiSign = "mapmanager" },
            new() { Id = 4, Code = "pg_rbtmgr", Name = "机器人类型管理访问权限", Type = 1, PermissionGroup = string.Empty, PermissionClass = "browse", UiSign = "robottypemanager" },
            new() { Id = 5, Code = "pg_rbmgr", Name = "机器人管理访问权限", Type = 1, PermissionGroup = string.Empty, PermissionClass = "browse", UiSign = "robotmanager" },
            new() { Id = 6, Code = "pg_cmmgr", Name = "容器模型管理访问权限", Type = 1, PermissionGroup = string.Empty, PermissionClass = "browse", UiSign = "containermodelmanager" },
            new() { Id = 7, Code = "pg_cmgr", Name = "容器管理访问权限", Type = 1, PermissionGroup = string.Empty, PermissionClass = "browse", UiSign = "containermanager" },
            new() { Id = 8, Code = "pg_mtplmgr", Name = "任务模板管理访问权限", Type = 1, PermissionGroup = string.Empty, PermissionClass = "browse", UiSign = "missiontemplatemanager" },
            new() { Id = 9, Code = "pg_mmgr", Name = "任务管理访问权限", Type = 1, PermissionGroup = string.Empty, PermissionClass = "browse", UiSign = "missionmanager" },
            new() { Id = 10, Code = "pg_monitor", Name = "地图监控权限", Type = 1, PermissionGroup = string.Empty, PermissionClass = "monitor", UiSign = "monitor" },
            new() { Id = 11, Code = "pg_editor", Name = "地图编辑权限", Type = 1, PermissionGroup = string.Empty, PermissionClass = "draw", UiSign = "editMap" },
            new() { Id = 12, Code = "act_monitorall", Name = "监控页下发功能权限", Type = 2, PermissionGroup = "pg_monitor", PermissionClass = "monitor", UiSign = "monitoraction" }
        };

    [HttpPost("login")]
    [HttpPost("~/api/login")]
    public ActionResult Login([FromBody] LoginRequest request)
    {
        if (!Users.TryGetValue(request.Username, out var expectedPassword) ||
            !PasswordMatches(request.Password, expectedPassword))
        {
            return Unauthorized(new
            {
                success = false,
                msg = "Invalid username or password.",
                message = "Invalid username or password.",
                code = "AUTH_INVALID",
                data = (object?)null
            });
        }

        var now = DateTime.UtcNow;
        var token = GenerateToken(request.Username, now);
        var refreshToken = GenerateRefreshToken(request.Username, now);

        var payload = new LoginResponseData
        {
            Token = $"Bearer {token}",
            RefreshToken = refreshToken.Token,
            TokenExpiresUtc = now.Add(_jwtOptions.AccessTokenLifetime),
            UserInfo = AdminUser,
            Permissions = AdminPermissions
        };

        _logger.LogInformation("User {Username} logged in successfully. Token expires at {ExpiresUtc}",
            request.Username, payload.TokenExpiresUtc);

        return Ok(new
        {
            success = true,
            msg = (string?)null,
            message = (string?)null,
            code = (string?)null,
            data = payload
        });
    }

    /// <summary>
    /// Refresh an expired access token using a valid refresh token
    /// </summary>
    /// <param name="request">Request containing the refresh token</param>
    /// <returns>New access token and refresh token</returns>
    [HttpPost("refresh")]
    public ActionResult RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new
            {
                success = false,
                msg = "Refresh token is required.",
                message = "Refresh token is required.",
                code = "REFRESH_TOKEN_REQUIRED",
                data = (object?)null
            });
        }

        // Retrieve and validate the refresh token
        var storedToken = _refreshTokenRepository.Get(request.RefreshToken);
        if (storedToken == null)
        {
            _logger.LogWarning("Refresh token not found: {Token}", request.RefreshToken);
            return Unauthorized(new
            {
                success = false,
                msg = "Invalid refresh token.",
                message = "Invalid refresh token.",
                code = "INVALID_REFRESH_TOKEN",
                data = (object?)null
            });
        }

        if (!storedToken.IsValid)
        {
            var reason = storedToken.IsRevoked ? "revoked" : "expired";
            _logger.LogWarning("Refresh token {Reason} for user {Username}",
                reason, storedToken.Username);
            return Unauthorized(new
            {
                success = false,
                msg = $"Refresh token has {reason}.",
                message = $"Refresh token has {reason}.",
                code = storedToken.IsRevoked ? "REFRESH_TOKEN_REVOKED" : "REFRESH_TOKEN_EXPIRED",
                data = (object?)null
            });
        }

        // Generate new tokens
        var now = DateTime.UtcNow;
        var newAccessToken = GenerateToken(storedToken.Username, now);
        var newRefreshToken = GenerateRefreshToken(storedToken.Username, now);

        // Revoke the old refresh token (one-time use)
        _refreshTokenRepository.Revoke(request.RefreshToken);

        var payload = new LoginResponseData
        {
            Token = $"Bearer {newAccessToken}",
            RefreshToken = newRefreshToken.Token,
            TokenExpiresUtc = now.Add(_jwtOptions.AccessTokenLifetime),
            UserInfo = AdminUser,
            Permissions = AdminPermissions
        };

        _logger.LogInformation("Refreshed token for user {Username}. New token expires at {ExpiresUtc}",
            storedToken.Username, payload.TokenExpiresUtc);

        return Ok(new
        {
            success = true,
            msg = (string?)null,
            message = (string?)null,
            code = (string?)null,
            data = payload
        });
    }

    /// <summary>
    /// Logout and revoke all refresh tokens for the current user
    /// </summary>
    /// <param name="request">Request containing the refresh token to identify the user</param>
    [HttpPost("logout")]
    public ActionResult Logout([FromBody] RefreshTokenRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var storedToken = _refreshTokenRepository.Get(request.RefreshToken);
            if (storedToken != null)
            {
                var count = _refreshTokenRepository.RevokeAllForUser(storedToken.Username);
                _logger.LogInformation("Logged out user {Username}, revoked {Count} refresh tokens",
                    storedToken.Username, count);
            }
        }

        return Ok(new
        {
            success = true,
            msg = "Logged out successfully.",
            message = "Logged out successfully.",
            code = (string?)null,
            data = (object?)null
        });
    }

    private static bool PasswordMatches(string providedPassword, string expectedPlainText)
    {
        if (string.Equals(providedPassword, expectedPlainText, StringComparison.Ordinal))
        {
            return true;
        }

        var expectedHash = ComputeMd5Hash(expectedPlainText);
        return string.Equals(providedPassword, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    private string GenerateToken(string username, DateTime now)
    {
        var signingKey = _jwtOptions.GetSigningKey();
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim("username", username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique token ID
        };

        var jwt = new JwtSecurityToken(
            claims: claims,
            notBefore: now,
            expires: now.Add(_jwtOptions.AccessTokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private RefreshToken GenerateRefreshToken(string username, DateTime now)
    {
        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"), // 32-character hex string
            Username = username,
            CreatedUtc = now,
            ExpiresUtc = now.Add(_jwtOptions.RefreshTokenLifetime),
            IsRevoked = false
        };

        _refreshTokenRepository.Store(refreshToken);
        _logger.LogDebug("Generated refresh token for {Username}, expires {ExpiresUtc}",
            username, refreshToken.ExpiresUtc);

        return refreshToken;
    }

    private static string ComputeMd5Hash(string input)
    {
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = md5.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
