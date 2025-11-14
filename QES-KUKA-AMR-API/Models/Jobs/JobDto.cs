using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models.Jobs;

public class JobDto
{
    [JsonPropertyName("jobCode")]
    public string JobCode { get; set; } = string.Empty;

    [JsonPropertyName("workflowId")]
    public long? WorkflowId { get; set; }

    [JsonPropertyName("containerCode")]
    public string? ContainerCode { get; set; }

    [JsonPropertyName("robotId")]
    public string? RobotId { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("workflowName")]
    public string? WorkflowName { get; set; }

    [JsonPropertyName("workflowCode")]
    public string? WorkflowCode { get; set; }

    [JsonPropertyName("workflowPriority")]
    public int? WorkflowPriority { get; set; }

    [JsonPropertyName("mapCode")]
    public string? MapCode { get; set; }

    [JsonPropertyName("targetCellCode")]
    public string? TargetCellCode { get; set; }

    [JsonPropertyName("beginCellCode")]
    public string? BeginCellCode { get; set; }

    [JsonPropertyName("targetCellCodeForeign")]
    public string? TargetCellCodeForeign { get; set; }

    [JsonPropertyName("beginCellCodeForeign")]
    public string? BeginCellCodeForeign { get; set; }

    [JsonPropertyName("finalNodeCode")]
    public string? FinalNodeCode { get; set; }

    [JsonPropertyName("warnFlag")]
    public int WarnFlag { get; set; }

    [JsonPropertyName("warnCode")]
    public string? WarnCode { get; set; }

    [JsonPropertyName("completeTime")]
    public string? CompleteTime { get; set; }

    [JsonPropertyName("spendTime")]
    public int? SpendTime { get; set; }

    [JsonPropertyName("createUsername")]
    public string? CreateUsername { get; set; }

    [JsonPropertyName("createTime")]
    public string CreateTime { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("materialsInfo")]
    public string? MaterialsInfo { get; set; }
}
