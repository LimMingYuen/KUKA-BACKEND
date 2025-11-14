namespace QES_KUKA_AMR_API.Models.QrCode;

public class QrCodeSummaryDto
{
    public int Id { get; set; }
    public string NodeLabel { get; set; } = string.Empty;
    public string MapCode { get; set; } = string.Empty;
    public string FloorNumber { get; set; } = string.Empty;
    public int NodeNumber { get; set; }
    public int Reliability { get; set; }
    public int ReportTimes { get; set; }
    public string LastUpdateTime { get; set; } = string.Empty;
}
