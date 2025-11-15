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