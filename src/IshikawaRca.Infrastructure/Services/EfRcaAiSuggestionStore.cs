using System.Text.Json;
using IshikawaRca.Application.Ai;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Entities;
using IshikawaRca.Domain.Enums;
using IshikawaRca.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IshikawaRca.Infrastructure.Services;

public class EfRcaAiSuggestionStore : IRcaAiSuggestionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RcaDbContext _dbContext;

    public EfRcaAiSuggestionStore(RcaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SavePendingAsync(
        Guid tenantId,
        Guid incidentId,
        RcaAiSuggestionType type,
        string title,
        string summary,
        object payload,
        RcaAiSuggestionMetadataDto metadata,
        string createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var suggestion = new RcaAiSuggestion
        {
            TenantId = tenantId,
            RcaIncidentId = incidentId,
            SuggestionType = type,
            Title = title,
            Summary = summary,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            Provider = metadata.Provider,
            Model = metadata.Model,
            IsFallback = metadata.IsFallback,
            Confidence = TryGetConfidence(payload),
            CreatedByUserId = createdByUserId
        };

        _dbContext.RcaAiSuggestions.Add(suggestion);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RcaAiSuggestionDto>> ListAsync(Guid incidentId, string? status, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RcaAiSuggestions
            .AsNoTracking()
            .Where(x => x.RcaIncidentId == incidentId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<RcaAiSuggestionStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    private static RcaAiSuggestionDto ToDto(RcaAiSuggestion suggestion)
    {
        return new RcaAiSuggestionDto
        {
            Id = suggestion.Id,
            TenantId = suggestion.TenantId,
            RcaIncidentId = suggestion.RcaIncidentId,
            SuggestionType = suggestion.SuggestionType.ToString(),
            Status = suggestion.Status.ToString(),
            Title = suggestion.Title,
            Summary = suggestion.Summary,
            PayloadJson = suggestion.PayloadJson,
            Provider = suggestion.Provider,
            Model = suggestion.Model,
            IsFallback = suggestion.IsFallback,
            Confidence = suggestion.Confidence,
            GatewayCorrelationId = suggestion.GatewayCorrelationId,
            CreatedAt = suggestion.CreatedAt,
            CreatedByUserId = suggestion.CreatedByUserId,
            ReviewedAt = suggestion.ReviewedAt,
            ReviewedByUserId = suggestion.ReviewedByUserId,
            ReviewNotes = suggestion.ReviewNotes,
            AppliedEntityType = suggestion.AppliedEntityType,
            AppliedEntityId = suggestion.AppliedEntityId
        };
    }

    private static int? TryGetConfidence(object payload)
    {
        return payload switch
        {
            RcaAiCauseSuggestionDto cause => NormalizeConfidence(cause.ConfidenceScore),
            RcaAiActionSuggestionDto => null,
            RcaAiRecurrenceResultDto recurrence => NormalizeConfidence(recurrence.ConfidenceScore),
            _ => null
        };
    }

    private static int? NormalizeConfidence(int confidence)
    {
        return confidence <= 0
            ? null
            : Math.Min(100, confidence);
    }
}
