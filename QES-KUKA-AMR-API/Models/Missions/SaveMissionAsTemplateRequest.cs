using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models.Missions;

/// <summary>
/// Request to save a mission submission as a reusable template
/// </summary>
public class SaveMissionAsTemplateRequest
{
    /// <summary>
    /// User-friendly name for the saved mission template
    /// </summary>
    [Required(ErrorMessage = "Mission name is required")]
    [MaxLength(200, ErrorMessage = "Mission name cannot exceed 200 characters")]
    [JsonPropertyName("missionName")]
    public string MissionName { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this mission does
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The mission submission request to save as a template
    /// </summary>
    [Required(ErrorMessage = "Mission request is required")]
    [JsonPropertyName("missionRequest")]
    public SubmitMissionRequest MissionRequest { get; set; } = new();
}

/// <summary>
/// Response after saving a mission as a template
/// </summary>
public class SaveMissionAsTemplateResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("savedMissionId")]
    public int? SavedMissionId { get; set; }

    [JsonPropertyName("missionName")]
    public string MissionName { get; set; } = string.Empty;
}
