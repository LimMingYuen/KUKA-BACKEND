using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.ResumeStrategies;
using QES_KUKA_AMR_API.Services.ResumeStrategies;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/resume-strategies")]
public class ResumeStrategiesController : ControllerBase
{
    private readonly IResumeStrategyService _strategyService;
    private readonly ILogger<ResumeStrategiesController> _logger;

    public ResumeStrategiesController(IResumeStrategyService strategyService, ILogger<ResumeStrategiesController> logger)
    {
        _strategyService = strategyService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ResumeStrategyDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ResumeStrategyDto>>>> GetStrategiesAsync(CancellationToken cancellationToken)
    {
        var strategies = await _strategyService.GetAsync(cancellationToken);
        var dtos = strategies.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    [HttpGet("{id:int}", Name = nameof(GetStrategyByIdAsync))]
    [ProducesResponseType(typeof(ApiResponse<ResumeStrategyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ResumeStrategyDto>>> GetStrategyByIdAsync(int id, CancellationToken cancellationToken)
    {
        var strategy = await _strategyService.GetByIdAsync(id, cancellationToken);
        if (strategy is null)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(Success(MapToDto(strategy)));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ResumeStrategyDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ResumeStrategyDto>>> CreateStrategyAsync(
        [FromBody] ResumeStrategyCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var entity = await _strategyService.CreateAsync(new ResumeStrategy
            {
                DisplayName = request.DisplayName,
                ActualValue = request.ActualValue,
                Description = request.Description,
                IsActive = request.IsActive
            }, cancellationToken);

            var dto = MapToDto(entity);
            return CreatedAtRoute(nameof(GetStrategyByIdAsync), new { id = dto.Id }, Success(dto));
        }
        catch (ResumeStrategyConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while creating resume strategy with actual value {ActualValue}", request.ActualValue);
            return Conflict(new ProblemDetails
            {
                Title = "Resume strategy already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ResumeStrategyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ResumeStrategyDto>>> UpdateStrategyAsync(
        int id,
        [FromBody] ResumeStrategyUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var updated = await _strategyService.UpdateAsync(id, new ResumeStrategy
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
        catch (ResumeStrategyConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while updating resume strategy {ResumeStrategyId}", id);
            return Conflict(new ProblemDetails
            {
                Title = "Resume strategy already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteStrategyAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await _strategyService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = "Resume strategy deleted.",
            Data = null
        });
    }

    private static ResumeStrategyDto MapToDto(ResumeStrategy strategy) => new()
    {
        Id = strategy.Id,
        DisplayName = strategy.DisplayName,
        ActualValue = strategy.ActualValue,
        Description = strategy.Description,
        IsActive = strategy.IsActive,
        CreatedUtc = strategy.CreatedUtc,
        UpdatedUtc = strategy.UpdatedUtc
    };

    private static ApiResponse<T> Success<T>(T data) => new()
    {
        Success = true,
        Data = data
    };

    private static ProblemDetails NotFoundProblem(int id) => new()
    {
        Title = "Resume strategy not found.",
        Detail = $"Resume strategy with id '{id}' was not found.",
        Status = StatusCodes.Status404NotFound,
        Type = "https://httpstatuses.com/404"
    };
}
