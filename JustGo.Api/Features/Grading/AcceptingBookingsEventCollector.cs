using System.Text.Json;

namespace JustGo.Api.Features.Grading;

/// <summary>
/// Collects grading events that members can still be booked onto. JustGo is paged through
/// <see cref="PageSize"/> events at a time and only "Accepting Bookings" events are kept.
/// Paging stops at the first page that comes back short of a full page.
/// </summary>
public static class AcceptingBookingsEventCollector
{
    public const string AcceptingBookingsStatus = "Accepting Bookings";
    public const int PageSize = 100;

    // Guards against an upstream that ignores PageNumber and keeps returning full pages.
    public const int MaxPages = 50;

    public static async Task<IReadOnlyList<JsonElement>> CollectAsync(
        Func<int, CancellationToken, Task<IReadOnlyList<JsonElement>>> fetchPage,
        CancellationToken ct)
    {
        var accepting = new List<JsonElement>();

        for (var pageNumber = 1; pageNumber <= MaxPages; pageNumber++)
        {
            var page = await fetchPage(pageNumber, ct);
            accepting.AddRange(page.Where(IsAcceptingBookings));

            if (page.Count < PageSize)
            {
                break;
            }
        }

        return accepting;
    }

    public static bool IsAcceptingBookings(JsonElement evt) =>
        evt.ValueKind == JsonValueKind.Object
        && evt.TryGetProperty("status", out var status)
        && status.ValueKind == JsonValueKind.String
        && string.Equals(status.GetString()?.Trim(), AcceptingBookingsStatus, StringComparison.OrdinalIgnoreCase);

    /// <summary>Reads the <c>data</c> array from a JustGo FindByAttributes response.</summary>
    public static IReadOnlyList<JsonElement> ReadEvents(object? response) =>
        response is JsonElement { ValueKind: JsonValueKind.Object } json
        && json.TryGetProperty("data", out var data)
        && data.ValueKind == JsonValueKind.Array
            ? [.. data.EnumerateArray()]
            : [];
}
