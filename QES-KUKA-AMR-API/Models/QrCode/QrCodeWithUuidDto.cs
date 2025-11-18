namespace QES_KUKA_AMR_API.Models.QrCode;

public class QrCodeWithUuidDto
{
    public int Id { get; set; }
    public string NodeUuid { get; set; } = string.Empty;
    public string MapCode { get; set; } = string.Empty;
    public string FloorNumber { get; set; } = string.Empty;
    public int NodeNumber { get; set; }
}
