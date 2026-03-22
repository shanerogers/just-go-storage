using System.Text.Json;

namespace JustGo.Extractor.Worker.Extraction.Services;

public static class ExtractionSerialization
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static JsonDocument ToJsonDocument<T>(T value)
    {
        return JsonSerializer.SerializeToDocument(value, JsonOptions);
    }
}
