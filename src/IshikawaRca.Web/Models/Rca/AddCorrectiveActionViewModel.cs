using System.ComponentModel.DataAnnotations;

namespace IshikawaRca.Web.Models.Rca;

public class AddCorrectiveActionViewModel
{
    public Guid? CauseId { get; set; }

    [Required(ErrorMessage = "El titulo de la accion es obligatorio.")]
    [StringLength(220)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "El tipo de accion es obligatorio.")]
    public string ActionType { get; set; } = "Corrective";

    [Required(ErrorMessage = "El ambito de resolucion es obligatorio.")]
    public string ResolutionScope { get; set; } = "RootCause";

    [StringLength(160)]
    public string? AssignedToUserId { get; set; }

    public DateTimeOffset? DueDate { get; set; }
}
