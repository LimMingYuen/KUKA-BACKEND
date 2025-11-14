using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models.Missions;

public class MissionListData
{
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("totalRecords")]
    public int TotalRecords { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("content")]
    public List<MissionListItem> Content { get; set; } = new();
}
