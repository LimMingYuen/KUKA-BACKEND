namespace QES_KUKA_AMR_API_Simulator.Models.QrCode;

public class QrCodeDto
{
    public int Id { get; set; }
    public string CreateTime { get; set; } = string.Empty;
    public string CreateBy { get; set; } = string.Empty;
    public string CreateApp { get; set; } = string.Empty;
    public string LastUpdateTime { get; set; } = string.Empty;
    public string LastUpdateBy { get; set; } = string.Empty;
    public string LastUpdateApp { get; set; } = string.Empty;
    public string NodeLabel { get; set; } = string.Empty;
    public int Reliability { get; set; }
    public string MapCode { get; set; } = string.Empty;
    public string FloorNumber { get; set; } = string.Empty;
    public int NodeNumber { get; set; }
    public int ReportTimes { get; set; }
}
