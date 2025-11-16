using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models.Missions;

public class SubmitMissionResponse
{
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = "0";

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }
}

public class SubmitMissionResponseData
{
    [JsonPropertyName("queueItemCodes")]
    public List<string> QueueItemCodes { get; set; } = new();

    [JsonPropertyName("queueItemCount")]
    public int QueueItemCount { get; set; }

    [JsonPropertyName("isMultiMap")]
    public bool IsMultiMap { get; set; }

    [JsonPropertyName("primaryMapCode")]
    public string PrimaryMapCode { get; set; } = string.Empty;
}
