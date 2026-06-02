using Microsoft.AspNetCore.Http;

namespace IshikawaRca.Web.Models.Rca;

public class AddRcaEvidenceFileViewModel
{
    public Guid? CauseId { get; set; }

    public Guid? ExternalIntakeId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string EvidenceType { get; set; } = "Document";

    public string Source { get; set; } = "Manual";

    public string? Summary { get; set; }

    public string? ReferenceUri { get; set; }

    public DateTimeOffset? CapturedAt { get; set; }

    public string? CapturedByUserId { get; set; }

    public IFormFile? Attachment { get; set; }
}
