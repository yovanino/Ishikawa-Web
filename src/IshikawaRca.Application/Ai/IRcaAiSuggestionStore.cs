using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Enums;

namespace IshikawaRca.Application.Ai;

public interface IRcaAiSuggestionStore
{
    Task SavePendingBatchAsync(
        Guid tenantId,
        Guid incidentId,
        IReadOnlyList<RcaAiPendingSuggestionInput> suggestions,
        string createdByUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RcaAiSuggestionDto>> ListAsync(Guid tenantId, Guid incidentId, RcaAiSuggestionStatus? status, CancellationToken cancellationToken = default);
}

public sealed record RcaAiPendingSuggestionInput(
    RcaAiSuggestionType Type,
    string Title,
    string Summary,
    object Payload,
    RcaAiSuggestionMetadataDto Metadata);
