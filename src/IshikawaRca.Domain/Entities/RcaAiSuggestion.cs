using IshikawaRca.Domain.Common;
using IshikawaRca.Domain.Enums;

namespace IshikawaRca.Domain.Entities;

public class RcaAiSuggestion : TenantEntity
{
    public Guid RcaIncidentId { get; set; }

    public RcaAiSuggestionType SuggestionType { get; set; }

    public RcaAiSuggestionStatus Status { get; set; } = RcaAiSuggestionStatus.Pending;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = "{}";

    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public bool IsFallback { get; set; }

    public int? Confidence { get; set; }

    public string GatewayCorrelationId { get; set; } = string.Empty;

    public DateTimeOffset? ReviewedAt { get; set; }

    public string ReviewedByUserId { get; set; } = string.Empty;

    public string ReviewNotes { get; set; } = string.Empty;

    public string AppliedEntityType { get; set; } = string.Empty;

    public Guid? AppliedEntityId { get; set; }
}
