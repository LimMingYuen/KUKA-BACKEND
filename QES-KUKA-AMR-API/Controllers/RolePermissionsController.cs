using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.RolePermission;
using QES_KUKA_AMR_API.Services.RolePermissions;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/role-permissions")]
public class RolePermissionsController : ControllerBase
{
    private readonly IRolePermissionService _rolePermissionService;
    private readonly ILogger<RolePermissionsController> _logger;

    public RolePermissionsController(
        IRolePermissionService rolePermissionService,
        ILogger<RolePermissionsController> logger)
    {
        _rolePermissionService = rolePermissionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all role permissions
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<RolePermissionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<RolePermissionDto>>>> GetRolePermissionsAsync(
        CancellationToken cancellationToken)
    {
        var permissions = await _rolePermissionService.GetAsync(cancellationToken);
        var dtos = permissions.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    /// <summary>
    /// Get role permission by ID
    /// </summary>
    [HttpGet("{id:int}", Name = nameof(GetRolePermissionByIdAsync))]
    [ProducesResponseType(typeof(ApiResponse<RolePermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RolePermissionDto>>> GetRolePermissionByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var permission = await _rolePermissionService.GetByIdAsync(id, cancellationToken);
        if (permission is null)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(Success(MapToDto(permission)));
    }

    /// <summary>
    /// Get all permissions for a specific role
    /// </summary>
    [HttpGet("role/{roleId:int}")]
    [ProducesResponseType(typeof(ApiResponse<List<RolePermissionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<RolePermissionDto>>>> GetByRoleIdAsync(
        int roleId,
        CancellationToken cancellationToken)
    {
        var permissions = await _rolePermissionService.GetByRoleIdAsync(roleId, cancellationToken);
        var dtos = permissions.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    /// <summary>
    /// Get all permissions for a specific page
    /// </summary>
    [HttpGet("page/{pageId:int}")]
    [ProducesResponseType(typeof(ApiResponse<List<RolePermissionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<RolePermissionDto>>>> GetByPageIdAsync(
        int pageId,
        CancellationToken cancellationToken)
    {
        var permissions = await _rolePermissionService.GetByPageIdAsync(pageId, cancellationToken);
        var dtos = permissions.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    /// <summary>
    /// Get permission matrix (all roles x all pages)
    /// </summary>
    [HttpGet("matrix")]
    [ProducesResponseType(typeof(ApiResponse<RolePermissionMatrix>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RolePermissionMatrix>>> GetPermissionMatrixAsync(
        CancellationToken cancellationToken)
    {
        var matrix = await _rolePermissionService.GetPermissionMatrixAsync(cancellationToken);
        return Ok(Success(matrix));
    }

    /// <summary>
    /// Create a new role permission
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RolePermissionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RolePermissionDto>>> CreateRolePermissionAsync(
        [FromBody] RolePermissionCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var entity = await _rolePermissionService.CreateAsync(new RolePermission
            {
                RoleId = request.RoleId,
                PageId = request.PageId,
                CanAccess = request.CanAccess
            }, cancellationToken);

            var dto = MapToDto(entity);
            return CreatedAtRoute(nameof(GetRolePermissionByIdAsync), new { id = dto.Id }, Success(dto));
        }
        catch (RolePermissionConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while creating role permission");
            return Conflict(new ProblemDetails
            {
                Title = "Role permission already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    /// <summary>
    /// Update an existing role permission
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<RolePermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RolePermissionDto>>> UpdateRolePermissionAsync(
        int id,
        [FromBody] RolePermissionUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updated = await _rolePermissionService.UpdateAsync(id, new RolePermission
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
    /// Delete a role permission
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteRolePermissionAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await _rolePermissionService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = "Role permission deleted.",
            Data = null
        });
    }

    /// <summary>
    /// Bulk set permissions for a role (creates or updates)
    /// </summary>
    [HttpPost("bulk-set")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> BulkSetPermissionsAsync(
        [FromBody] RolePermissionBulkSetRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var modifiedCount = await _rolePermissionService.BulkSetPermissionsAsync(
            request.RoleId,
            request.PagePermissions,
            cancellationToken);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = $"Bulk set {modifiedCount} permissions for role.",
            Data = new { ModifiedCount = modifiedCount }
        });
    }

    private static RolePermissionDto MapToDto(RolePermission permission) => new()
    {
        Id = permission.Id,
        RoleId = permission.RoleId,
        RoleName = permission.Role?.Name ?? string.Empty,
        RoleCode = permission.Role?.RoleCode ?? string.Empty,
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
        Title = "Role permission not found.",
        Detail = $"Role permission with id '{id}' was not found.",
        Status = StatusCodes.Status404NotFound,
        Type = "https://httpstatuses.com/404"
    };
}
