namespace JustGo.Extractor.Worker.Extraction.Configuration;

public static class QuartzJobSettings
{
    public const string SchedulerName = "justgo-extractor-scheduler";
    public const string ExtractJobName = "justgo-daily-extraction";
    public const string ExtractJobGroup = "extraction";
    public const string TriggerName = "justgo-daily-trigger";
    public const string TriggerGroup = "extraction";
}
