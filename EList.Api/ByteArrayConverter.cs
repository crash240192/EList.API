using System.Text.Json;
using System.Text.Json.Serialization;

namespace EList.Api
{
    /// <summary>
    /// byte[] ↔ base64; null/пустая строка → пустой массив (event_categories.ico NOT NULL).
    /// </summary>
    internal sealed class ByteArrayConverter : JsonConverter<byte[]>
    {
        public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return Array.Empty<byte>();

            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"Expected string for byte[], got {reader.TokenType}");

            var s = reader.GetString();
            if (string.IsNullOrEmpty(s))
                return Array.Empty<byte>();

            try
            {
                return Convert.FromBase64String(s);
            }
            catch (FormatException ex)
            {
                throw new JsonException("Invalid base64 for byte[] field", ex);
            }
        }

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
        {
            if (value == null || value.Length == 0)
            {
                writer.WriteStringValue(string.Empty);
                return;
            }

            writer.WriteBase64StringValue(value);
        }
    }
}
