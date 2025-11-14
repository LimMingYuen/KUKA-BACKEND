using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models.Missions;

public class OperationFeedbackRequest
{
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("missionCode")]
    public string MissionCode { get; set; } = string.Empty;

    [JsonPropertyName("containerCode")]
    public string ContainerCode { get; set; } = string.Empty;

    [JsonPropertyName("position")]
    public string Position { get; set; } = string.Empty;
}
