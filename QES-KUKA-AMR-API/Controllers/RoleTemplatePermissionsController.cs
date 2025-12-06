using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.RoleTemplatePermission;
using QES_KUKA_AMR_API.Services.RoleTemplatePermissions;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/role-template-permissions")]
public class RoleTemplatePermissionsController : ControllerBase
{
    private readonly IRoleTemplatePermissionService _roleTemplatePermissionService;
    private readonly ILogger<RoleTemplatePermissionsController> _logger;

    public RoleTemplatePermissionsController(
        IRoleTemplatePermissionService roleTemplatePermissionService,
        ILogger<RoleTemplatePermissionsController> logger)
    {
        _roleTemplatePermissionService = roleTemplatePermissionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all role template permissions
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<RoleTemplatePermissionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<RoleTemplatePermissionDto>>>> GetRoleTemplatePermissionsAsync(
        CancellationToken cancellationToken)
    {
        var permissions = await _roleTemplatePermissionService.GetAsync(cancellationToken);
        var dtos = permissions.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    /// <summary>
    /// Get role template permission by ID
    /// </summary>
    [HttpGet("{id:int}", Name = nameof(GetRoleTemplatePermissionByIdAsync))]
    [ProducesResponseType(typeof(ApiResponse<RoleTemplatePermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RoleTemplatePermissionDto>>> GetRoleTemplatePermissionByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var permission = await _roleTemplatePermissionService.GetByIdAsync(id, cancellationToken);
        if (permission is null)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(Success(MapToDto(permission)));
    }

    /// <summary>
    /// Get all template permissions for a specific role
    /// </summary>
    [HttpGet("role/{roleId:int}")]
    [ProducesResponseType(typeof(ApiResponse<List<RoleTemplatePermissionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<RoleTemplatePermissionDto>>>> GetByRoleIdAsync(
        int roleId,
        CancellationToken cancellationToken)
    {
        var permissions = await _roleTemplatePermissionService.GetByRoleIdAsync(roleId, cancellationToken);
        var dtos = permissions.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    /// <summary>
    /// Get all role permissions for a specific template
    /// </summary>
    [HttpGet("template/{templateId:int}")]
    [ProducesResponseType(typeof(ApiResponse<List<RoleTemplatePermissionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<RoleTemplatePermissionDto>>>> GetByTemplateIdAsync(
        int templateId,
        CancellationToken cancellationToken)
    {
        var permissions = await _roleTemplatePermissionService.GetByTemplateIdAsync(templateId, cancellationToken);
        var dtos = permissions.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    /// <summary>
    /// Get permission matrix (all roles x all templates)
    /// </summary>
    [HttpGet("matrix")]
    [ProducesResponseType(typeof(ApiResponse<RoleTemplatePermissionMatrix>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RoleTemplatePermissionMatrix>>> GetPermissionMatrixAsync(
        CancellationToken cancellationToken)
    {
        var matrix = await _roleTemplatePermissionService.GetPermissionMatrixAsync(cancellationToken);
        return Ok(Success(matrix));
    }

    /// <summary>
    /// Create a new role template permission
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RoleTemplatePermissionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RoleTemplatePermissionDto>>> CreateRoleTemplatePermissionAsync(
        [FromBody] RoleTemplatePermissionCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var entity = await _roleTemplatePermissionService.CreateAsync(new RoleTemplatePermission
            {
                RoleId = request.RoleId,
                SavedCustomMissionId = request.SavedCustomMissionId,
                CanAccess = request.CanAccess
            }, cancellationToken);

            var dto = MapToDto(entity);
            return CreatedAtRoute(nameof(GetRoleTemplatePermissionByIdAsync), new { id = dto.Id }, Success(dto));
        }
        catch (RoleTemplatePermissionConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while creating role template permission");
            return Conflict(new ProblemDetails
            {
                Title = "Role template permission already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    /// <summary>
    /// Update an existing role template permission
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<RoleTemplatePermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RoleTemplatePermissionDto>>> UpdateRoleTemplatePermissionAsync(
        int id,
        [FromBody] RoleTemplatePermissionUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updated = await _roleTemplatePermissionService.UpdateAsync(id, new RoleTemplatePermission
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
    /// Delete a role template permission
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteRoleTemplatePermissionAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await _roleTemplatePermissionService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = "Role template permission deleted.",
            Data = null
        });
    }

    /// <summary>
    /// Bulk set template permissions for a role (creates or updates)
    /// </summary>
    [HttpPost("bulk-set")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> BulkSetPermissionsAsync(
        [FromBody] RoleTemplatePermissionBulkSetRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var modifiedCount = await _roleTemplatePermissionService.BulkSetPermissionsAsync(
            request.RoleId,
            request.TemplatePermissions,
            cancellationToken);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = $"Bulk set {modifiedCount} template permissions for role.",
            Data = new { ModifiedCount = modifiedCount }
        });
    }

    private static RoleTemplatePermissionDto MapToDto(RoleTemplatePermission permission) => new()
    {
        Id = permission.Id,
        RoleId = permission.RoleId,
        RoleName = permission.Role?.Name ?? string.Empty,
        RoleCode = permission.Role?.RoleCode ?? string.Empty,
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
        Title = "Role template permission not found.",
        Detail = $"Role template permission with id '{id}' was not found.",
        Status = StatusCodes.Status404NotFound,
        Type = "https://httpstatuses.com/404"
    };
}
