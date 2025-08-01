namespace EventStore.StreamConnectors {
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class EmptyGuidConverter : JsonConverter<Guid> {
        public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var str = reader.GetString();
            return string.IsNullOrWhiteSpace(str) ? Guid.Empty : Guid.Parse(str);
        }

        public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options) {
            writer.WriteStringValue(value.ToString("N"));
        }
    }
}
