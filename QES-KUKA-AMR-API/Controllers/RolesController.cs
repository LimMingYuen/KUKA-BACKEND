using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.Roles;
using QES_KUKA_AMR_API.Services.Roles;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/roles")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IRoleService roleService, ILogger<RolesController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    /// <summary>
    /// Get all roles
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<RoleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await _roleService.GetAsync(cancellationToken);
        var dtos = roles.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    [HttpGet("{id:int}", Name = nameof(GetRoleByIdAsync))]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetRoleByIdAsync(int id, CancellationToken cancellationToken)
    {
        var role = await _roleService.GetByIdAsync(id, cancellationToken);
        if (role is null)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(Success(MapToDto(role)));
    }

    /// <summary>
    /// Get role by role code
    /// </summary>
    [HttpGet("code/{roleCode}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetRoleByCodeAsync(string roleCode, CancellationToken cancellationToken)
    {
        var role = await _roleService.GetByRoleCodeAsync(roleCode, cancellationToken);
        if (role is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Role not found.",
                Detail = $"Role with code '{roleCode}' was not found.",
                Status = StatusCodes.Status404NotFound,
                Type = "https://httpstatuses.com/404"
            });
        }

        return Ok(Success(MapToDto(role)));
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRoleAsync(
        [FromBody] RoleCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var entity = await _roleService.CreateAsync(new Role
            {
                Name = request.Name,
                RoleCode = request.RoleCode,
                IsProtected = request.IsProtected
            }, cancellationToken);

            var dto = MapToDto(entity);
            return CreatedAtRoute(nameof(GetRoleByIdAsync), new { id = dto.Id }, Success(dto));
        }
        catch (RoleConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while creating role with code {RoleCode}", request.RoleCode);
            return Conflict(new ProblemDetails
            {
                Title = "Role already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    /// <summary>
    /// Update an existing role
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRoleAsync(
        int id,
        [FromBody] RoleUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var updated = await _roleService.UpdateAsync(id, new Role
            {
                Name = request.Name,
                RoleCode = request.RoleCode,
                IsProtected = request.IsProtected
            }, cancellationToken);

            if (updated is null)
            {
                return NotFound(NotFoundProblem(id));
            }

            return Ok(Success(MapToDto(updated)));
        }
        catch (RoleConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while updating role {RoleId}", id);
            return Conflict(new ProblemDetails
            {
                Title = "Role already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    /// <summary>
    /// Delete a role (protected roles cannot be deleted)
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteRoleAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _roleService.DeleteAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound(NotFoundProblem(id));
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Msg = "Role deleted.",
                Data = null
            });
        }
        catch (RoleProtectedException ex)
        {
            _logger.LogWarning(ex, "Attempted to delete protected role {RoleId}", id);
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Role is protected.",
                Detail = ex.Message,
                Status = StatusCodes.Status403Forbidden,
                Type = "https://httpstatuses.com/403"
            });
        }
    }

    private static RoleDto MapToDto(Role role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        RoleCode = role.RoleCode,
        IsProtected = role.IsProtected,
        CreatedUtc = role.CreatedUtc,
        UpdatedUtc = role.UpdatedUtc
    };

    private static ApiResponse<T> Success<T>(T data) => new()
    {
        Success = true,
        Data = data
    };

    private static ProblemDetails NotFoundProblem(int id) => new()
    {
        Title = "Role not found.",
        Detail = $"Role with id '{id}' was not found.",
        Status = StatusCodes.Status404NotFound,
        Type = "https://httpstatuses.com/404"
    };
}
