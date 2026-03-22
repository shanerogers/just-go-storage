namespace JustGo.Extractor.Worker.Extraction.Configuration;

public sealed class JustGoExtractionOptions
{
    public const string SectionName = "JustGoExtraction";

    public string TimeZoneId { get; set; } = "Australia/Sydney";
    public string DailyCron { get; set; } = "0 0 2 * * ?";
    public int PageSize { get; set; } = 100;
    public bool EnrichDetails { get; set; } = false;
}
