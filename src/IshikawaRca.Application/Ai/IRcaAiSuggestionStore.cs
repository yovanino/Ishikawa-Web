using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Enums;

namespace IshikawaRca.Application.Ai;

public interface IRcaAiSuggestionStore
{
    Task SavePendingAsync(
        Guid tenantId,
        Guid incidentId,
        RcaAiSuggestionType type,
        string title,
        string summary,
        object payload,
        RcaAiSuggestionMetadataDto metadata,
        string createdByUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RcaAiSuggestionDto>> ListAsync(Guid incidentId, string? status, CancellationToken cancellationToken = default);
}
