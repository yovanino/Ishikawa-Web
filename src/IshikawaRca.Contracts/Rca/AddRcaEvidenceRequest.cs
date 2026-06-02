namespace IshikawaRca.Contracts.Rca;

public class AddRcaEvidenceRequest
{
    public Guid? CauseId { get; set; }

    public Guid? ExternalIntakeId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string EvidenceType { get; set; } = "Observation";

    public string Source { get; set; } = "Manual";

    public string? Summary { get; set; }

    public string? ReferenceUri { get; set; }

    public string? AttachmentFileName { get; set; }

    public string? AttachmentContentType { get; set; }

    public long? AttachmentSizeBytes { get; set; }

    public string? AttachmentStorageProvider { get; set; }

    public string? AttachmentStorageKey { get; set; }

    public string? AttachmentSha256 { get; set; }

    public DateTimeOffset? CapturedAt { get; set; }

    public string? CapturedByUserId { get; set; }
}
