using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models.Missions;

public class MissionListRequest
{
    [JsonPropertyName("pageNum")]
    public int PageNum { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("query")]
    public MissionListQuery? Query { get; set; }
}

public class MissionListQuery
{
    [JsonPropertyName("beginTimeStart")]
    public string? BeginTimeStart { get; set; }

    [JsonPropertyName("beginTimeEnd")]
    public string? BeginTimeEnd { get; set; }

    [JsonPropertyName("robotId")]
    public string? RobotId { get; set; }

    [JsonPropertyName("robotTypeCode")]
    public string? RobotTypeCode { get; set; }

    [JsonPropertyName("templateCode")]
    public string? TemplateCode { get; set; }

    [JsonPropertyName("templateName")]
    public string? TemplateName { get; set; }
}
