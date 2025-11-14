using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API_Simulator.Models.Missions;

public class MissionListItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("createTime")]
    public string CreateTime { get; set; } = string.Empty;

    [JsonPropertyName("createBy")]
    public string CreateBy { get; set; } = string.Empty;

    [JsonPropertyName("createApp")]
    public string CreateApp { get; set; } = string.Empty;

    [JsonPropertyName("lastUpdateTime")]
    public string LastUpdateTime { get; set; } = string.Empty;

    [JsonPropertyName("lastUpdateBy")]
    public string LastUpdateBy { get; set; } = string.Empty;

    [JsonPropertyName("lastUpdateApp")]
    public string LastUpdateApp { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("missionType")]
    public int MissionType { get; set; }

    [JsonPropertyName("robotId")]
    public string RobotId { get; set; } = string.Empty;

    [JsonPropertyName("robotTypeCode")]
    public string RobotTypeCode { get; set; } = string.Empty;

    [JsonPropertyName("templateCode")]
    public string TemplateCode { get; set; } = string.Empty;

    [JsonPropertyName("templateName")]
    public string TemplateName { get; set; } = string.Empty;

    [JsonPropertyName("mapCode")]
    public string MapCode { get; set; } = string.Empty;

    [JsonPropertyName("floorNumber")]
    public string FloorNumber { get; set; } = string.Empty;

    [JsonPropertyName("extraInfo")]
    public string ExtraInfo { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("statusName")]
    public string StatusName { get; set; } = string.Empty;

    [JsonPropertyName("statusInfo")]
    public string StatusInfo { get; set; } = string.Empty;

    [JsonPropertyName("statusInfoI18n")]
    public object? StatusInfoI18n { get; set; }

    [JsonPropertyName("source")]
    public int Source { get; set; }

    [JsonPropertyName("beginTime")]
    public string BeginTime { get; set; } = string.Empty;

    [JsonPropertyName("endTime")]
    public string EndTime { get; set; } = string.Empty;

    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;

    [JsonPropertyName("errorInfo")]
    public string ErrorInfo { get; set; } = string.Empty;

    [JsonPropertyName("lockStatus")]
    public int LockStatus { get; set; }

    [JsonPropertyName("manualStatus")]
    public int ManualStatus { get; set; }

    [JsonPropertyName("errorLevel")]
    public int ErrorLevel { get; set; }

    [JsonPropertyName("targetContainerCode")]
    public string TargetContainerCode { get; set; } = string.Empty;

    [JsonPropertyName("targetNodeNumber")]
    public int TargetNodeNumber { get; set; }

    [JsonPropertyName("jobCode")]
    public string? JobCode { get; set; }
}
