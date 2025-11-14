namespace QES_KUKA_AMR_API_Simulator.Models.Missions;

public class MissionListRequest
{
    public int PageNum { get; set; }
    public int PageSize { get; set; }
    public MissionQuery? Query { get; set; }
}

public class MissionQuery
{
    public string? BeginTimeStart { get; set; }
    public string? BeginTimeEnd { get; set; }
    public string? RobotId { get; set; }
    public string? RobotTypeCode { get; set; }
    public string? TemplateCode { get; set; }
    public string? TemplateName { get; set; }
}
