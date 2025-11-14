using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models.Config;

public class QueryWorkflowDiagramsRequest
{
    [JsonPropertyName("pageNum")]
    public int PageNum { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }
}
