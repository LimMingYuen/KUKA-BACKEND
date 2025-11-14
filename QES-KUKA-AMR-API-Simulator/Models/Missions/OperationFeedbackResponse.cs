using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API_Simulator.Models.Missions;

public class OperationFeedbackResponse
{
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = "0";

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}
