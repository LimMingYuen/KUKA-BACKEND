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
