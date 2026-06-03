namespace IshikawaRca.Contracts.Rca;

public class ReplaceRcaEvidenceAttachmentRequest
{
    public string AttachmentFileName { get; set; } = string.Empty;

    public string AttachmentContentType { get; set; } = "application/octet-stream";

    public long AttachmentSizeBytes { get; set; }

    public string AttachmentStorageProvider { get; set; } = "LocalFileSystem";

    public string AttachmentStorageKey { get; set; } = string.Empty;

    public string AttachmentSha256 { get; set; } = string.Empty;
}
