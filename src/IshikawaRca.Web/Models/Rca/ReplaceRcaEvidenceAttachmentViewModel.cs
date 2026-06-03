using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace IshikawaRca.Web.Models.Rca;

public class ReplaceRcaEvidenceAttachmentViewModel
{
    [Required]
    public Guid EvidenceId { get; set; }

    [Required(ErrorMessage = "Selecciona un archivo para reemplazar el adjunto.")]
    public IFormFile? Attachment { get; set; }
}
