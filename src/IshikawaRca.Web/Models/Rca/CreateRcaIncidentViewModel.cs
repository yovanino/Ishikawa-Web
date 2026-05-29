using System.ComponentModel.DataAnnotations;

namespace IshikawaRca.Web.Models.Rca;

public class CreateRcaIncidentViewModel
{
    [Required(ErrorMessage = "El titulo del problema es obligatorio.")]
    [Display(Name = "Problema")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Descripcion")]
    public string? ProblemDescription { get; set; }

    [Required]
    [Display(Name = "Severidad")]
    public string Severity { get; set; } = "Medium";

    [Required]
    [Display(Name = "Tipo de reclamo")]
    public string ClaimScope { get; set; } = "Internal";

    [Display(Name = "Area / cliente")]
    [StringLength(160, ErrorMessage = "El area o cliente no puede superar 160 caracteres.")]
    public string? ClaimOwnerName { get; set; }

    [Display(Name = "Origen")]
    public string SourceSystem { get; set; } = "MANUAL";

    [Display(Name = "Fecha/hora del evento")]
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.Now;

    [Display(Name = "Maquina")]
    public string? MachineCode { get; set; }

    [Display(Name = "Linea")]
    public string? LineCode { get; set; }

    [Display(Name = "Orden de trabajo")]
    public string? WorkOrderCode { get; set; }

    [Display(Name = "Reportado por")]
    public string? ReportedBy { get; set; }
}
