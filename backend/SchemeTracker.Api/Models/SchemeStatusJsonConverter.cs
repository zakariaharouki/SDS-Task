using System.Text.Json;
using System.Text.Json.Serialization;

namespace SchemeTracker.Api.Models;

/// <summary>
/// Accepts only the exact status name. The built-in JsonStringEnumConverter also accepts numbers,
/// including undefined ones like 99, and ignores case.
/// </summary>
public sealed class SchemeStatusJsonConverter : JsonConverter<SchemeStatus>
{
    public override SchemeStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String
            && SchemeStatusNames.TryParseExact(reader.GetString(), out var status))
        {
            return status;
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, SchemeStatus value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
