namespace QES_KUKA_AMR_API.Services.WorkflowNodeCodes;

/// <summary>
/// Service for syncing workflow node codes from external AMR API
/// </summary>
public interface IWorkflowNodeCodeService
{
    /// <summary>
    /// Syncs node codes for all workflows with external IDs from the external AMR API
    /// </summary>
    /// <param name="maxConcurrency">Maximum number of concurrent API calls (default: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sync result with statistics</returns>
    Task<WorkflowNodeCodeSyncResult> SyncAllWorkflowNodeCodesAsync(
        int maxConcurrency = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs node codes for a specific workflow by its external ID
    /// </summary>
    /// <param name="externalWorkflowId">The external workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if sync was successful, false otherwise</returns>
    Task<bool> SyncWorkflowNodeCodesAsync(
        int externalWorkflowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all node codes for a specific workflow
    /// </summary>
    /// <param name="externalWorkflowId">The external workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of node codes</returns>
    Task<List<string>> GetWorkflowNodeCodesAsync(
        int externalWorkflowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Classifies a workflow into a zone based on node code matching.
    /// Compares workflow node codes (in order) against MapZone.Nodes.
    /// Returns the first zone where ALL zone nodes exist in the workflow's node codes.
    /// </summary>
    /// <param name="externalWorkflowId">The external workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Zone classification result with zone name and code, or null if no match</returns>
    Task<WorkflowZoneClassification?> ClassifyWorkflowByZoneAsync(
        int externalWorkflowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs workflow node codes from external API and immediately classifies the workflow by zone.
    /// This is a convenience method that combines sync + classify in one call.
    /// </summary>
    /// <param name="externalWorkflowId">The external workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Zone classification result after sync, or null if sync failed or no zone match</returns>
    Task<WorkflowZoneClassification?> SyncAndClassifyWorkflowAsync(
        int externalWorkflowId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of workflow zone classification
/// </summary>
public class WorkflowZoneClassification
{
    /// <summary>
    /// The zone name where the workflow is classified
    /// </summary>
    public string ZoneName { get; set; } = string.Empty;

    /// <summary>
    /// The zone code
    /// </summary>
    public string ZoneCode { get; set; } = string.Empty;

    /// <summary>
    /// The map code
    /// </summary>
    public string MapCode { get; set; } = string.Empty;

    /// <summary>
    /// Number of zone nodes that matched
    /// </summary>
    public int MatchedNodesCount { get; set; }

    /// <summary>
    /// The zone nodes that were matched
    /// </summary>
    public List<string> MatchedNodes { get; set; } = new();
}

/// <summary>
/// Result of workflow node code sync operation
/// </summary>
public class WorkflowNodeCodeSyncResult
{
    /// <summary>
    /// Total number of workflows processed
    /// </summary>
    public int TotalWorkflows { get; set; }

    /// <summary>
    /// Number of workflows successfully synced
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of workflows that failed to sync
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Total number of node codes inserted
    /// </summary>
    public int NodeCodesInserted { get; set; }

    /// <summary>
    /// Total number of node codes deleted (removed from previous sync)
    /// </summary>
    public int NodeCodesDeleted { get; set; }

    /// <summary>
    /// List of workflow IDs that failed to sync
    /// </summary>
    public List<int> FailedWorkflowIds { get; set; } = new();

    /// <summary>
    /// Error messages for failed workflows
    /// </summary>
    public Dictionary<int, string> Errors { get; set; } = new();
}
