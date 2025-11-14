using System.Text.Json.Serialization;
using QES_KUKA_AMR_API.Json.Converters;

namespace QES_KUKA_AMR_API.Models.Jobs;

public class JobQueryRequest
{
    [JsonConverter(typeof(EmptyStringNullableLongJsonConverter))]
    [JsonPropertyName("workflowId")]
    public long? WorkflowId { get; set; }

    [JsonPropertyName("containerCode")]
    public string? ContainerCode { get; set; }

    [JsonPropertyName("jobCode")]
    public string? JobCode { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("robotId")]
    public string? RobotId { get; set; }

    [JsonPropertyName("targetCellCode")]
    public string? TargetCellCode { get; set; }

    [JsonPropertyName("workflowName")]
    public string? WorkflowName { get; set; }

    [JsonPropertyName("workflowCode")]
    public string? WorkflowCode { get; set; }

    [JsonPropertyName("maps")]
    public List<string>? Maps { get; set; }

    [JsonPropertyName("createUsername")]
    public string? CreateUsername { get; set; }

    [JsonConverter(typeof(EmptyStringNullableIntJsonConverter))]
    [JsonPropertyName("sourceValue")]
    public int? SourceValue { get; set; }

    [JsonConverter(typeof(EmptyStringNullableIntJsonConverter))]
    [JsonPropertyName("limit")]
    public int? Limit { get; set; } = 10;
}
