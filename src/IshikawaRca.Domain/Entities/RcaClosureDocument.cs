using IshikawaRca.Domain.Common;
using IshikawaRca.Domain.Enums;

namespace IshikawaRca.Domain.Entities;

public class RcaClosureDocument : TenantEntity
{
    public Guid RcaIncidentId { get; set; }

    public int Version { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/pdf";

    public long SizeBytes { get; set; }

    public string StorageProvider { get; set; } = string.Empty;

    public string StorageKey { get; set; } = string.Empty;

    public string Sha256 { get; set; } = string.Empty;

    public RcaClosureDocumentStatus Status { get; set; } = RcaClosureDocumentStatus.Draft;

    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;

    public string GeneratedByUserId { get; set; } = string.Empty;

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? ReviewedByUserId { get; set; }

    public string? ReviewNotes { get; set; }
}
