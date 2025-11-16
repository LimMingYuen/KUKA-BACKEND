namespace QES_KUKA_AMR_API.Data.Entities;

public enum QueueStatus
{
    Queued = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

public enum MissionTriggerSource
{
    /// <summary>
    /// Mission manually triggered by user through UI or direct API call
    /// </summary>
    Manual = 0,

    /// <summary>
    /// Mission triggered by a saved mission schedule
    /// </summary>
    Scheduled = 1,

    /// <summary>
    /// Mission triggered as part of a workflow execution
    /// </summary>
    Workflow = 2,

    /// <summary>
    /// Mission triggered by external system via API
    /// </summary>
    API = 3,

    /// <summary>
    /// Direct mission submission (not from saved template)
    /// </summary>
    Direct = 4
}

public enum MissionQueueStatus
{
    /// <summary>
    /// Mission waiting in queue
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Mission eligible for robot assignment
    /// </summary>
    ReadyToAssign = 1,

    /// <summary>
    /// Robot assigned, not yet submitted to AMR
    /// </summary>
    Assigned = 2,

    /// <summary>
    /// Submitted to external AMR system
    /// </summary>
    SubmittedToAmr = 3,

    /// <summary>
    /// Robot currently executing mission
    /// </summary>
    Executing = 4,

    /// <summary>
    /// Mission successfully finished
    /// </summary>
    Completed = 5,

    /// <summary>
    /// Mission execution failed
    /// </summary>
    Failed = 6,

    /// <summary>
    /// Mission cancelled by user or system
    /// </summary>
    Cancelled = 7
}

public enum OpportunityDecision
{
    /// <summary>
    /// Evaluation pending
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Found and chained next job
    /// </summary>
    JobChained = 1,

    /// <summary>
    /// No suitable job, returning to original map
    /// </summary>
    ReturnToOriginal = 2,

    /// <summary>
    /// Hit consecutive job limit
    /// </summary>
    LimitReached = 3,

    /// <summary>
    /// No pending jobs available in current map
    /// </summary>
    NoJobsAvailable = 4
}