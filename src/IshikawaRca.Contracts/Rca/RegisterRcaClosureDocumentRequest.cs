namespace IshikawaRca.Contracts.Rca;

public class RegisterRcaClosureDocumentRequest
{
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/pdf";

    public long SizeBytes { get; set; }

    public string StorageProvider { get; set; } = string.Empty;

    public string StorageKey { get; set; } = string.Empty;

    public string Sha256 { get; set; } = string.Empty;

    public string GeneratedByUserId { get; set; } = string.Empty;
}
