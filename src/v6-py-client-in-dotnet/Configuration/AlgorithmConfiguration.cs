using System.Text.Json;
using System.Text.Json.Serialization;

namespace V6DotNet.Configuration;

public class AlgorithmConfiguration
{
    public string Name { get; set; }
    public string Description { get; set; }
    [JsonConverter(typeof(JsonStringArrayConverter))]
    public string[] DatabaseLabels { get; set; }
    public string Image { get; set; }
    public TaskInput Input { get; set; }
}

public class TaskInput
{
    public string Method { get; set; }
    public Dictionary<string, object> Kwargs { get; set; }
}

// Custom converter to handle single string to array conversion
public class JsonStringArrayConverter : JsonConverter<string[]>
{
    public override string[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new[] { reader.GetString() };
        }
        
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<string>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;
                list.Add(reader.GetString());
            }
            return list.ToArray();
        }
        
        throw new JsonException("Unexpected token type");
    }

    public override void Write(Utf8JsonWriter writer, string[] value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteStringValue(item);
        }
        writer.WriteEndArray();
    }
}