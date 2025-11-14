using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API_Simulator.Models
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("succ")]
        public bool Succ { get; set; }
    }
}
