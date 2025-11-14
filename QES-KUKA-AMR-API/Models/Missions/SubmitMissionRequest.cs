using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models.Missions;

public class SubmitMissionRequest
{
    [JsonPropertyName("orgId")]
    public string OrgId { get; set; } = string.Empty;

    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("missionCode")]
    public string MissionCode { get; set; } = string.Empty;

    [JsonPropertyName("missionType")]
    public string MissionType { get; set; } = string.Empty;

    [JsonPropertyName("viewBoardType")]
    public string ViewBoardType { get; set; } = string.Empty;

    [JsonPropertyName("robotModels")]
    public IReadOnlyList<string> RobotModels { get; set; } = Array.Empty<string>();

    [JsonPropertyName("robotIds")]
    public IReadOnlyList<string> RobotIds { get; set; } = Array.Empty<string>();

    [JsonPropertyName("robotType")]
    public string RobotType { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("containerModelCode")]
    public string ContainerModelCode { get; set; } = string.Empty;

    [JsonPropertyName("containerCode")]
    public string ContainerCode { get; set; } = string.Empty;

    [JsonPropertyName("templateCode")]
    public string TemplateCode { get; set; } = string.Empty;

    [JsonPropertyName("lockRobotAfterFinish")]
    public bool LockRobotAfterFinish { get; set; }

    [JsonPropertyName("unlockRobotId")]
    public string UnlockRobotId { get; set; } = string.Empty;

    [JsonPropertyName("unlockMissionCode")]
    public string UnlockMissionCode { get; set; } = string.Empty;

    [JsonPropertyName("idleNode")]
    public string IdleNode { get; set; } = string.Empty;

    [JsonPropertyName("missionData")]
    public IReadOnlyList<MissionDataItem>? MissionData { get; set; }
}
