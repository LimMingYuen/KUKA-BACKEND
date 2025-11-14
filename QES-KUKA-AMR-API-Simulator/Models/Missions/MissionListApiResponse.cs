using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API_Simulator.Models.Missions;

public class MissionListApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("code")]
    public object? Code { get; set; }

    [JsonPropertyName("message")]
    public object? Message { get; set; }

    [JsonPropertyName("data")]
    public MissionListData? Data { get; set; }
}
