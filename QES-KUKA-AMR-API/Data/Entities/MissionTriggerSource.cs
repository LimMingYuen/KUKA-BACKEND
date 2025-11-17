namespace QES_KUKA_AMR_API.Data.Entities;

/// <summary>
/// Represents how a mission was triggered or created
/// </summary>
public enum MissionTriggerSource
{
    /// <summary>
    /// Mission manually triggered by user
    /// </summary>
    Manual = 0,

    /// <summary>
    /// Mission triggered by a scheduled task
    /// </summary>
    Scheduled = 1,

    /// <summary>
    /// Mission triggered by a workflow
    /// </summary>
    Workflow = 2,

    /// <summary>
    /// Mission triggered via API call
    /// </summary>
    API = 3,

    /// <summary>
    /// Mission submitted directly to AMR system
    /// </summary>
    Direct = 4
}
