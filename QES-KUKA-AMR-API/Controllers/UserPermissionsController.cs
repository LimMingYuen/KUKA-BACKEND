using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.UserPermission;
using QES_KUKA_AMR_API.Services.UserPermissions;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/user-permissions")]
public class UserPermissionsController : ControllerBase
{
    private readonly IUserPermissionService _userPermissionService;
    private readonly ILogger<UserPermissionsController> _logger;

    public UserPermissionsController(
        IUserPermissionService userPermissionService,
        ILogger<UserPermissionsController> logger)
    {
        _userPermissionService = userPermissionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all user permissions
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<UserPermissionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserPermissionDto>>>> GetUserPermissionsAsync(
        CancellationToken cancellationToken)
    {
        var permissions = await _userPermissionService.GetAsync(cancellationToken);
        var dtos = permissions.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    /// <summary>
    /// Get user permission by ID
    /// </summary>
    [HttpGet("{id:int}", Name = nameof(GetUserPermissionByIdAsync))]
    [ProducesResponseType(typeof(ApiResponse<UserPermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserPermissionDto>>> GetUserPermissionByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var permission = await _userPermissionService.GetByIdAsync(id, cancellationToken);
        if (permission is null)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(Success(MapToDto(permission)));
    }

    /// <summary>
    /// Get all permissions for a specific user
    /// </summary>
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(typeof(ApiResponse<List<UserPermissionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserPermissionDto>>>> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var permissions = await _userPermissionService.GetByUserIdAsync(userId, cancellationToken);
        var dtos = permissions.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    /// <summary>
    /// Get all permissions for a specific page
    /// </summary>
    [HttpGet("page/{pageId:int}")]
    [ProducesResponseType(typeof(ApiResponse<List<UserPermissionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserPermissionDto>>>> GetByPageIdAsync(
        int pageId,
        CancellationToken cancellationToken)
    {
        var permissions = await _userPermissionService.GetByPageIdAsync(pageId, cancellationToken);
        var dtos = permissions.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    /// <summary>
    /// Create a new user permission
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserPermissionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserPermissionDto>>> CreateUserPermissionAsync(
        [FromBody] UserPermissionCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var entity = await _userPermissionService.CreateAsync(new UserPermission
            {
                UserId = request.UserId,
                PageId = request.PageId,
                CanAccess = request.CanAccess
            }, cancellationToken);

            var dto = MapToDto(entity);
            return CreatedAtRoute(nameof(GetUserPermissionByIdAsync), new { id = dto.Id }, Success(dto));
        }
        catch (UserPermissionConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while creating user permission");
            return Conflict(new ProblemDetails
            {
                Title = "User permission already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    /// <summary>
    /// Update an existing user permission
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserPermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserPermissionDto>>> UpdateUserPermissionAsync(
        int id,
        [FromBody] UserPermissionUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updated = await _userPermissionService.UpdateAsync(id, new UserPermission
        {
            CanAccess = request.CanAccess
        }, cancellationToken);

        if (updated is null)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(Success(MapToDto(updated)));
    }

    /// <summary>
    /// Delete a user permission
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUserPermissionAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await _userPermissionService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = "User permission deleted.",
            Data = null
        });
    }

    /// <summary>
    /// Bulk set permissions for a user (creates or updates)
    /// </summary>
    [HttpPost("bulk-set")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> BulkSetPermissionsAsync(
        [FromBody] UserPermissionBulkSetRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var modifiedCount = await _userPermissionService.BulkSetPermissionsAsync(
            request.UserId,
            request.PagePermissions,
            cancellationToken);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = $"Bulk set {modifiedCount} permissions for user.",
            Data = new { ModifiedCount = modifiedCount }
        });
    }

    private static UserPermissionDto MapToDto(UserPermission permission) => new()
    {
        Id = permission.Id,
        UserId = permission.UserId,
        Username = permission.User?.Username ?? string.Empty,
        PageId = permission.PageId,
        PagePath = permission.Page?.PagePath ?? string.Empty,
        PageName = permission.Page?.PageName ?? string.Empty,
        CanAccess = permission.CanAccess,
        CreatedUtc = permission.CreatedUtc
    };

    private static ApiResponse<T> Success<T>(T data) => new()
    {
        Success = true,
        Data = data
    };

    private static ProblemDetails NotFoundProblem(int id) => new()
    {
        Title = "User permission not found.",
        Detail = $"User permission with id '{id}' was not found.",
        Status = StatusCodes.Status404NotFound,
        Type = "https://httpstatuses.com/404"
    };
}
