using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LocalPlaylistMaster.Backend.Utilities
{
    public interface IMiscJsonUser
    {
        public string MiscJson { get; set; }

        public void UpdateJson(JsonElement root, string propertyName, JsonElement obj)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Name == propertyName) continue;
                    writer.WritePropertyName(property.Name);
                    property.Value.WriteTo(writer);
                }
                writer.WritePropertyName(propertyName);
                obj.WriteTo(writer);
                writer.WriteEndObject();
            }
            MiscJson = Encoding.UTF8.GetString(stream.ToArray());
        }

        public void UpdateJson(JsonElement root, string propertyName, JsonObject obj)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Name == propertyName) continue;
                    writer.WritePropertyName(property.Name);
                    property.Value.WriteTo(writer);
                }
                writer.WritePropertyName(propertyName);
                obj.WriteTo(writer);
                writer.WriteEndObject();
            }
            MiscJson = Encoding.UTF8.GetString(stream.ToArray());
        }

        public T? GetProperty<T>(string name)
        {
            using var doc = JsonDocument.Parse(MiscJson);
            var root = doc.RootElement;
            if (root.TryGetProperty(name, out JsonElement element))
            {
                return JsonSerializer.Deserialize<T>(element);
            }

            return default;
        }

        public void SetProperty<T>(string name, T property)
        {
            using var doc = JsonDocument.Parse(MiscJson);
            var root = doc.RootElement;
            JsonElement element = JsonSerializer.SerializeToElement(property);
            UpdateJson(root, name, element);
        }
    }
}
