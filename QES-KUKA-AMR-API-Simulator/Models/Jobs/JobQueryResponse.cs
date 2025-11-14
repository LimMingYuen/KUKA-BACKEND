using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API_Simulator.Models.Jobs;

public class JobQueryResponse
{
    [JsonPropertyName("data")]
    public List<JobDto>? Data { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;
}
