using System.ComponentModel.DataAnnotations;

namespace IshikawaRca.Web.Models.Rca;

public class CloseRcaIncidentViewModel
{
    [StringLength(160)]
    public string? ClosedByUserId { get; set; }

    [Required(ErrorMessage = "El resumen de cierre es obligatorio.")]
    [StringLength(4000)]
    public string ClosureSummary { get; set; } = string.Empty;
}
