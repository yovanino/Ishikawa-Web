using System.ComponentModel.DataAnnotations;

namespace IshikawaRca.Web.Models.Rca;

public class EscalateRcaIncidentTo8DViewModel
{
    [StringLength(160)]
    public string? EscalatedByUserId { get; set; }

    [Required(ErrorMessage = "El motivo de escalamiento es obligatorio.")]
    [StringLength(4000)]
    public string EscalationReason { get; set; } = string.Empty;
}
