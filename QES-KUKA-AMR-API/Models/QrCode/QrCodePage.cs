namespace QES_KUKA_AMR_API.Models.QrCode;

public class QrCodePage
{
    public int TotalPages { get; set; }
    public int TotalRecords { get; set; }
    public int Page { get; set; }
    public int Size { get; set; }
    public List<QrCodeDto>? Content { get; set; }
}
