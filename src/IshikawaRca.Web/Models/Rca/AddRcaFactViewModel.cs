using System.ComponentModel.DataAnnotations;

namespace IshikawaRca.Web.Models.Rca;

public class AddRcaFactViewModel
{
    public Guid? CauseId { get; set; }

    public Guid? EvidenceId { get; set; }

    public Guid? CorrectiveActionId { get; set; }

    public Guid? ExternalIntakeId { get; set; }

    public string FactType { get; set; } = "Observation";

    public string Source { get; set; } = "Manual";

    [StringLength(220)]
    public string? SourceDetail { get; set; }

    [Required]
    [StringLength(220)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    public DateTimeOffset? OccurredAt { get; set; }

    [StringLength(160)]
    public string? CapturedByUserId { get; set; }
}
