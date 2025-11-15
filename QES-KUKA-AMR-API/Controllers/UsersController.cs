using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.Users;
using QES_KUKA_AMR_API.Services.Users;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<UserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAsync(cancellationToken);
        var dtos = users.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:int}", Name = nameof(GetUserByIdAsync))]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserByIdAsync(int id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(Success(MapToDto(user)));
    }

    /// <summary>
    /// Get user by username
    /// </summary>
    [HttpGet("username/{username}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByUsernameAsync(username, cancellationToken);
        if (user is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found.",
                Detail = $"User with username '{username}' was not found.",
                Status = StatusCodes.Status404NotFound,
                Type = "https://httpstatuses.com/404"
            });
        }

        return Ok(Success(MapToDto(user)));
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUserAsync(
        [FromBody] UserCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var entity = await _userService.CreateAsync(new User
            {
                Username = request.Username,
                Nickname = request.Nickname,
                IsSuperAdmin = request.IsSuperAdmin,
                Roles = request.Roles ?? new List<string>(),
                CreateBy = request.CreateBy,
                CreateApp = request.CreateApp
            }, cancellationToken);

            var dto = MapToDto(entity);
            return CreatedAtRoute(nameof(GetUserByIdAsync), new { id = dto.Id }, Success(dto));
        }
        catch (UserConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while creating user with username {Username}", request.Username);
            return Conflict(new ProblemDetails
            {
                Title = "User already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUserAsync(
        int id,
        [FromBody] UserUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var updated = await _userService.UpdateAsync(id, new User
            {
                Username = request.Username,
                Nickname = request.Nickname,
                IsSuperAdmin = request.IsSuperAdmin,
                Roles = request.Roles ?? new List<string>(),
                LastUpdateBy = request.LastUpdateBy,
                LastUpdateApp = request.LastUpdateApp
            }, cancellationToken);

            if (updated is null)
            {
                return NotFound(NotFoundProblem(id));
            }

            return Ok(Success(MapToDto(updated)));
        }
        catch (UserConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while updating user {UserId}", id);
            return Conflict(new ProblemDetails
            {
                Title = "User already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUserAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await _userService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = "User deleted.",
            Data = null
        });
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Nickname = user.Nickname,
        IsSuperAdmin = user.IsSuperAdmin,
        Roles = user.Roles,
        CreateTime = user.CreateTime,
        CreateBy = user.CreateBy,
        CreateApp = user.CreateApp,
        LastUpdateTime = user.LastUpdateTime,
        LastUpdateBy = user.LastUpdateBy,
        LastUpdateApp = user.LastUpdateApp
    };

    private static ApiResponse<T> Success<T>(T data) => new()
    {
        Success = true,
        Data = data
    };

    private static ProblemDetails NotFoundProblem(int id) => new()
    {
        Title = "User not found.",
        Detail = $"User with id '{id}' was not found.",
        Status = StatusCodes.Status404NotFound,
        Type = "https://httpstatuses.com/404"
    };
}
