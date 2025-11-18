using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models;

/// <summary>
/// Generic model for external API error responses
/// Handles different error formats from external AMR system
/// </summary>
public class ExternalApiErrorResponse
{
    /// <summary>
    /// Error code (e.g., "100001")
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Error message describing what went wrong
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Success flag (usually false for errors)
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Timestamp of the error (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    /// <summary>
    /// HTTP status code
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// Error type description (e.g., "Internal Server Error")
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Exception class name (e.g., "com.kuka.interfaces.plugin.common.InterfaceAPIException")
    /// </summary>
    [JsonPropertyName("exception")]
    public string? Exception { get; set; }

    /// <summary>
    /// API path where error occurred
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// Additional data (if any)
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}
