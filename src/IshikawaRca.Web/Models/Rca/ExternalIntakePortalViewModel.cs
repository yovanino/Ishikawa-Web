using System.ComponentModel.DataAnnotations;
using IshikawaRca.Contracts.Rca;

namespace IshikawaRca.Web.Models.Rca;

public class ExternalIntakePortalViewModel
{
    public string Token { get; set; } = string.Empty;

    public RcaExternalIntakeDto Intake { get; set; } = new();

    [Display(Name = "Contacto")]
    [StringLength(160)]
    public string? ContactName { get; set; }

    [Display(Name = "Email")]
    [EmailAddress]
    [StringLength(254)]
    public string? ContactEmail { get; set; }

    [Display(Name = "Referencia del reclamo")]
    [StringLength(160)]
    public string? ClaimReference { get; set; }

    [Display(Name = "Material")]
    [StringLength(120)]
    public string? MaterialCode { get; set; }

    [Display(Name = "Lote")]
    [StringLength(120)]
    public string? BatchOrLot { get; set; }

    [Required(ErrorMessage = "La descripcion es obligatoria.")]
    [Display(Name = "Descripcion")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Contencion")]
    public string? ContainmentResponse { get; set; }

    [Display(Name = "Causa propuesta")]
    public string? ProposedRootCause { get; set; }

    [Display(Name = "Accion propuesta")]
    public string? ProposedCorrectiveAction { get; set; }

    [Display(Name = "Evidencia")]
    public string? EvidenceSummary { get; set; }
}
