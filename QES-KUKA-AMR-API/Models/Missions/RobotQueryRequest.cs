using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models.Missions;

public class RobotQueryRequest
{
    [JsonPropertyName("robotId")]
    public string RobotId { get; set; } = string.Empty;

    [JsonPropertyName("robotType")]
    public string? RobotType { get; set; }

    [JsonPropertyName("mapCode")]
    public string? MapCode { get; set; }

    [JsonPropertyName("floorNumber")]
    public string? FloorNumber { get; set; }
}
