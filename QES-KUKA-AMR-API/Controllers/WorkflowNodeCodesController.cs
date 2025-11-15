using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Services.WorkflowNodeCodes;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/workflow-node-codes")]
public class WorkflowNodeCodesController : ControllerBase
{
    private readonly IWorkflowNodeCodeService _workflowNodeCodeService;
    private readonly ILogger<WorkflowNodeCodesController> _logger;

    public WorkflowNodeCodesController(
        IWorkflowNodeCodeService workflowNodeCodeService,
        ILogger<WorkflowNodeCodesController> logger)
    {
        _workflowNodeCodeService = workflowNodeCodeService;
        _logger = logger;
    }

    /// <summary>
    /// Syncs node codes for all workflows from the external AMR API.
    /// This will query the external API for each workflow in parallel (with concurrency control)
    /// and update the database with the latest node codes.
    /// </summary>
    /// <param name="maxConcurrency">Maximum number of concurrent API calls (default: 10, max: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sync result with statistics</returns>
    [HttpPost("sync")]
    public async Task<ActionResult<WorkflowNodeCodeSyncResult>> SyncAllAsync(
        [FromQuery] int maxConcurrency = 10,
        CancellationToken cancellationToken = default)
    {
        // Validate and cap concurrency
        if (maxConcurrency < 1)
        {
            maxConcurrency = 1;
        }
        else if (maxConcurrency > 50)
        {
            maxConcurrency = 50;
        }

        _logger.LogInformation("Starting sync of all workflow node codes with max concurrency {MaxConcurrency}",
            maxConcurrency);

        try
        {
            var result = await _workflowNodeCodeService.SyncAllWorkflowNodeCodesAsync(
                maxConcurrency,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing all workflow node codes");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Error = "Failed to sync workflow node codes",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Syncs node codes for a specific workflow from the external AMR API
    /// </summary>
    /// <param name="externalWorkflowId">The external workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure status</returns>
    [HttpPost("sync/{externalWorkflowId}")]
    public async Task<ActionResult> SyncByWorkflowIdAsync(
        int externalWorkflowId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting sync for workflow {ExternalWorkflowId}", externalWorkflowId);

        try
        {
            var success = await _workflowNodeCodeService.SyncWorkflowNodeCodesAsync(
                externalWorkflowId,
                cancellationToken);

            if (success)
            {
                return Ok(new { Message = $"Successfully synced workflow {externalWorkflowId}" });
            }
            else
            {
                return BadRequest(new { Error = $"Failed to sync workflow {externalWorkflowId}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing workflow {ExternalWorkflowId}", externalWorkflowId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Error = $"Failed to sync workflow {externalWorkflowId}",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets all node codes for a specific workflow
    /// </summary>
    /// <param name="externalWorkflowId">The external workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of node codes</returns>
    [HttpGet("{externalWorkflowId}")]
    public async Task<ActionResult<List<string>>> GetNodeCodesAsync(
        int externalWorkflowId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nodeCodes = await _workflowNodeCodeService.GetWorkflowNodeCodesAsync(
                externalWorkflowId,
                cancellationToken);

            return Ok(nodeCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting node codes for workflow {ExternalWorkflowId}", externalWorkflowId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Error = $"Failed to get node codes for workflow {externalWorkflowId}",
                Message = ex.Message
            });
        }
    }
}
