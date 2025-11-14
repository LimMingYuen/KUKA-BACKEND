using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models.Missions;

public class MissionDataItem
{
    [JsonPropertyName("sequence")]
    public int Sequence { get; set; }

    [JsonPropertyName("position")]
    public string Position { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("putDown")]
    public bool PutDown { get; set; }

    [JsonPropertyName("passStrategy")]
    public string PassStrategy { get; set; } = string.Empty;

    [JsonPropertyName("waitingMillis")]
    public int WaitingMillis { get; set; }
}
