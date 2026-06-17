using IshikawaRca.Contracts.Common;
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

    Task<RcaAiSuggestionDto?> GetAsync(Guid tenantId, Guid incidentId, Guid suggestionId, CancellationToken cancellationToken = default);

    Task<ApiResult<RcaAiSuggestionDto>> ExecuteReviewTransactionAsync(
        Func<CancellationToken, Task<ApiResult<RcaAiSuggestionDto>>> operation,
        CancellationToken cancellationToken = default);

    Task<RcaAiSuggestionDto?> ClaimAcceptedAsync(
        Guid tenantId,
        Guid incidentId,
        Guid suggestionId,
        string reviewedByUserId,
        string reviewNotes,
        CancellationToken cancellationToken = default);

    Task<RcaAiSuggestionDto?> CompleteAcceptedAsync(
        Guid tenantId,
        Guid incidentId,
        Guid suggestionId,
        string appliedEntityType,
        Guid? appliedEntityId,
        CancellationToken cancellationToken = default);

    Task<RcaAiSuggestionDto?> MarkRejectedAsync(
        Guid tenantId,
        Guid incidentId,
        Guid suggestionId,
        string reviewedByUserId,
        string reviewNotes,
        CancellationToken cancellationToken = default);
}

public sealed record RcaAiPendingSuggestionInput(
    RcaAiSuggestionType Type,
    string Title,
    string Summary,
    object Payload,
    RcaAiSuggestionMetadataDto Metadata);
