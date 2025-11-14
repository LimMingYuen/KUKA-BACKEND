using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API_Simulator.Models
{
    public class WorkflowDiagramDto
    {
        [JsonPropertyName("createUsername")]
        public string CreateUsername { get; set; } = string.Empty;

        [JsonPropertyName("createTime")]
        public string CreateTime { get; set; } = string.Empty;

        [JsonPropertyName("createTimeBegin")]
        public string? CreateTimeBegin { get; set; }

        [JsonPropertyName("createTimeEnd")]
        public string? CreateTimeEnd { get; set; }

        [JsonPropertyName("updateUsername")]
        public string UpdateUsername { get; set; } = string.Empty;

        [JsonPropertyName("updateTime")]
        public string UpdateTime { get; set; } = string.Empty;

        [JsonPropertyName("updateTimeBegin")]
        public string? UpdateTimeBegin { get; set; }

        [JsonPropertyName("updateTimeEnd")]
        public string? UpdateTimeEnd { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("workflowCode")]
        public string WorkflowCode { get; set; } = string.Empty;

        [JsonPropertyName("workflowOuterCode")]
        public string WorkflowOuterCode { get; set; } = string.Empty;

        [JsonPropertyName("workflowName")]
        public string WorkflowName { get; set; } = string.Empty;

        [JsonPropertyName("workflowModel")]
        public int WorkflowModel { get; set; }

        [JsonPropertyName("robotTypeClass")]
        public int RobotTypeClass { get; set; }

        [JsonPropertyName("mapCode")]
        public string MapCode { get; set; } = string.Empty;

        [JsonPropertyName("templateId")]
        public int? TemplateId { get; set; }

        [JsonPropertyName("templateCode")]
        public string? TemplateCode { get; set; }

        [JsonPropertyName("buttonName")]
        public string? ButtonName { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("nodeConfigList")]
        public object? NodeConfigList { get; set; }

        [JsonPropertyName("segmentConfigList")]
        public object? SegmentConfigList { get; set; }

        [JsonPropertyName("appointRobotList")]
        public object? AppointRobotList { get; set; }

        [JsonPropertyName("robots")]
        public object? Robots { get; set; }

        [JsonPropertyName("needConfirm")]
        public int NeedConfirm { get; set; }

        [JsonPropertyName("robotTypes")]
        public object? RobotTypes { get; set; }

        [JsonPropertyName("pdaEnable")]
        public object? PdaEnable { get; set; }

        [JsonPropertyName("users")]
        public object? Users { get; set; }

        [JsonPropertyName("robotStrategy")]
        public object? RobotStrategy { get; set; }

        [JsonPropertyName("autoCreateCommand")]
        public object? AutoCreateCommand { get; set; }

        [JsonPropertyName("lockRobotAfterFinish")]
        public int LockRobotAfterFinish { get; set; }

        [JsonPropertyName("keepOrderPoint")]
        public object? KeepOrderPoint { get; set; }

        [JsonPropertyName("workflowPriority")]
        public int WorkflowPriority { get; set; }

        [JsonPropertyName("preSelectedRobotId")]
        public int? PreSelectedRobotId { get; set; }

        [JsonPropertyName("preSelectedRobotCellCode")]
        public string? PreSelectedRobotCellCode { get; set; }

        [JsonPropertyName("targetAreaCode")]
        public string? TargetAreaCode { get; set; }
    }
}
