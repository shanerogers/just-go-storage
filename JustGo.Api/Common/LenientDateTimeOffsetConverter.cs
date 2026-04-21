using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustGo.Api.Common;

/// <summary>
/// Handles <see cref="DateTimeOffset"/> values returned by the JustGo API that lack a
/// timezone offset (e.g. <c>"2021-05-01T00:00:00"</c>). Such strings are treated as UTC.
/// </summary>
public sealed class LenientDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();

        if (raw is null)
        {
            throw new JsonException("Expected a date-time string but got null.");
        }

        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
        {
            return dto;
        }

        // Fall back: no offset present — treat as UTC
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
        {
            return new DateTimeOffset(dt, TimeSpan.Zero);
        }

        throw new JsonException($"Unable to parse \"{raw}\" as a DateTimeOffset.");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("O", CultureInfo.InvariantCulture));
    }
}
