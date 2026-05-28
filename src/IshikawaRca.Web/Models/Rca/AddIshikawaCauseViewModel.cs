using System.ComponentModel.DataAnnotations;

namespace IshikawaRca.Web.Models.Rca;

public class AddIshikawaCauseViewModel
{
    [Required]
    public Guid BranchId { get; set; }

    public Guid? ParentCauseId { get; set; }

    [Required(ErrorMessage = "El titulo de la causa es obligatorio.")]
    [StringLength(220)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [Range(0, 5)]
    public int ProbabilityScore { get; set; } = 3;

    [Range(0, 5)]
    public int ImpactScore { get; set; } = 3;

    [Range(0, 5)]
    public int FrequencyScore { get; set; } = 3;

    public bool IsRootCause { get; set; }

    [StringLength(4000)]
    public string? EvidenceSummary { get; set; }
}
