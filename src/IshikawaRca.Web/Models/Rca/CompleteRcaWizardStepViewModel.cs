using System.ComponentModel.DataAnnotations;

namespace IshikawaRca.Web.Models.Rca;

public class CompleteRcaWizardStepViewModel
{
    [Required(ErrorMessage = "La etapa es obligatoria.")]
    public string Step { get; set; } = "Problem";

    [Display(Name = "Completado por")]
    public string? CompletedByUserId { get; set; }

    [Display(Name = "Nota")]
    public string? Notes { get; set; }
}
