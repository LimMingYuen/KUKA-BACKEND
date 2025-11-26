using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API_Simulator.Models.MobileRobot;

public class RealtimeInfoResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public RealtimeInfoData? Data { get; set; }
}

public class RealtimeInfoData
{
    [JsonPropertyName("robotRealtimeList")]
    public List<RobotRealtimeDto> RobotRealtimeList { get; set; } = new();

    [JsonPropertyName("containerRealtimeList")]
    public List<ContainerRealtimeDto> ContainerRealtimeList { get; set; } = new();

    [JsonPropertyName("errorRobotList")]
    public List<RobotRealtimeDto> ErrorRobotList { get; set; } = new();
}
