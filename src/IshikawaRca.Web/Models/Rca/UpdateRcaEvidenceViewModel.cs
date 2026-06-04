using System.ComponentModel.DataAnnotations;

namespace IshikawaRca.Web.Models.Rca;

public class UpdateRcaEvidenceViewModel
{
    [Required]
    public Guid EvidenceId { get; set; }

    public Guid? CauseId { get; set; }

    [Required(ErrorMessage = "El titulo de la evidencia es obligatorio.")]
    [StringLength(220)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string EvidenceType { get; set; } = "Observation";

    [Required]
    [StringLength(64)]
    public string Source { get; set; } = "Manual";

    [StringLength(220)]
    public string? SourceDetail { get; set; }

    [StringLength(500)]
    public string? Tags { get; set; }

    [StringLength(4000)]
    public string? Summary { get; set; }

    [StringLength(1000)]
    public string? ReferenceUri { get; set; }

    public DateTimeOffset? CapturedAt { get; set; }

    [StringLength(160)]
    public string? CapturedByUserId { get; set; }

    [Required]
    [StringLength(32)]
    public string ValidationStatus { get; set; } = "PendingReview";

    public DateTimeOffset? ValidatedAt { get; set; }

    [StringLength(160)]
    public string? ValidatedByUserId { get; set; }

    [StringLength(2000)]
    public string? ValidationNotes { get; set; }
}
