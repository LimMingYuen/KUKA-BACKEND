using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models;

public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("message")]
    public string? Message
    {
        get => Msg;
        set => Msg = value;
    }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}
