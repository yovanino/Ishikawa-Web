namespace IshikawaRca.Contracts.Rca;

public class RcaClosureDocumentDto
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid RcaIncidentId { get; set; }

    public int Version { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/pdf";

    public long SizeBytes { get; set; }

    public string StorageProvider { get; set; } = string.Empty;

    public string StorageKey { get; set; } = string.Empty;

    public string Sha256 { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset GeneratedAt { get; set; }

    public string GeneratedByUserId { get; set; } = string.Empty;

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? ReviewedByUserId { get; set; }

    public string? ReviewNotes { get; set; }
}
