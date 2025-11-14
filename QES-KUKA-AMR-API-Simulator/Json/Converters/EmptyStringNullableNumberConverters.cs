using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API_Simulator.Json.Converters;

/// <summary>
/// Converts empty JSON strings to <c>null</c> when deserializing nullable integers.
/// Accepts both numeric tokens and string tokens.
/// </summary>
public sealed class EmptyStringNullableIntJsonConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number => reader.GetInt32(),
            JsonTokenType.String => ParseString(reader.GetString()),
            _ => throw new JsonException($"Unexpected token parsing nullable int. TokenType: {reader.TokenType}")
        };
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

    private static int? ParseString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        throw new JsonException($"Unable to convert \"{raw}\" to int.");
    }
}

/// <summary>
/// Converts empty JSON strings to <c>null</c> when deserializing nullable longs.
/// Accepts both numeric tokens and string tokens.
/// </summary>
public sealed class EmptyStringNullableLongJsonConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number => reader.GetInt64(),
            JsonTokenType.String => ParseString(reader.GetString()),
            _ => throw new JsonException($"Unexpected token parsing nullable long. TokenType: {reader.TokenType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
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

    private static long? ParseString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        throw new JsonException($"Unable to convert \"{raw}\" to long.");
    }
}
