namespace IshikawaRca.Contracts.Rca;

public class RcaAiSuggestionDto
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid RcaIncidentId { get; set; }

    public string SuggestionType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = "{}";

    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public bool IsFallback { get; set; }

    public int? Confidence { get; set; }

    public string GatewayCorrelationId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public string ReviewedByUserId { get; set; } = string.Empty;

    public string ReviewNotes { get; set; } = string.Empty;

    public string AppliedEntityType { get; set; } = string.Empty;

    public Guid? AppliedEntityId { get; set; }
}
