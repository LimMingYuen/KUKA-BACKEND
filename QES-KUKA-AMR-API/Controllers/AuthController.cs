using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.Auth;
using QES_KUKA_AMR_API.Services.Auth;
using QES_KUKA_AMR_API.Services.Users;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IExternalApiTokenService _externalApiTokenService;
    private readonly IUserService _userService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IExternalApiTokenService externalApiTokenService,
        IUserService userService,
        ApplicationDbContext dbContext,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _externalApiTokenService = externalApiTokenService;
        _userService = userService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Login with internal application credentials.
    /// Automatically triggers external API authentication in the background.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<InternalLoginResponse>>> LoginAsync(
        [FromBody] InternalLoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Authenticate user with internal credentials
            var response = await _authService.LoginAsync(request, cancellationToken);

            // Trigger external API token refresh in background (don't wait for it)
            // This ensures the external API token is ready when needed
            _ = Task.Run(async () =>
            {
                try
                {
                    await _externalApiTokenService.GetTokenAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to pre-fetch external API token during login");
                }
            }, cancellationToken);

            return Ok(new ApiResponse<InternalLoginResponse>
            {
                Success = true,
                Code = "AUTH_SUCCESS",
                Msg = "Login successful",
                Data = response
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ApiResponse<InternalLoginResponse>
            {
                Success = false,
                Code = "AUTH_FAILED",
                Msg = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", request.Username);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<InternalLoginResponse>
            {
                Success = false,
                Code = "AUTH_ERROR",
                Msg = "An error occurred during login"
            });
        }
    }

    /// <summary>
    /// Verify admin credentials for privileged operations.
    /// Validates that the provided credentials belong to a SuperAdmin user.
    /// Does not issue a token - only verifies credentials.
    /// </summary>
    [HttpPost("verify-admin")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<VerifyAdminResponse>>> VerifyAdminAsync(
        [FromBody] VerifyAdminRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.VerifyAdminAsync(request, cancellationToken);

            if (!response.IsValid)
            {
                return Ok(new ApiResponse<VerifyAdminResponse>
                {
                    Success = false,
                    Code = "ADMIN_VERIFY_FAILED",
                    Msg = response.Message ?? "Admin verification failed",
                    Data = response
                });
            }

            return Ok(new ApiResponse<VerifyAdminResponse>
            {
                Success = true,
                Code = "ADMIN_VERIFY_SUCCESS",
                Msg = "Admin verification successful",
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin verification for user {Username}", request.Username);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<VerifyAdminResponse>
            {
                Success = false,
                Code = "ADMIN_VERIFY_ERROR",
                Msg = "An error occurred during admin verification"
            });
        }
    }

    /// <summary>
    /// Refresh the access token using a valid refresh token.
    /// Implements token rotation - old refresh token is revoked and new one is issued.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<InternalLoginResponse>>> RefreshTokenAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

            return Ok(new ApiResponse<InternalLoginResponse>
            {
                Success = true,
                Code = "REFRESH_SUCCESS",
                Msg = "Token refreshed successfully",
                Data = response
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ApiResponse<InternalLoginResponse>
            {
                Success = false,
                Code = "REFRESH_FAILED",
                Msg = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<InternalLoginResponse>
            {
                Success = false,
                Code = "REFRESH_ERROR",
                Msg = "An error occurred during token refresh"
            });
        }
    }

    /// <summary>
    /// Logout the current user by revoking their refresh token.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> LogoutAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _authService.RevokeTokenAsync(request.RefreshToken, "Logout", cancellationToken);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = "LOGOUT_SUCCESS",
                Msg = "Logged out successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            // Still return success - user experience should not be affected
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = "LOGOUT_SUCCESS",
                Msg = "Logged out successfully"
            });
        }
    }

    /// <summary>
    /// Register a new user with internal credentials.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<InternalLoginResponse>>> RegisterAsync(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RegisterAsync(request, cancellationToken);

            // Trigger external API token refresh in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await _externalApiTokenService.GetTokenAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to pre-fetch external API token during registration");
                }
            }, cancellationToken);

            return Ok(new ApiResponse<InternalLoginResponse>
            {
                Success = true,
                Code = "REGISTER_SUCCESS",
                Msg = "Registration successful",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<InternalLoginResponse>
            {
                Success = false,
                Code = "REGISTER_FAILED",
                Msg = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user {Username}", request.Username);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<InternalLoginResponse>
            {
                Success = false,
                Code = "REGISTER_ERROR",
                Msg = "An error occurred during registration"
            });
        }
    }

    /// <summary>
    /// Validate the current user's token and return user information.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserInfo>>> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        try
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new ApiResponse<UserInfo>
                {
                    Success = false,
                    Code = "AUTH_INVALID_TOKEN",
                    Msg = "Invalid or missing token"
                });
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var userInfo = await _authService.ValidateTokenAsync(token, cancellationToken);

            if (userInfo == null)
            {
                return Unauthorized(new ApiResponse<UserInfo>
                {
                    Success = false,
                    Code = "AUTH_INVALID_TOKEN",
                    Msg = "Invalid token"
                });
            }

            return Ok(new ApiResponse<UserInfo>
            {
                Success = true,
                Code = "AUTH_SUCCESS",
                Msg = "Token is valid",
                Data = userInfo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<UserInfo>
            {
                Success = false,
                Code = "AUTH_ERROR",
                Msg = "An error occurred during token validation"
            });
        }
    }

    /// <summary>
    /// Get the current external API token status (for debugging).
    /// </summary>
    [HttpGet("external-token-status")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> GetExternalTokenStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            var token = await _externalApiTokenService.GetTokenAsync(cancellationToken);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = "SUCCESS",
                Msg = "External API token is available",
                Data = new
                {
                    HasToken = !string.IsNullOrEmpty(token),
                    TokenPreview = !string.IsNullOrEmpty(token) && token.Length > 20
                        ? $"{token.Substring(0, 10)}...{token.Substring(token.Length - 10)}"
                        : "N/A"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting external API token status");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Code = "ERROR",
                Msg = "Failed to get external API token"
            });
        }
    }

    /// <summary>
    /// DEBUG: Check if admin user exists and password hash format
    /// </summary>
    [HttpGet("_debug/check-admin")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> CheckAdminUser()
    {
        try
        {
            var adminUser = await _userService.GetByUsernameAsync("admin");

            if (adminUser == null)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Code = "USER_NOT_FOUND",
                    Msg = "Admin user does not exist in database",
                    Data = new
                    {
                        userExists = false,
                        suggestion = "Run: POST /api/auth/_debug/create-admin to create the admin user"
                    }
                });
            }

            var hashPreview = adminUser.PasswordHash.Length > 30
                ? $"{adminUser.PasswordHash.Substring(0, 30)}..."
                : adminUser.PasswordHash;

            var isValidBCryptFormat = adminUser.PasswordHash.StartsWith("$2a$") ||
                                     adminUser.PasswordHash.StartsWith("$2b$") ||
                                     adminUser.PasswordHash.StartsWith("$2y$");

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = "USER_FOUND",
                Msg = "Admin user found",
                Data = new
                {
                    userExists = true,
                    username = adminUser.Username,
                    nickname = adminUser.Nickname,
                    isSuperAdmin = adminUser.IsSuperAdmin,
                    passwordHashPreview = hashPreview,
                    passwordHashLength = adminUser.PasswordHash.Length,
                    isValidBCryptFormat = isValidBCryptFormat,
                    expectedHashLength = 60,
                    roles = adminUser.Roles
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking admin user");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Code = "ERROR",
                Msg = ex.Message
            });
        }
    }

    /// <summary>
    /// DEBUG: Create admin user manually
    /// </summary>
    [HttpPost("_debug/create-admin")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> CreateAdminUser()
    {
        try
        {
            await DbInitializer.SeedAsync(_dbContext);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = "ADMIN_CREATED",
                Msg = "Admin user created or already exists",
                Data = new
                {
                    username = "admin",
                    password = "admin",
                    note = "Check console output for confirmation"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating admin user");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Code = "ERROR",
                Msg = ex.Message
            });
        }
    }

    /// <summary>
    /// DEBUG: Test password verification
    /// </summary>
    [HttpPost("_debug/test-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> TestPassword([FromBody] TestPasswordRequest request)
    {
        try
        {
            var user = await _userService.GetByUsernameAsync(request.Username);

            if (user == null)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Code = "USER_NOT_FOUND",
                    Msg = $"User '{request.Username}' not found"
                });
            }

            var isPasswordCorrect = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Code = "TEST_COMPLETE",
                Msg = "Password verification test complete",
                Data = new
                {
                    username = user.Username,
                    passwordMatches = isPasswordCorrect,
                    passwordHashPreview = user.PasswordHash.Substring(0, Math.Min(30, user.PasswordHash.Length)) + "...",
                    testedPassword = request.Password
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing password");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Code = "ERROR",
                Msg = ex.Message
            });
        }
    }
}

public class TestPasswordRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
