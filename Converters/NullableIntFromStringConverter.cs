using System.Text.Json;
using System.Text.Json.Serialization;

namespace IPOClient.Converters
{
    /// <summary>
    /// Converts "-" or empty string to 0 for nullable int fields.
    /// </summary>
    public class NullableIntFromStringConverter : JsonConverter<int?>
    {
        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                if (string.IsNullOrWhiteSpace(str) || str == "-")
                    return 0;
                if (int.TryParse(str, out var parsed))
                    return parsed;
                return 0;
            }

            if (reader.TokenType == JsonTokenType.Number)
                return reader.GetInt32();

            if (reader.TokenType == JsonTokenType.Null)
                return 0;

            return 0;
        }

        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }
}
