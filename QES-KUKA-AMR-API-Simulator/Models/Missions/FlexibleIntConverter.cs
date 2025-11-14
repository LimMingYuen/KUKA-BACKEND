using System.Text.Json;
using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API_Simulator.Models.Missions;

/// <summary>
/// JSON converter that handles int values that may come as strings or numbers from the API
/// Supports both "85" and 85, as well as decimal strings like "85.5" (rounds to integer)
/// </summary>
public class FlexibleIntConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                // If it's a number, try to read it as int
                if (reader.TryGetInt32(out var intValue))
                {
                    return intValue;
                }
                // If it's a decimal number, round it
                if (reader.TryGetDouble(out var doubleValue))
                {
                    return (int)Math.Round(doubleValue);
                }
                return null;

            case JsonTokenType.String:
                // If it's a string, try to parse it
                var stringValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return null;
                }

                // Try parsing as integer first
                if (int.TryParse(stringValue, out var parsedInt))
                {
                    return parsedInt;
                }

                // Try parsing as double and round
                if (double.TryParse(stringValue, out var parsedDouble))
                {
                    return (int)Math.Round(parsedDouble);
                }

                return null;

            case JsonTokenType.Null:
                return null;

            default:
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
