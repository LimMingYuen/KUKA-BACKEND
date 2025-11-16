namespace QES_KUKA_AMR_API.Models.Queue;

public class NodePosition
{
    public string NodeLabel { get; set; } = string.Empty;
    public string MapCode { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
}
