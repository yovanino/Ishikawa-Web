using Microsoft.AspNetCore.Http;

namespace IshikawaRca.Web.Models.Rca;

public class AddRcaEvidenceFileViewModel
{
    public Guid? CauseId { get; set; }

    public Guid? ExternalIntakeId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string EvidenceType { get; set; } = "Document";

    public string Source { get; set; } = "Manual";

    public string? SourceDetail { get; set; }

    public string? Tags { get; set; }

    public string? Summary { get; set; }

    public string? ReferenceUri { get; set; }

    public DateTimeOffset? CapturedAt { get; set; }

    public string? CapturedByUserId { get; set; }

    public string ValidationStatus { get; set; } = "PendingReview";

    public DateTimeOffset? ValidatedAt { get; set; }

    public string? ValidatedByUserId { get; set; }

    public string? ValidationNotes { get; set; }

    public IFormFile? Attachment { get; set; }
}
