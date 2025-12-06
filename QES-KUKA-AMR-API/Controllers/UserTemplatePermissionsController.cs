using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.UserTemplatePermission;
using QES_KUKA_AMR_API.Services.UserTemplatePermissions;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/user-template-permissions")]
public class UserTemplatePermissionsController : ControllerBase
{
    private readonly IUserTemplatePermissionService _userTemplatePermissionService;
    private readonly ILogger<UserTemplatePermissionsController> _logger;

    public UserTemplatePermissionsController(
        IUserTemplatePermissionService userTemplatePermissionService,
        ILogger<UserTemplatePermissionsController> logger)
    {
        _userTemplatePermissionService = userTemplatePermissionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all user template permissions
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<UserTemplatePermissionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserTemplatePermissionDto>>>> GetUserTemplatePermissionsAsync(
        CancellationToken cancellationToken)
    {
        var permissions = await _userTemplatePermissionService.GetAsync(cancellationToken);
        var dtos = permissions.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    /// <summary>
    /// Get user template permission by ID
    /// </summary>
    [HttpGet("{id:int}", Name = nameof(GetUserTemplatePermissionByIdAsync))]
    [ProducesResponseType(typeof(ApiResponse<UserTemplatePermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserTemplatePermissionDto>>> GetUserTemplatePermissionByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var permission = await _userTemplatePermissionService.GetByIdAsync(id, cancellationToken);
        if (permission is null)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(Success(MapToDto(permission)));
    }

    /// <summary>
    /// Get all template permissions for a specific user
    /// </summary>
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(typeof(ApiResponse<List<UserTemplatePermissionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserTemplatePermissionDto>>>> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var permissions = await _userTemplatePermissionService.GetByUserIdAsync(userId, cancellationToken);
        var dtos = permissions.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    /// <summary>
    /// Get all user permissions for a specific template
    /// </summary>
    [HttpGet("template/{templateId:int}")]
    [ProducesResponseType(typeof(ApiResponse<List<UserTemplatePermissionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserTemplatePermissionDto>>>> GetByTemplateIdAsync(
        int templateId,
        CancellationToken cancellationToken)
    {
        var permissions = await _userTemplatePermissionService.GetByTemplateIdAsync(templateId, cancellationToken);
        var dtos = permissions.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    /// <summary>
    /// Create a new user template permission
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserTemplatePermissionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserTemplatePermissionDto>>> CreateUserTemplatePermissionAsync(
        [FromBody] UserTemplatePermissionCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var entity = await _userTemplatePermissionService.CreateAsync(new UserTemplatePermission
            {
                UserId = request.UserId,
                SavedCustomMissionId = request.SavedCustomMissionId,
                CanAccess = request.CanAccess
            }, cancellationToken);

            var dto = MapToDto(entity);
            return CreatedAtRoute(nameof(GetUserTemplatePermissionByIdAsync), new { id = dto.Id }, Success(dto));
        }
        catch (UserTemplatePermissionConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while creating user template permission");
            return Conflict(new ProblemDetails
            {
                Title = "User template permission already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    /// <summary>
    /// Update an existing user template permission
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserTemplatePermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserTemplatePermissionDto>>> UpdateUserTemplatePermissionAsync(
        int id,
        [FromBody] UserTemplatePermissionUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updated = await _userTemplatePermissionService.UpdateAsync(id, new UserTemplatePermission
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
    /// Delete a user template permission
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUserTemplatePermissionAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await _userTemplatePermissionService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = "User template permission deleted.",
            Data = null
        });
    }

    /// <summary>
    /// Bulk set template permissions for a user (creates or updates)
    /// </summary>
    [HttpPost("bulk-set")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> BulkSetPermissionsAsync(
        [FromBody] UserTemplatePermissionBulkSetRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var modifiedCount = await _userTemplatePermissionService.BulkSetPermissionsAsync(
            request.UserId,
            request.TemplatePermissions,
            cancellationToken);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = $"Bulk set {modifiedCount} template permissions for user.",
            Data = new { ModifiedCount = modifiedCount }
        });
    }

    private static UserTemplatePermissionDto MapToDto(UserTemplatePermission permission) => new()
    {
        Id = permission.Id,
        UserId = permission.UserId,
        Username = permission.User?.Username ?? string.Empty,
        SavedCustomMissionId = permission.SavedCustomMissionId,
        MissionName = permission.SavedCustomMission?.MissionName ?? string.Empty,
        Description = permission.SavedCustomMission?.Description,
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
        Title = "User template permission not found.",
        Detail = $"User template permission with id '{id}' was not found.",
        Status = StatusCodes.Status404NotFound,
        Type = "https://httpstatuses.com/404"
    };
}
