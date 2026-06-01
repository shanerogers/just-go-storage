using System.ComponentModel.DataAnnotations;

namespace JustGo.Grading;

public sealed class GradingOptions
{
    public const string SectionName = "Grading";

    [Range(1, int.MaxValue, ErrorMessage = "MaxConcurrentEnrichments must be greater than 0.")]
    public int MaxConcurrentEnrichments { get; set; } = 10;
}
