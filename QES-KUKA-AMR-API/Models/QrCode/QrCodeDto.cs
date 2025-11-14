namespace QES_KUKA_AMR_API.Models.QrCode;

public class QrCodeDto
{
    public int Id { get; set; }
    public string? CreateTime { get; set; }
    public string? CreateBy { get; set; }
    public string? CreateApp { get; set; }
    public string? LastUpdateTime { get; set; }
    public string? LastUpdateBy { get; set; }
    public string? LastUpdateApp { get; set; }
    public string? NodeLabel { get; set; }
    public int Reliability { get; set; }
    public string? MapCode { get; set; }
    public string? FloorNumber { get; set; }
    public int NodeNumber { get; set; }
    public int ReportTimes { get; set; }
}
