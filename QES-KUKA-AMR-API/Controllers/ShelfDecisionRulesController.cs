using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.ShelfDecisionRules;
using QES_KUKA_AMR_API.Services.ShelfDecisionRules;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/v1/shelf-decision-rules")]
public class ShelfDecisionRulesController : ControllerBase
{
    private readonly IShelfDecisionRuleService _ruleService;
    private readonly ILogger<ShelfDecisionRulesController> _logger;

    public ShelfDecisionRulesController(IShelfDecisionRuleService ruleService, ILogger<ShelfDecisionRulesController> logger)
    {
        _ruleService = ruleService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ShelfDecisionRuleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ShelfDecisionRuleDto>>>> GetRulesAsync(CancellationToken cancellationToken)
    {
        var rules = await _ruleService.GetAsync(cancellationToken);
        var dtos = rules.Select(MapToDto).ToList();
        return Ok(Success(dtos));
    }

    [HttpGet("{id:int}", Name = nameof(GetRuleByIdAsync))]
    [ProducesResponseType(typeof(ApiResponse<ShelfDecisionRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ShelfDecisionRuleDto>>> GetRuleByIdAsync(int id, CancellationToken cancellationToken)
    {
        var rule = await _ruleService.GetByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(Success(MapToDto(rule)));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ShelfDecisionRuleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ShelfDecisionRuleDto>>> CreateRuleAsync(
        [FromBody] ShelfDecisionRuleCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var entity = await _ruleService.CreateAsync(new ShelfDecisionRule
            {
                DisplayName = request.DisplayName,
                ActualValue = request.ActualValue,
                Description = request.Description,
                IsActive = request.IsActive
            }, cancellationToken);

            var dto = MapToDto(entity);
            return CreatedAtRoute(nameof(GetRuleByIdAsync), new { id = dto.Id }, Success(dto));
        }
        catch (ShelfDecisionRuleConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while creating shelf decision rule with actual value {ActualValue}", request.ActualValue);
            return Conflict(new ProblemDetails
            {
                Title = "Shelf decision rule already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ShelfDecisionRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ShelfDecisionRuleDto>>> UpdateRuleAsync(
        int id,
        [FromBody] ShelfDecisionRuleUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var updated = await _ruleService.UpdateAsync(id, new ShelfDecisionRule
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
        catch (ShelfDecisionRuleConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict while updating shelf decision rule {RuleId}", id);
            return Conflict(new ProblemDetails
            {
                Title = "Shelf decision rule already exists.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://httpstatuses.com/409"
            });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteRuleAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await _ruleService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(NotFoundProblem(id));
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = "Shelf decision rule deleted.",
            Data = null
        });
    }

    private static ShelfDecisionRuleDto MapToDto(ShelfDecisionRule rule) => new()
    {
        Id = rule.Id,
        DisplayName = rule.DisplayName,
        ActualValue = rule.ActualValue,
        Description = rule.Description,
        IsActive = rule.IsActive,
        CreatedUtc = rule.CreatedUtc,
        UpdatedUtc = rule.UpdatedUtc
    };

    private static ApiResponse<T> Success<T>(T data) => new()
    {
        Success = true,
        Data = data
    };

    private static ProblemDetails NotFoundProblem(int id) => new()
    {
        Title = "Shelf decision rule not found.",
        Detail = $"Shelf decision rule with id '{id}' was not found.",
        Status = StatusCodes.Status404NotFound,
        Type = "https://httpstatuses.com/404"
    };
}
