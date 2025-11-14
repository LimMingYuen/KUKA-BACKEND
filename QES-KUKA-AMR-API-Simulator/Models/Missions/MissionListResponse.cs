namespace QES_KUKA_AMR_API_Simulator.Models.Missions;

public class MissionListResponse
{
    public MissionListData? Data { get; set; }
}

public class MissionListData
{
    public int TotalPages { get; set; }
    public int TotalRecords { get; set; }
    public int Page { get; set; }
    public int Size { get; set; }
    public List<MissionListItem> Content { get; set; } = new();
}
