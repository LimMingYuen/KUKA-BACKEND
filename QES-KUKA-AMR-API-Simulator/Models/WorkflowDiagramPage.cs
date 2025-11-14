using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API_Simulator.Models
{
    public class WorkflowDiagramPage
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
        public IReadOnlyList<WorkflowDiagramDto> Content { get; set; } = Array.Empty<WorkflowDiagramDto>();
    }
}
