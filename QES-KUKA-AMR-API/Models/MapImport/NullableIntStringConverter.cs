using System.Text.Json;
using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models.MapImport;

/// <summary>
/// JSON converter that handles int fields that might be strings or null
/// Converts "0", "123", "" to nullable int
/// </summary>
public class NullableIntStringConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();

            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return null;
            }

            if (int.TryParse(stringValue, out var intValue))
            {
                return intValue;
            }

            return null;
        }

        throw new JsonException($"Unable to convert {reader.TokenType} to nullable int");
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

/// <summary>
/// JSON converter that handles int fields that might be strings (non-nullable)
/// Converts "0", "123" to int, "" to 0
/// </summary>
public class IntStringConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();

            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return 0;
            }

            if (int.TryParse(stringValue, out var intValue))
            {
                return intValue;
            }

            return 0;
        }

        throw new JsonException($"Unable to convert {reader.TokenType} to int");
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
