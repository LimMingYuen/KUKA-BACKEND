namespace QES_KUKA_AMR_API.Models.Workflow;

public class WorkflowSummaryDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Number { get; set; } = string.Empty;

    public string ExternalCode { get; set; } = string.Empty;

    public int Status { get; set; }

    public string LayoutCode { get; set; } = string.Empty;

    public int ActiveSchedulesCount { get; set; }
}
