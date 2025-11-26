using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserService userService,
        IPermissionCheckService permissionCheckService,
        ITemplatePermissionCheckService templatePermissionCheckService,
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider,
        ILogger<AuthService> logger)
    {
        _userService = userService;
        _permissionCheckService = permissionCheckService;
        _templatePermissionCheckService = templatePermissionCheckService;
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

        var token = GenerateJwtToken(user);
        var expiresAt = _timeProvider.GetUtcNow().AddHours(_jwtOptions.ExpirationHours);

        // Get allowed pages for the user
        var allowedPages = await _permissionCheckService.GetUserAllowedPagePathsAsync(user.Id, cancellationToken);

        // Get allowed templates for the user
        var allowedTemplates = await _templatePermissionCheckService.GetUserAllowedTemplateIdsAsync(user.Id, cancellationToken);

        _logger.LogInformation("User '{Username}' logged in successfully", user.Username);

        return new InternalLoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt.UtcDateTime,
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
            var expiresAt = _timeProvider.GetUtcNow().AddHours(_jwtOptions.ExpirationHours);

            // Get allowed pages for the user
            var allowedPages = await _permissionCheckService.GetUserAllowedPagePathsAsync(createdUser.Id, cancellationToken);

            // Get allowed templates for the user
            var allowedTemplates = await _templatePermissionCheckService.GetUserAllowedTemplateIdsAsync(createdUser.Id, cancellationToken);

            _logger.LogInformation("User '{Username}' registered successfully", createdUser.Username);

            return new InternalLoginResponse
            {
                Token = token,
                ExpiresAt = expiresAt.UtcDateTime,
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
            Expires = _timeProvider.GetUtcNow().AddHours(_jwtOptions.ExpirationHours).UtcDateTime,
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
