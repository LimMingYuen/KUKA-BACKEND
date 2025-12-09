using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.OrganizationIds;
using QES_KUKA_AMR_API.Services.OrganizationIds;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/organization-ids")]
public class OrganizationIdsController : ControllerBase
{
    private readonly IOrganizationIdService _organizationIdService;
    private readonly ILogger<OrganizationIdsController> _logger;

    public OrganizationIdsController(IOrganizationIdService organizationIdService, ILogger<OrganizationIdsController> logger)
    {
        _organizationIdService = organizationIdService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<OrganizationIdDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<OrganizationIdDto>>>> GetOrganizationIdsAsync(CancellationToken cancellationToken)
    {
        var organizationIds = await _organizationIdService.GetAsync(cancellationToken);
        var dtos = organizationIds.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    [HttpGet("{id:int}", Name = nameof(GetOrganizationIdByIdAsync))]
    [ProducesResponseType(typeof(ApiResponse<OrganizationIdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OrganizationIdDto>>> GetOrganizationIdByIdAsync(int id, CancellationToken cancellationToken)
    {
        var organizationId = await _organizationIdService.GetByIdAsync(id, cancellationToken);
        if (organizationId is null)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(Success(MapToDto(organizationId)));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrganizationIdDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<OrganizationIdDto>>> CreateOrganizationIdAsync(
        [FromBody] OrganizationIdCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var entity = await _organizationIdService.CreateAsync(new OrganizationId
            {
                DisplayName = request.DisplayName,
                ActualValue = request.ActualValue,
                Description = request.Description,
                IsActive = request.IsActive
            }, cancellationToken);

            var dto = MapToDto(entity);
            var response = Success(dto);

            return CreatedAtRoute(nameof(GetOrganizationIdByIdAsync), new { id = dto.Id }, response);
        }
        catch (OrganizationIdConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while creating organization ID with actual value {ActualValue}", request.ActualValue);
            return Conflict(new ProblemDetails
            {
                Title = "Organization ID already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<OrganizationIdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<OrganizationIdDto>>> UpdateOrganizationIdAsync(
        int id,
        [FromBody] OrganizationIdUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var updated = await _organizationIdService.UpdateAsync(id, new OrganizationId
            {
                DisplayName = request.DisplayName,
                ActualValue = request.ActualValue,
                Description = request.Description,
                IsActive = request.IsActive
            }, cancellationToken);

            if (updated is null)
            {
                return NotFound(NotFoundProblem(id));
            }

            return Ok(Success(MapToDto(updated)));
        }
        catch (OrganizationIdConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while updating organization ID {OrganizationIdId}", id);
            return Conflict(new ProblemDetails
            {
                Title = "Organization ID already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteOrganizationIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _organizationIdService.DeleteAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound(NotFoundProblem(id));
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Msg = "Organization ID deleted.",
                Data = null
            });
        }
        catch (OrganizationIdValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error while deleting organization ID {Id}", id);
            return BadRequest(new ProblemDetails
            {
                Title = "Cannot delete organization ID.",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = "https://httpstatuses.com/400"
            });
        }
    }

    [HttpPatch("{id:int}/toggle-status")]
    [ProducesResponseType(typeof(ApiResponse<OrganizationIdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OrganizationIdDto>>> ToggleOrganizationIdStatusAsync(int id, CancellationToken cancellationToken)
    {
        var toggled = await _organizationIdService.ToggleStatusAsync(id, cancellationToken);
        if (toggled is null)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(Success(MapToDto(toggled)));
    }

    private static OrganizationIdDto MapToDto(OrganizationId organizationId) => new()
    {
        Id = organizationId.Id,
        DisplayName = organizationId.DisplayName,
        ActualValue = organizationId.ActualValue,
        Description = organizationId.Description,
        IsActive = organizationId.IsActive,
        CreatedUtc = organizationId.CreatedUtc,
        UpdatedUtc = organizationId.UpdatedUtc
    };

    private static ApiResponse<T> Success<T>(T data) => new()
    {
        Success = true,
        Data = data
    };

    private static ProblemDetails NotFoundProblem(int id) => new()
    {
        Title = "Organization ID not found.",
        Detail = $"Organization ID with id '{id}' was not found.",
        Status = StatusCodes.Status404NotFound,
        Type = "https://httpstatuses.com/404"
    };
}
