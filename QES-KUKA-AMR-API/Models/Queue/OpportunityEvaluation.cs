using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Models.Queue;

public class OpportunityEvaluation
{
    public OpportunityDecision Decision { get; set; }
    public string Reason { get; set; } = string.Empty;
    public MissionQueueItem? SelectedJob { get; set; }
    public double? DistanceToJob { get; set; }
    public int ConsecutiveJobCount { get; set; }
}
