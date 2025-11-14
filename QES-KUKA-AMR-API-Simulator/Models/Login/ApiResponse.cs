namespace QES_KUKA_AMR_API_Simulator.Models.Login;

public class ApiResponse<T>
{
    public bool Success { get; set; }

    public T? Data { get; set; }

    public string? Code { get; set; }

    public string? Msg { get; set; }
}
