using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Services.WorkflowNodeCodes;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Authorize]
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
    /// Processes workflows sequentially (one by one) to ensure reliability.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sync result with statistics</returns>
    [HttpPost("sync")]
    public async Task<ActionResult<WorkflowNodeCodeSyncResult>> SyncAllAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting sequential sync of all workflow node codes");

        try
        {
            var result = await _workflowNodeCodeService.SyncAllWorkflowNodeCodesAsync(
                maxConcurrency: 1, // Sequential processing
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

    /// <summary>
    /// Classifies a workflow into a zone based on node code matching.
    /// Compares the workflow's node codes against MapZone.Nodes to find the first zone
    /// where ALL zone nodes exist in the workflow's node codes.
    /// </summary>
    /// <param name="externalWorkflowId">The external workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Zone classification result, or 404 if no matching zone found</returns>
    [HttpGet("{externalWorkflowId}/zone")]
    public async Task<ActionResult<WorkflowZoneClassification>> ClassifyByZoneAsync(
        int externalWorkflowId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var classification = await _workflowNodeCodeService.ClassifyWorkflowByZoneAsync(
                externalWorkflowId,
                cancellationToken);

            if (classification == null)
            {
                return NotFound(new
                {
                    Error = $"No matching zone found for workflow {externalWorkflowId}",
                    Message = "The workflow's node codes do not match any zone's nodes"
                });
            }

            return Ok(classification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error classifying workflow {ExternalWorkflowId} by zone", externalWorkflowId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Error = $"Failed to classify workflow {externalWorkflowId}",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Syncs and classifies ALL workflows with external IDs in one operation.
    /// This processes all workflows sequentially and saves results to the database.
    /// No need to specify individual workflow IDs - just call this endpoint once.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Summary of sync and classify results for all workflows</returns>
    [HttpPost("sync-and-classify-all")]
    public async Task<ActionResult<SyncAndClassifyAllResult>> SyncAndClassifyAllAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sync and classify all workflows request");

        try
        {
            var result = await _workflowNodeCodeService.SyncAndClassifyAllWorkflowsAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing and classifying all workflows");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Error = "Failed to sync and classify all workflows",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Syncs node codes from external API and immediately classifies the workflow by zone.
    /// This combined operation ensures classification uses fresh data without requiring
    /// separate sync and classify API calls.
    /// </summary>
    /// <param name="externalWorkflowId">The external workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Zone classification result after sync, or 404 if sync failed or no zone found</returns>
    [HttpPost("{externalWorkflowId}/sync-and-classify")]
    public async Task<ActionResult<WorkflowZoneClassification>> SyncAndClassifyAsync(
        int externalWorkflowId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sync and classify request for workflow {ExternalWorkflowId}", externalWorkflowId);

        try
        {
            var classification = await _workflowNodeCodeService.SyncAndClassifyWorkflowAsync(
                externalWorkflowId,
                cancellationToken);

            if (classification == null)
            {
                return NotFound(new
                {
                    Error = $"Failed to sync and classify workflow {externalWorkflowId}",
                    Message = "Either the sync failed or the workflow's node codes do not match any zone's nodes"
                });
            }

            return Ok(classification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing and classifying workflow {ExternalWorkflowId}", externalWorkflowId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Error = $"Failed to sync and classify workflow {externalWorkflowId}",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets all workflow zone mappings from the database.
    /// Returns the stored classification results for all workflows.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all workflow zone mappings</returns>
    [HttpGet("zone-mappings")]
    public async Task<ActionResult<IEnumerable<Data.Entities.WorkflowZoneMapping>>> GetAllZoneMappingsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mappings = await _workflowNodeCodeService.GetAllWorkflowZoneMappingsAsync(cancellationToken);
            return Ok(mappings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all workflow zone mappings");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Error = "Failed to get workflow zone mappings",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets a specific workflow zone mapping by external workflow ID.
    /// Returns the stored classification result for the workflow.
    /// </summary>
    /// <param name="externalWorkflowId">The external workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The workflow zone mapping, or 404 if not found</returns>
    [HttpGet("{externalWorkflowId}/zone-mapping")]
    public async Task<ActionResult<Data.Entities.WorkflowZoneMapping>> GetZoneMappingAsync(
        int externalWorkflowId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mapping = await _workflowNodeCodeService.GetWorkflowZoneMappingAsync(
                externalWorkflowId,
                cancellationToken);

            if (mapping == null)
            {
                return NotFound(new
                {
                    Error = $"No zone mapping found for workflow {externalWorkflowId}",
                    Message = "The workflow has not been classified yet. Use sync-and-classify endpoint first."
                });
            }

            return Ok(mapping);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow zone mapping for {ExternalWorkflowId}", externalWorkflowId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Error = $"Failed to get zone mapping for workflow {externalWorkflowId}",
                Message = ex.Message
            });
        }
    }
}
