namespace IshikawaRca.Contracts.Rca;

public class RcaEvidenceDto
{
    public Guid Id { get; set; }

    public Guid RcaIncidentId { get; set; }

    public Guid? CauseId { get; set; }

    public Guid? ExternalIntakeId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string EvidenceType { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string? SourceDetail { get; set; }

    public string? Tags { get; set; }

    public string? Summary { get; set; }

    public string? ReferenceUri { get; set; }

    public string? AttachmentFileName { get; set; }

    public string? AttachmentContentType { get; set; }

    public long? AttachmentSizeBytes { get; set; }

    public string? AttachmentStorageProvider { get; set; }

    public string? AttachmentStorageKey { get; set; }

    public string? AttachmentSha256 { get; set; }

    public DateTimeOffset CapturedAt { get; set; }

    public string? CapturedByUserId { get; set; }

    public string ValidationStatus { get; set; } = string.Empty;

    public DateTimeOffset? ValidatedAt { get; set; }

    public string? ValidatedByUserId { get; set; }

    public string? ValidationNotes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
