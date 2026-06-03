namespace IshikawaRca.Contracts.Rca;

public class UpdateRcaEvidenceRequest
{
    public Guid? CauseId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string EvidenceType { get; set; } = "Observation";

    public string Source { get; set; } = "Manual";

    public string? Summary { get; set; }

    public string? ReferenceUri { get; set; }

    public DateTimeOffset? CapturedAt { get; set; }

    public string? CapturedByUserId { get; set; }
}
