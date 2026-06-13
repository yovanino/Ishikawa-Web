using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
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
    private const int TitleMaxLength = 300;
    private const int SummaryMaxLength = 4000;
    private const int ProviderMaxLength = 100;
    private const int ModelMaxLength = 100;
    private const int CreatedByUserIdMaxLength = 160;
    private readonly RcaDbContext _dbContext;

    public EfRcaAiSuggestionStore(RcaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SavePendingBatchAsync(
        Guid tenantId,
        Guid incidentId,
        IReadOnlyList<RcaAiPendingSuggestionInput> suggestions,
        string createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (suggestions.Count == 0)
        {
            return;
        }

        var candidates = suggestions
            .Select(x => ToEntity(tenantId, incidentId, x, createdByUserId))
            .GroupBy(x => x.GatewayCorrelationId)
            .Select(x => x.First())
            .ToList();

        var correlationIds = candidates.Select(x => x.GatewayCorrelationId).ToList();
        var existingCorrelationIds = await _dbContext.RcaAiSuggestions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                x.RcaIncidentId == incidentId &&
                correlationIds.Contains(x.GatewayCorrelationId) &&
                !x.IsDeleted)
            .Select(x => x.GatewayCorrelationId)
            .ToListAsync(cancellationToken);

        var newSuggestions = candidates
            .Where(x => !existingCorrelationIds.Contains(x.GatewayCorrelationId))
            .ToList();

        if (newSuggestions.Count == 0)
        {
            return;
        }

        _dbContext.RcaAiSuggestions.AddRange(newSuggestions);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RcaAiSuggestionDto>> ListAsync(Guid tenantId, Guid incidentId, RcaAiSuggestionStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RcaAiSuggestions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.RcaIncidentId == incidentId && !x.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    private static RcaAiSuggestion ToEntity(Guid tenantId, Guid incidentId, RcaAiPendingSuggestionInput input, string createdByUserId)
    {
        var payloadJson = JsonSerializer.Serialize(input.Payload, JsonOptions);
        var correlationId = CreateCorrelationId(tenantId, incidentId, input.Type, payloadJson);

        return new RcaAiSuggestion
        {
            TenantId = tenantId,
            RcaIncidentId = incidentId,
            SuggestionType = input.Type,
            Title = Truncate(input.Title, TitleMaxLength),
            Summary = Truncate(input.Summary, SummaryMaxLength),
            PayloadJson = payloadJson,
            Provider = Truncate(input.Metadata.Provider, ProviderMaxLength),
            Model = Truncate(input.Metadata.Model, ModelMaxLength),
            IsFallback = input.Metadata.IsFallback,
            Confidence = TryGetConfidence(input.Payload),
            GatewayCorrelationId = correlationId,
            CreatedByUserId = Truncate(createdByUserId, CreatedByUserIdMaxLength)
        };
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

    private static string CreateCorrelationId(Guid tenantId, Guid incidentId, RcaAiSuggestionType type, string payloadJson)
    {
        var input = $"{tenantId:N}:{incidentId:N}:{type}:{payloadJson}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}
