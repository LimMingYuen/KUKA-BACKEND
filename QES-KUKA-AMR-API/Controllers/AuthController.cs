using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.Auth;
using QES_KUKA_AMR_API.Services.Auth;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IExternalApiTokenService _externalApiTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IExternalApiTokenService externalApiTokenService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _externalApiTokenService = externalApiTokenService;
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
}
