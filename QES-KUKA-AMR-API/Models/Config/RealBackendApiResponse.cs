using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models.Config;

/// <summary>
/// Response format for real Java backend API
/// </summary>
public class RealBackendApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}
