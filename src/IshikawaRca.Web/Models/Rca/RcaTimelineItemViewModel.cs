namespace IshikawaRca.Web.Models.Rca;

public class RcaTimelineItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Kind { get; set; } = "incident";

    public string Label { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public string SourceSystem { get; set; } = string.Empty;

    public string? Severity { get; set; }

    public IReadOnlyList<string> Badges { get; set; } = [];

    public IReadOnlyList<string> References { get; set; } = [];

    public IReadOnlyList<string> IndustrialContext { get; set; } = [];
}
