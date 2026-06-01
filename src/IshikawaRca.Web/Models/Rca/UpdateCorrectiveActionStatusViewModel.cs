using System.ComponentModel.DataAnnotations;

namespace IshikawaRca.Web.Models.Rca;

public class UpdateCorrectiveActionStatusViewModel
{
    [Required]
    public Guid ActionId { get; set; }

    [Required(ErrorMessage = "El estado es obligatorio.")]
    public string Status { get; set; } = "Completed";

    [StringLength(160)]
    public string? CompletedByUserId { get; set; }

    [StringLength(4000)]
    public string? ValidationNotes { get; set; }
}
