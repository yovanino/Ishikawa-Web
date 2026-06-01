using System.ComponentModel.DataAnnotations;

namespace IshikawaRca.Web.Models.Rca;

public class CreateExternalIntakeViewModel
{
    [Required]
    [Display(Name = "Actor externo")]
    public string ActorType { get; set; } = "Supplier";

    [Display(Name = "Cliente / proveedor")]
    [StringLength(160)]
    public string? ActorName { get; set; }

    [Display(Name = "Contacto")]
    [StringLength(160)]
    public string? ContactName { get; set; }

    [Display(Name = "Email")]
    [EmailAddress]
    [StringLength(254)]
    public string? ContactEmail { get; set; }

    [Display(Name = "Expira")]
    public DateTimeOffset? ExpiresAt { get; set; }
}
