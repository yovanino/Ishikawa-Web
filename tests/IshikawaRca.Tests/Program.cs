using IshikawaRca.Application.Ai;
using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Entities;
using IshikawaRca.Domain.Enums;
using IshikawaRca.Domain.Services;
using IshikawaRca.Infrastructure.Ai;
using IshikawaRca.Infrastructure;
using IshikawaRca.Infrastructure.Data;
using IshikawaRca.Infrastructure.Services;
using IshikawaRca.Web.Controllers.Api;
using IshikawaRca.Web.Security;
using IshikawaRca.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Cryptography;
using System.Text;

var rootOnlyActions = new[]
{
    NewAction(CorrectiveActionType.Corrective, RcaResolutionScope.RootCause),
    NewAction(CorrectiveActionType.Preventive, RcaResolutionScope.RootCause)
};

AssertContains(
    RcaResolutionPolicy.GetResolutionBlockers(rootOnlyActions, hasEscapeAnalysis: false),
    "Falta una accion preventiva de recurrencia para la causa raiz.");

var documentedEscapeWithoutActions = new[]
{
    NewAction(CorrectiveActionType.Corrective, RcaResolutionScope.RootCause),
    NewAction(CorrectiveActionType.Preventive, RcaResolutionScope.RootCause),
    NewAction(CorrectiveActionType.RecurrencePreventive, RcaResolutionScope.RootCause)
};

AssertContains(
    RcaResolutionPolicy.GetResolutionBlockers(documentedEscapeWithoutActions, hasEscapeAnalysis: true),
    "La FUGA requiere accion correctiva, preventiva y preventiva de recurrencia.");

var completeResolution = new[]
{
    NewAction(CorrectiveActionType.Corrective, RcaResolutionScope.RootCause),
    NewAction(CorrectiveActionType.Preventive, RcaResolutionScope.RootCause),
    NewAction(CorrectiveActionType.RecurrencePreventive, RcaResolutionScope.RootCause),
    NewAction(CorrectiveActionType.Corrective, RcaResolutionScope.Escape),
    NewAction(CorrectiveActionType.Preventive, RcaResolutionScope.Escape),
    NewAction(CorrectiveActionType.RecurrencePreventive, RcaResolutionScope.Escape)
};

AssertEmpty(RcaResolutionPolicy.GetResolutionBlockers(completeResolution, hasEscapeAnalysis: true));

AssertAiSuggestionDefaults();
AssertRcaClosureDocumentDefaults();
AssertRcaClosureDocumentEfModel();
AssertRcaClosureDocumentContracts();
await AssertExternalFactIdempotencyAsync();
await AssertIncompleteExternalFactCorrelationFailsAsync();
await AssertIntegrationEventCompatibilityAsync();
AssertOutboxEventDomainDefaults();
AssertRcaIntegrationOptionsDefaults();
await AssertOutboxPublisherSkipsWhenNoWebhooksAreEnabledAsync();
await AssertOutboxPublisherMarksEventPublishedWhenWebhookSucceedsAsync();
await AssertOutboxPublisherMarksEventFailedWhenWebhookFailsAsync();
await AssertOutboxPublisherMarksEventDeadLetterWhenMaxAttemptsIsReachedAsync();
await AssertOutboxPublishEndpointInvokesPublisherAsync();
await AssertIntegrationEventsLiveEndpointWritesServerSentEventsAsync();
await AssertHttpAiGatewayClientPostsCauseContextAsync();
await AssertHttpAiGatewayClientPreservesBaseUrlPrefixAsync();
await AssertHttpAiGatewayClientRejectsInvalidBaseUrlAsync();
await AssertHttpAiGatewayClientReturnsUnavailableForNonSuccessStatusAsync();
await AssertHttpAiGatewayClientReturnsInvalidResponseForInvalidJsonAsync();
await AssertHttpAiGatewayClientReturnsUnavailableWhenResponseReadTimesOutAsync();
await AssertHttpAiGatewayClientPostsRecurrenceContextAsync();
await AssertStubAiGatewayReturnsRecurrenceAndEightDWithoutMutatingContextAsync();
await AssertStubAiGatewayMetadataIsDeterministicAsync();
await AssertConfiguredAiGatewayFallsBackWhenHttpFailsAsync();
await AssertConfiguredAiGatewayUsesStubModeEvenWhenHttpWouldFailAsync();
await AssertConfiguredAiGatewayReturnsHttpFailureWhenFallbackIsDisabledAsync();
await AssertConfiguredAiGatewayFallsBackWhenHttpThrowsAsync();
await AssertConfiguredAiGatewayReturnsUnavailableWhenHttpThrowsAndFallbackIsDisabledAsync();
AssertInfrastructureResolvesConfiguredAiGatewayClient();
await AssertAiControllerExposesRecurrenceAndEightDAsync();
await AssertAiControllerMapsGatewayFailuresToServiceUnavailableAsync();
await AssertAiControllerMapsMissingRcaToNotFoundAsync();
await AssertAiControllerExposesSuggestionReviewEndpointsAsync();
await AssertAiAssistantPersistsPendingCauseSuggestionsAsync();
await AssertAiAssistantRejectsInvalidSuggestionStatusAsync();
await AssertAiAssistantRejectsUndefinedNumericSuggestionStatusAsync();
await AssertAcceptingCauseSuggestionCreatesCauseAndMarksAcceptedAsync();
await AssertRejectingSuggestionDoesNotCreateOfficialEntityAsync();
await AssertAcceptingSummarySuggestionOnlyMarksAcceptedAsync();
await AssertAcceptingCauseSuggestionRequiresBranchWithContractErrorAsync();
await AssertAcceptingAlreadyReviewedSuggestionDoesNotCreateCauseAsync();
await AssertHttpWebhookSenderPostsPayloadAsync();
await AssertHttpWebhookSenderSignsPayloadWhenSecretExistsAsync();
await AssertHttpWebhookSenderFailsWhenConfiguredTimeoutExpiresAsync();
await AssertInMemoryAuditRecordsAsync();
await AssertEvidenceStorageRejectsOversizedFilesAsync();
AssertEvidenceStorageRejectsUnsafeKeys();

static CorrectiveAction NewAction(CorrectiveActionType type, RcaResolutionScope scope)
{
    return new CorrectiveAction
    {
        ActionType = type,
        ResolutionScope = scope
    };
}

static void AssertAiSuggestionDefaults()
{
    var suggestion = new RcaAiSuggestion
    {
        TenantId = Guid.NewGuid(),
        RcaIncidentId = Guid.NewGuid(),
        SuggestionType = RcaAiSuggestionType.Cause,
        Title = "AI cause",
        PayloadJson = "{}"
    };

    if (suggestion.Status != RcaAiSuggestionStatus.Pending ||
        suggestion.Id == Guid.Empty ||
        suggestion.IsFallback)
    {
        throw new InvalidOperationException("Expected AI suggestions to default to pending, non-fallback review records.");
    }
}

static void AssertRcaClosureDocumentDefaults()
{
    var incidentId = Guid.NewGuid();
    var document = new RcaClosureDocument
    {
        TenantId = Guid.NewGuid(),
        RcaIncidentId = incidentId,
        Version = 1,
        FileName = "rca-closure-v1.pdf",
        ContentType = "application/pdf",
        SizeBytes = 128,
        StorageProvider = "Local",
        StorageKey = "closure/incident/v1.pdf",
        Sha256 = new string('a', 64),
        GeneratedByUserId = "quality"
    };

    if (document.Id == Guid.Empty ||
        document.RcaIncidentId != incidentId ||
        document.Status != RcaClosureDocumentStatus.Draft ||
        document.Version != 1)
    {
        throw new InvalidOperationException("Closure document defaults are invalid.");
    }
}

static void AssertRcaClosureDocumentEfModel()
{
    var options = new DbContextOptionsBuilder<RcaDbContext>()
        .UseMySql("Server=localhost;Database=ishikawa_test;User=root;Password=test;", new MySqlServerVersion(new Version(8, 0, 36)))
        .Options;
    using var dbContext = new RcaDbContext(options);
    var entityType = dbContext.Model.FindEntityType(typeof(RcaClosureDocument))
        ?? throw new InvalidOperationException("RcaClosureDocument must be mapped in EF model.");

    if (entityType.GetTableName() != "rca_closure_documents")
    {
        throw new InvalidOperationException("RcaClosureDocument table name is invalid.");
    }

    var indexes = entityType.GetIndexes().ToList();
    AssertHasIndex(indexes, ["TenantId", "RcaIncidentId", "Version"], unique: true);
    AssertHasIndex(indexes, ["TenantId", "RcaIncidentId", "GeneratedAt"], unique: false);
    AssertHasIndex(indexes, ["TenantId", "Status", "GeneratedAt"], unique: false);
}

static void AssertHasIndex(IReadOnlyList<Microsoft.EntityFrameworkCore.Metadata.IIndex> indexes, string[] propertyNames, bool unique)
{
    var hasIndex = indexes.Any(index =>
        index.IsUnique == unique &&
        index.Properties.Select(property => property.Name).SequenceEqual(propertyNames));

    if (!hasIndex)
    {
        throw new InvalidOperationException($"Expected EF index on {string.Join(", ", propertyNames)} with unique={unique}.");
    }
}

static void AssertRcaClosureDocumentContracts()
{
    var dto = new RcaClosureDocumentDto
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        RcaIncidentId = Guid.NewGuid(),
        Version = 1,
        FileName = "rca-closure-v1.pdf",
        SizeBytes = 128,
        StorageProvider = "Local",
        StorageKey = "closure/rca/v1.pdf",
        Sha256 = new string('b', 64),
        Status = nameof(RcaClosureDocumentStatus.Draft),
        GeneratedAt = DateTimeOffset.UtcNow,
        GeneratedByUserId = "quality"
    };

    var register = new RegisterRcaClosureDocumentRequest
    {
        FileName = dto.FileName,
        SizeBytes = dto.SizeBytes,
        StorageProvider = dto.StorageProvider,
        StorageKey = dto.StorageKey,
        Sha256 = dto.Sha256,
        GeneratedByUserId = dto.GeneratedByUserId
    };

    var review = new ReviewRcaClosureDocumentRequest
    {
        ReviewedByUserId = "quality",
        ReviewNotes = "Approved for pilot release."
    };

    if (dto.ContentType != "application/pdf" ||
        register.ContentType != "application/pdf" ||
        review.ReviewedByUserId != "quality")
    {
        throw new InvalidOperationException("Closure document contracts must keep PDF defaults and reviewer metadata.");
    }
}

static async Task AssertExternalFactIdempotencyAsync()
{
    var service = new InMemoryRcaIncidentService();
    var created = await service.CreateAsync(new CreateRcaIncidentRequest
    {
        TenantId = Guid.NewGuid(),
        Title = "External fact idempotency",
        ProblemDescription = "Test RCA",
        SourceSystem = "TEST",
        ReportedBy = "tests"
    });

    if (created.Data is null)
    {
        throw new InvalidOperationException("Expected test incident to be created.");
    }

    var request = new AddRcaFactRequest
    {
        Title = "SCADA alarm",
        FactType = "Alarm",
        Source = "SCADA",
        ExternalSourceSystem = "SCADA",
        ExternalEventId = "ALM-001",
        AlarmCode = "PRES-HIGH"
    };

    var first = await service.AddFactAsync(created.Data.Id, request);
    var second = await service.AddFactAsync(created.Data.Id, request);

    if (first.Data is null || second.Data is null || first.Data.Id != second.Data.Id)
    {
        throw new InvalidOperationException("Expected repeated external fact to return the existing record.");
    }
}

static async Task AssertIncompleteExternalFactCorrelationFailsAsync()
{
    var service = new InMemoryRcaIncidentService();
    var created = await service.CreateAsync(new CreateRcaIncidentRequest
    {
        TenantId = Guid.NewGuid(),
        Title = "External fact validation",
        ProblemDescription = "Test RCA",
        SourceSystem = "TEST",
        ReportedBy = "tests"
    });

    if (created.Data is null)
    {
        throw new InvalidOperationException("Expected test incident to be created.");
    }

    var result = await service.AddFactAsync(created.Data.Id, new AddRcaFactRequest
    {
        Title = "SCADA alarm",
        Source = "SCADA",
        ExternalSourceSystem = "SCADA"
    });

    AssertContains(
        result.Errors.Select(x => x.Code).ToList(),
        "EXTERNAL_FACT_CORRELATION_INCOMPLETE");
}

static async Task AssertAiControllerExposesRecurrenceAndEightDAsync()
{
    var service = new RecordingAiAssistantService();
    var controller = new RcaAiController(service, new FixedCurrentRcaUserContext());
    var id = Guid.NewGuid();

    var recurrence = await controller.DetectRecurrence(id, CancellationToken.None);
    var eightD = await controller.GenerateEightDDraft(id, CancellationToken.None);

    if (recurrence.Result is not OkObjectResult ||
        eightD.Result is not OkObjectResult ||
        !service.DetectRecurrenceCalled ||
        !service.GenerateEightDCalled)
    {
        throw new InvalidOperationException("Expected AI controller to expose recurrence and 8D draft endpoints.");
    }
}

static async Task AssertAiControllerMapsGatewayFailuresToServiceUnavailableAsync()
{
    var controller = new RcaAiController(new RecordingAiAssistantService(
        recurrenceResult: ApiResult<RcaAiRecurrenceResultDto>.Fail(
            "Gateway down",
            new ApiError { Code = "AI_GATEWAY_UNAVAILABLE", Message = "Gateway down" })),
        new FixedCurrentRcaUserContext());

    var result = await controller.DetectRecurrence(Guid.NewGuid(), CancellationToken.None);

    if (result.Result is not ObjectResult objectResult ||
        objectResult.StatusCode != StatusCodes.Status503ServiceUnavailable)
    {
        throw new InvalidOperationException("Expected AI controller to map gateway failures to 503 Service Unavailable.");
    }
}

static async Task AssertAiControllerMapsMissingRcaToNotFoundAsync()
{
    var controller = new RcaAiController(new RecordingAiAssistantService(
        recurrenceResult: ApiResult<RcaAiRecurrenceResultDto>.Fail(
            "RCA missing",
            new ApiError { Code = "RCA_NOT_FOUND", Message = "RCA missing" })),
        new FixedCurrentRcaUserContext());

    var result = await controller.DetectRecurrence(Guid.NewGuid(), CancellationToken.None);

    if (result.Result is not NotFoundObjectResult)
    {
        throw new InvalidOperationException("Expected AI controller to keep missing RCA results as 404 Not Found.");
    }
}

static async Task AssertAiControllerExposesSuggestionReviewEndpointsAsync()
{
    var service = new RecordingAiAssistantService();
    var controller = new RcaAiController(service, new FixedCurrentRcaUserContext("authenticated-quality"));
    var incidentId = Guid.NewGuid();
    var suggestionId = Guid.NewGuid();

    var list = await controller.ListSuggestions(incidentId, "Pending", CancellationToken.None);
    var accept = await controller.AcceptSuggestion(incidentId, suggestionId, new AcceptRcaAiSuggestionRequest
    {
        ReviewedByUserId = "quality"
    }, CancellationToken.None);
    var reject = await controller.RejectSuggestion(incidentId, suggestionId, new RejectRcaAiSuggestionRequest
    {
        ReviewedByUserId = "quality"
    }, CancellationToken.None);

    if (list.Result is not OkObjectResult ||
        accept.Result is not OkObjectResult ||
        reject.Result is not OkObjectResult ||
        !service.ListSuggestionsCalled ||
        !service.AcceptSuggestionCalled ||
        !service.RejectSuggestionCalled ||
        service.LastAcceptedReviewedByUserId != "authenticated-quality" ||
        service.LastRejectedReviewedByUserId != "authenticated-quality")
    {
        throw new InvalidOperationException("Expected AI controller to expose suggestion list, accept and reject endpoints.");
    }
}

static async Task AssertAiAssistantPersistsPendingCauseSuggestionsAsync()
{
    var incidentId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var store = new RecordingAiSuggestionStore();
    var service = new RcaAiAssistantService(
        new FixedRcaIncidentService(tenantId, incidentId),
        new FixedAiGatewayClient(),
        store);

    var result = await service.SuggestCausesAsync(incidentId, CancellationToken.None);

    if (!result.Success || result.Data?.Suggestions.Count != 2)
    {
        throw new InvalidOperationException("Expected AI assistant to return gateway cause suggestions.");
    }

    if (store.Saved.Count != 2 ||
        store.SaveBatchCallCount != 1 ||
        store.Saved.Any(x => x.TenantId != tenantId || x.IncidentId != incidentId) ||
        store.Saved.Any(x => x.Type != RcaAiSuggestionType.Cause) ||
        store.Saved.Any(x => x.CreatedByUserId != "ai-request") ||
        store.Saved.Any(x => !x.Metadata.IsFallback))
    {
        throw new InvalidOperationException("Expected AI assistant to persist one pending cause suggestion per gateway suggestion.");
    }
}

static async Task AssertAiAssistantRejectsInvalidSuggestionStatusAsync()
{
    var incidentId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var service = new RcaAiAssistantService(
        new FixedRcaIncidentService(tenantId, incidentId),
        new FixedAiGatewayClient(),
        new RecordingAiSuggestionStore());

    var result = await service.ListSuggestionsAsync(incidentId, "PendienteMalEscrito", CancellationToken.None);

    if (result.Success || result.Errors.All(x => x.Code != "AI_SUGGESTION_STATUS_INVALID"))
    {
        throw new InvalidOperationException("Expected invalid AI suggestion status filters to fail with a controlled error.");
    }
}

static async Task AssertAiAssistantRejectsUndefinedNumericSuggestionStatusAsync()
{
    var incidentId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var service = new RcaAiAssistantService(
        new FixedRcaIncidentService(tenantId, incidentId),
        new FixedAiGatewayClient(),
        new RecordingAiSuggestionStore());

    var result = await service.ListSuggestionsAsync(incidentId, "999", CancellationToken.None);

    if (result.Success || result.Errors.All(x => x.Code != "AI_SUGGESTION_STATUS_INVALID"))
    {
        throw new InvalidOperationException("Expected undefined numeric AI suggestion statuses to fail with a controlled error.");
    }
}

static async Task AssertAcceptingCauseSuggestionCreatesCauseAndMarksAcceptedAsync()
{
    var incidentId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var branchId = Guid.NewGuid();
    var suggestionId = Guid.NewGuid();
    var store = new RecordingAiSuggestionStore();
    store.Suggestions.Add(new RcaAiSuggestionDto
    {
        Id = suggestionId,
        TenantId = tenantId,
        RcaIncidentId = incidentId,
        SuggestionType = nameof(RcaAiSuggestionType.Cause),
        Status = nameof(RcaAiSuggestionStatus.Pending),
        Title = "Accepted cause",
        PayloadJson = "{\"branchName\":\"Metodo\",\"title\":\"Accepted cause\",\"reasoning\":\"AI reasoning\",\"confidenceScore\":91,\"suggestedImpactScore\":4,\"suggestedProbabilityScore\":3,\"suggestedFrequencyScore\":2}"
    });
    var incidentService = new FixedRcaIncidentService(tenantId, incidentId, branchId);
    var service = new RcaAiAssistantService(incidentService, new FixedAiGatewayClient(), store);

    var result = await service.AcceptSuggestionAsync(incidentId, suggestionId, new AcceptRcaAiSuggestionRequest
    {
        TargetBranchId = branchId,
        ReviewedByUserId = "quality",
        ReviewNotes = "Approved"
    });

    if (!result.Success ||
        result.Data?.Status != nameof(RcaAiSuggestionStatus.Accepted) ||
        store.ReviewTransactionCallCount != 1 ||
        incidentService.AddedCauses.Count != 1 ||
        incidentService.AddedCauses.Single().Title != "Accepted cause" ||
        store.AuditRecords.All(x => x.Action != "AiSuggestionAccepted"))
    {
        throw new InvalidOperationException("Expected accepting a cause suggestion to create an official cause and mark the suggestion accepted with audit.");
    }
}

static async Task AssertAcceptingSummarySuggestionOnlyMarksAcceptedAsync()
{
    var incidentId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var suggestionId = Guid.NewGuid();
    var store = new RecordingAiSuggestionStore();
    store.Suggestions.Add(new RcaAiSuggestionDto
    {
        Id = suggestionId,
        TenantId = tenantId,
        RcaIncidentId = incidentId,
        SuggestionType = nameof(RcaAiSuggestionType.Summary),
        Status = nameof(RcaAiSuggestionStatus.Pending),
        Title = "Summary",
        PayloadJson = "{}"
    });
    var incidentService = new FixedRcaIncidentService(tenantId, incidentId);
    var service = new RcaAiAssistantService(incidentService, new FixedAiGatewayClient(), store);

    var result = await service.AcceptSuggestionAsync(incidentId, suggestionId, new AcceptRcaAiSuggestionRequest
    {
        ReviewedByUserId = "quality"
    });

    if (!result.Success ||
        result.Data?.Status != nameof(RcaAiSuggestionStatus.Accepted) ||
        incidentService.AddedCauses.Count != 0 ||
        incidentService.AddedActions.Count != 0 ||
        result.Data.AppliedEntityId.HasValue)
    {
        throw new InvalidOperationException("Expected accepting a summary AI suggestion to mark it accepted without official RCA mutations.");
    }
}

static async Task AssertAcceptingCauseSuggestionRequiresBranchWithContractErrorAsync()
{
    var incidentId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var suggestionId = Guid.NewGuid();
    var store = new RecordingAiSuggestionStore();
    store.Suggestions.Add(new RcaAiSuggestionDto
    {
        Id = suggestionId,
        TenantId = tenantId,
        RcaIncidentId = incidentId,
        SuggestionType = nameof(RcaAiSuggestionType.Cause),
        Status = nameof(RcaAiSuggestionStatus.Pending),
        Title = "Cause",
        PayloadJson = "{\"title\":\"Cause\"}"
    });
    var service = new RcaAiAssistantService(new FixedRcaIncidentService(tenantId, incidentId), new FixedAiGatewayClient(), store);

    var result = await service.AcceptSuggestionAsync(incidentId, suggestionId, new AcceptRcaAiSuggestionRequest
    {
        ReviewedByUserId = "quality"
    });

    if (result.Success || result.Errors.All(x => x.Code != "AI_SUGGESTION_BRANCH_REQUIRED"))
    {
        throw new InvalidOperationException("Expected accepting a cause suggestion without branch to fail with AI_SUGGESTION_BRANCH_REQUIRED.");
    }
}

static async Task AssertAcceptingAlreadyReviewedSuggestionDoesNotCreateCauseAsync()
{
    var incidentId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var branchId = Guid.NewGuid();
    var suggestionId = Guid.NewGuid();
    var store = new RecordingAiSuggestionStore();
    store.Suggestions.Add(new RcaAiSuggestionDto
    {
        Id = suggestionId,
        TenantId = tenantId,
        RcaIncidentId = incidentId,
        SuggestionType = nameof(RcaAiSuggestionType.Cause),
        Status = nameof(RcaAiSuggestionStatus.Accepted),
        Title = "Already accepted",
        PayloadJson = "{\"title\":\"Already accepted\"}"
    });
    var incidentService = new FixedRcaIncidentService(tenantId, incidentId, branchId);
    var service = new RcaAiAssistantService(incidentService, new FixedAiGatewayClient(), store);

    var result = await service.AcceptSuggestionAsync(incidentId, suggestionId, new AcceptRcaAiSuggestionRequest
    {
        TargetBranchId = branchId,
        ReviewedByUserId = "quality"
    });

    if (result.Success || incidentService.AddedCauses.Count != 0)
    {
        throw new InvalidOperationException("Expected already reviewed AI suggestions to avoid official RCA mutations.");
    }
}

static async Task AssertRejectingSuggestionDoesNotCreateOfficialEntityAsync()
{
    var incidentId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var suggestionId = Guid.NewGuid();
    var store = new RecordingAiSuggestionStore();
    store.Suggestions.Add(new RcaAiSuggestionDto
    {
        Id = suggestionId,
        TenantId = tenantId,
        RcaIncidentId = incidentId,
        SuggestionType = nameof(RcaAiSuggestionType.Cause),
        Status = nameof(RcaAiSuggestionStatus.Pending),
        Title = "Rejected cause",
        PayloadJson = "{\"title\":\"Rejected cause\"}"
    });
    var incidentService = new FixedRcaIncidentService(tenantId, incidentId);
    var service = new RcaAiAssistantService(incidentService, new FixedAiGatewayClient(), store);

    var result = await service.RejectSuggestionAsync(incidentId, suggestionId, new RejectRcaAiSuggestionRequest
    {
        ReviewedByUserId = "quality",
        ReviewNotes = "Not enough evidence"
    });

    if (!result.Success ||
        result.Data?.Status != nameof(RcaAiSuggestionStatus.Rejected) ||
        incidentService.AddedCauses.Count != 0 ||
        incidentService.AddedActions.Count != 0 ||
        store.AuditRecords.All(x => x.Action != "AiSuggestionRejected"))
    {
        throw new InvalidOperationException("Expected rejecting an AI suggestion to avoid official RCA mutations and write audit.");
    }
}

static async Task AssertIntegrationEventCompatibilityAsync()
{
    var tenantId = Guid.NewGuid();
    var service = new InMemoryRcaIncidentService();
    var created = await service.CreateAsync(new CreateRcaIncidentRequest
    {
        TenantId = tenantId,
        Title = "Integration event contract",
        ProblemDescription = "Test RCA",
        SourceSystem = "GANTT",
        ExternalTaskId = "TASK-001",
        ExternalEventId = "EVT-001",
        ExternalWorkOrderId = "WO-001",
        ReportedBy = "tests"
    });

    if (created.Data is null)
    {
        throw new InvalidOperationException("Expected test incident to be created.");
    }

    var canvas = await service.GetCanvasAsync(created.Data.Id);
    var branchId = canvas.Data?.Branches.FirstOrDefault()?.Id
        ?? throw new InvalidOperationException("Expected default Ishikawa branches.");

    var cause = await service.AddCauseAsync(created.Data.Id, new AddIshikawaCauseRequest
    {
        BranchId = branchId,
        Title = "Root cause",
        IsRootCause = true,
        ProbabilityScore = 3,
        ImpactScore = 4,
        FrequencyScore = 2
    });

    if (cause.Data is null)
    {
        throw new InvalidOperationException("Expected test cause to be created.");
    }

    var action = await service.AddCorrectiveActionAsync(created.Data.Id, new AddCorrectiveActionRequest
    {
        CauseId = cause.Data.Id,
        Title = "Corrective event action",
        ActionType = "Corrective",
        ResolutionScope = "RootCause",
        DueDate = DateTimeOffset.UtcNow.AddDays(1)
    });

    if (action.Data is null)
    {
        throw new InvalidOperationException("Expected test action to be created.");
    }

    await service.UpdateCorrectiveActionStatusAsync(created.Data.Id, action.Data.Id, new UpdateCorrectiveActionStatusRequest
    {
        Status = "Completed",
        CompletedByUserId = "quality",
        ValidationNotes = "Event contract validation."
    });

    var evidence = await service.AddEvidenceAsync(created.Data.Id, new AddRcaEvidenceRequest
    {
        CauseId = cause.Data.Id,
        Title = "Event evidence",
        EvidenceType = "Photo",
        Source = "Manual",
        Tags = "event-contract",
        ValidationStatus = "Validated",
        ValidatedByUserId = "quality",
        AttachmentFileName = "evidence.jpg",
        AttachmentContentType = "image/jpeg",
        AttachmentSizeBytes = 128,
        AttachmentStorageProvider = "Local",
        AttachmentSha256 = new string('a', 64)
    });

    if (evidence.Data is null)
    {
        throw new InvalidOperationException("Expected test evidence to be created.");
    }

    var fact = await service.AddFactAsync(created.Data.Id, new AddRcaFactRequest
    {
        CauseId = cause.Data.Id,
        EvidenceId = evidence.Data.Id,
        CorrectiveActionId = action.Data.Id,
        Title = "SCADA pressure alarm",
        FactType = "Alarm",
        Source = "SCADA",
        ExternalSourceSystem = "SCADA",
        ExternalEventId = "ALM-002",
        ExternalRecordUri = "scada://line-1/alarm/ALM-002",
        AlarmCode = "PRES-HIGH"
    });

    if (fact.Data is null)
    {
        throw new InvalidOperationException("Expected test fact to be created.");
    }

    var result = await service.ListIntegrationEventsAsync(created.Data.Id);
    var events = result.Data ?? throw new InvalidOperationException("Expected integration events.");

    AssertEventTypes(events, [
        "RcaIncidentCreated",
        "RcaRootCauseSelected",
        "RcaCorrectiveActionCreated",
        "RcaCorrectiveActionCompleted",
        "RcaEvidenceAttached",
        "RcaFactRecorded"
    ]);

    foreach (var integrationEvent in events)
    {
        if (string.IsNullOrWhiteSpace(integrationEvent.Id) ||
            string.IsNullOrWhiteSpace(integrationEvent.Type) ||
            integrationEvent.IncidentId != created.Data.Id ||
            integrationEvent.TenantId != tenantId ||
            integrationEvent.SourceSystem != "GANTT" ||
            integrationEvent.ExternalTaskId != "TASK-001" ||
            integrationEvent.ExternalEventId != "EVT-001" ||
            integrationEvent.ExternalWorkOrderId != "WO-001")
        {
            throw new InvalidOperationException("Expected integration event envelope to preserve correlation fields.");
        }
    }

    var createdEvent = RequireEvent(events, "RcaIncidentCreated");
    AssertDataValue(createdEvent, "title", "Integration event contract");
    AssertDataValue(createdEvent, "status", "Open");

    var causeEvent = RequireEvent(events, "RcaRootCauseSelected");
    AssertDataValue(causeEvent, "causeId", cause.Data.Id.ToString());
    AssertDataValue(causeEvent, "isRootCause", "True");

    var completedActionEvent = RequireEvent(events, "RcaCorrectiveActionCompleted");
    AssertDataValue(completedActionEvent, "completedByUserId", "quality");
    AssertDataValue(completedActionEvent, "validationNotes", "Event contract validation.");

    var evidenceEvent = RequireEvent(events, "RcaEvidenceAttached");
    AssertDataValue(evidenceEvent, "attachmentFileName", "evidence.jpg");
    AssertDataValue(evidenceEvent, "attachmentSha256", new string('a', 64));

    var factEvent = RequireEvent(events, "RcaFactRecorded");
    AssertDataValue(factEvent, "externalSourceSystem", "SCADA");
    AssertDataValue(factEvent, "externalEventId", "ALM-002");
    AssertDataValue(factEvent, "alarmCode", "PRES-HIGH");

    var afterCreated = await service.ListIntegrationEventsAsync(created.Data.Id, createdEvent.OccurredAt.AddTicks(1));
    if (afterCreated.Data is null || afterCreated.Data.Any(x => x.Id == createdEvent.Id))
    {
        throw new InvalidOperationException("Expected since filter to exclude older processed events.");
    }
}

static void AssertOutboxEventDomainDefaults()
{
    var tenantId = Guid.NewGuid();
    var incidentId = Guid.NewGuid();
    var payload = "{\"type\":\"RcaIncidentCreated\"}";

    var outboxEvent = new RcaOutboxEvent
    {
        TenantId = tenantId,
        EventId = "rca-incident-created:" + incidentId,
        EventType = "RcaIncidentCreated",
        OccurredAt = DateTimeOffset.UtcNow,
        IncidentId = incidentId,
        PayloadJson = payload
    };

    if (outboxEvent.Status != RcaOutboxEventStatus.Pending ||
        outboxEvent.AttemptCount != 0 ||
        outboxEvent.NextAttemptAt is not null ||
        outboxEvent.PayloadJson != payload)
    {
        throw new InvalidOperationException("Expected outbox event defaults to preserve pending delivery state.");
    }
}

static void AssertRcaIntegrationOptionsDefaults()
{
    var options = new RcaIntegrationOptions();

    if (options.PublishBatchSize != 50 ||
        options.MaxPublishAttempts != 5 ||
        options.PublishTimeoutSeconds != 5 ||
        options.Webhooks.Count != 0)
    {
        throw new InvalidOperationException("Expected RCA integration options to default to safe disabled webhooks.");
    }

    var webhook = new RcaWebhookOptions
    {
        Name = "test",
        Url = "https://example.local/rca/events"
    };

    if (webhook.Enabled || webhook.Secret.Length != 0 || webhook.EventTypes.Count != 0)
    {
        throw new InvalidOperationException("Expected webhook options to be disabled and secret-free by default.");
    }
}

static async Task AssertOutboxPublisherSkipsWhenNoWebhooksAreEnabledAsync()
{
    var outboxService = new ThrowingOutboxService();
    var publisher = new RcaOutboxPublisher(
        outboxService,
        new RecordingWebhookSender(),
        Options.Create(new RcaIntegrationOptions
        {
            Webhooks = []
        }));

    var result = await publisher.PublishPendingAsync();

    if (!result.Success ||
        result.Data is null ||
        result.Data.EnabledWebhookCount != 0 ||
        result.Data.AttemptedEventCount != 0 ||
        outboxService.ListPendingCalls != 0)
    {
        throw new InvalidOperationException("Expected publisher to skip outbox reads when no webhooks are enabled.");
    }
}

static async Task AssertOutboxPublisherMarksEventPublishedWhenWebhookSucceedsAsync()
{
    var pendingEvent = new RcaOutboxEvent
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        EventId = "event-001",
        EventType = "RcaClosed",
        IncidentId = Guid.NewGuid(),
        OccurredAt = DateTimeOffset.UtcNow,
        PayloadJson = "{\"type\":\"RcaClosed\"}",
        Status = RcaOutboxEventStatus.Pending
    };
    var outboxService = new RecordingOutboxService([pendingEvent]);
    var webhookSender = new RecordingWebhookSender();
    var publisher = new RcaOutboxPublisher(
        outboxService,
        webhookSender,
        Options.Create(new RcaIntegrationOptions
        {
            Webhooks =
            [
                new RcaWebhookOptions
                {
                    Name = "test-webhook",
                    Url = "https://example.local/rca/events",
                    Enabled = true,
                    EventTypes = ["RcaClosed"]
                }
            ]
        }));

    var result = await publisher.PublishPendingAsync();

    if (!result.Success ||
        result.Data is null ||
        result.Data.AttemptedEventCount != 1 ||
        result.Data.PublishedEventCount != 1 ||
        webhookSender.SentEventIds.SingleOrDefault() != pendingEvent.Id ||
        outboxService.PublishedEventIds.SingleOrDefault() != pendingEvent.Id)
    {
        throw new InvalidOperationException("Expected successful webhook delivery to mark the outbox event as published.");
    }
}

static async Task AssertOutboxPublisherMarksEventFailedWhenWebhookFailsAsync()
{
    var pendingEvent = new RcaOutboxEvent
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        EventId = "event-failed",
        EventType = "RcaClosed",
        IncidentId = Guid.NewGuid(),
        OccurredAt = DateTimeOffset.UtcNow,
        PayloadJson = "{\"type\":\"RcaClosed\"}",
        Status = RcaOutboxEventStatus.Pending
    };
    var outboxService = new RecordingOutboxService([pendingEvent]);
    var publisher = new RcaOutboxPublisher(
        outboxService,
        new FailingWebhookSender("remote unavailable"),
        Options.Create(new RcaIntegrationOptions
        {
            Webhooks =
            [
                new RcaWebhookOptions
                {
                    Name = "failing-webhook",
                    Url = "https://example.local/rca/events",
                    Enabled = true
                }
            ]
        }));

    var result = await publisher.PublishPendingAsync();

    if (!result.Success ||
        result.Data is null ||
        result.Data.FailedEventCount != 1 ||
        outboxService.FailedEventIds.SingleOrDefault() != pendingEvent.Id ||
        string.IsNullOrWhiteSpace(outboxService.LastFailureError) ||
        outboxService.LastFailureNextAttemptAt is null)
    {
        throw new InvalidOperationException("Expected failed webhook delivery to mark the outbox event as failed.");
    }
}

static async Task AssertOutboxPublisherMarksEventDeadLetterWhenMaxAttemptsIsReachedAsync()
{
    var pendingEvent = new RcaOutboxEvent
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        EventId = "event-dead-letter",
        EventType = "RcaClosed",
        IncidentId = Guid.NewGuid(),
        OccurredAt = DateTimeOffset.UtcNow,
        PayloadJson = "{\"type\":\"RcaClosed\"}",
        Status = RcaOutboxEventStatus.Failed,
        AttemptCount = 4
    };
    var outboxService = new RecordingOutboxService([pendingEvent]);
    var publisher = new RcaOutboxPublisher(
        outboxService,
        new FailingWebhookSender("remote unavailable"),
        Options.Create(new RcaIntegrationOptions
        {
            MaxPublishAttempts = 5,
            Webhooks =
            [
                new RcaWebhookOptions
                {
                    Name = "failing-webhook",
                    Url = "https://example.local/rca/events",
                    Enabled = true
                }
            ]
        }));

    var result = await publisher.PublishPendingAsync();

    if (!result.Success ||
        result.Data is null ||
        result.Data.DeadLetterEventCount != 1 ||
        outboxService.DeadLetterEventIds.SingleOrDefault() != pendingEvent.Id ||
        outboxService.FailedEventIds.Count != 0)
    {
        throw new InvalidOperationException("Expected event to move to dead-letter when max publish attempts is reached.");
    }
}

static async Task AssertOutboxPublishEndpointInvokesPublisherAsync()
{
    var publisher = new RecordingOutboxPublisher();
    var controller = new RcaIntegrationsController(null!, null!, publisher);

    var response = await controller.PublishOutbox(CancellationToken.None);
    var ok = response.Result as OkObjectResult
        ?? throw new InvalidOperationException("Expected outbox publish endpoint to return 200 OK.");
    var result = ok.Value as ApiResult<RcaOutboxPublishResultDto>
        ?? throw new InvalidOperationException("Expected outbox publish endpoint to return ApiResult<RcaOutboxPublishResultDto>.");

    if (!publisher.WasCalled || !result.Success || result.Data?.PublishedEventCount != 2)
    {
        throw new InvalidOperationException("Expected outbox publish endpoint to invoke the publisher.");
    }
}

static async Task AssertIntegrationEventsLiveEndpointWritesServerSentEventsAsync()
{
    var service = new InMemoryRcaIncidentService();
    var created = await service.CreateAsync(new CreateRcaIncidentRequest
    {
        TenantId = Guid.NewGuid(),
        Title = "SSE integration event",
        ProblemDescription = "Test RCA",
        SourceSystem = "TEST",
        ReportedBy = "tests"
    });

    if (created.Data is null)
    {
        throw new InvalidOperationException("Expected test incident to be created.");
    }

    await using var body = new MemoryStream();
    var controller = new RcaIntegrationsController(service, null!, null!)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Response =
                {
                    Body = body
                }
            }
        }
    };

    var response = await controller.StreamEvents(
        created.Data.Id,
        null,
        pollIntervalSeconds: 1,
        maxBatches: 1,
        CancellationToken.None);

    body.Position = 0;
    using var reader = new StreamReader(body, Encoding.UTF8);
    var streamText = await reader.ReadToEndAsync();

    if (response is not EmptyResult ||
        controller.Response.ContentType != "text/event-stream" ||
        !streamText.Contains("event: RcaIncidentCreated", StringComparison.Ordinal) ||
        !streamText.Contains("\"type\":\"RcaIncidentCreated\"", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("Expected live integration events endpoint to write RCA events as server-sent events.");
    }
}

static async Task AssertHttpAiGatewayClientPostsCauseContextAsync()
{
    var handler = new RecordingHttpMessageHandler(HttpStatusCode.OK)
    {
        ResponseContent = """
        {
          "incidentId":"11111111-1111-1111-1111-111111111111",
          "summary":"Gateway causes",
          "suggestions":[
            {
              "branchName":"Metodo",
              "title":"Verificar estandar",
              "reasoning":"Patron historico similar.",
              "confidenceScore":82,
              "suggestedImpactScore":4,
              "suggestedProbabilityScore":3,
              "suggestedFrequencyScore":2
            }
          ],
          "metadata":{
            "provider":"Gateway",
            "model":"rca-v1",
            "isFallback":false,
            "generatedAt":"2026-06-12T00:00:00Z"
          }
        }
        """
    };
    var client = new HttpRcaAiGatewayClient(
        new HttpClient(handler),
        Options.Create(new RcaAiGatewayOptions
        {
            BaseUrl = "https://ai.example.local",
            TimeoutSeconds = 5,
            ApiKey = "secret-token"
        }));

    var result = await client.SuggestCausesAsync(new RcaAiContextDto
    {
        Incident = new RcaIncidentDto
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Title = "AI RCA",
            ProblemDescription = "Problem"
        }
    });

    var request = handler.Requests.Single();
    var hasAuth = request.Headers.Authorization?.Scheme == "Bearer" &&
        request.Headers.Authorization.Parameter == "secret-token";

    if (!result.Success ||
        result.Data?.Suggestions.Single().Title != "Verificar estandar" ||
        request.Method != HttpMethod.Post ||
        request.RequestUri?.ToString() != "https://ai.example.local/ai/rca/suggest-causes" ||
        !hasAuth ||
        !handler.Bodies.Single().Contains("\"title\":\"AI RCA\"", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("Expected HTTP AI Gateway client to POST RCA context and map cause suggestions.");
    }
}

static async Task AssertHttpAiGatewayClientPreservesBaseUrlPrefixAsync()
{
    var handler = new RecordingHttpMessageHandler(HttpStatusCode.OK)
    {
        ResponseContent = """
        {
          "incidentId":"11111111-1111-1111-1111-111111111111",
          "summary":"Gateway causes",
          "suggestions":[],
          "metadata":{
            "provider":"Gateway",
            "model":"rca-v1",
            "isFallback":false,
            "generatedAt":"2026-06-12T00:00:00Z"
          }
        }
        """
    };
    var client = new HttpRcaAiGatewayClient(
        new HttpClient(handler),
        Options.Create(new RcaAiGatewayOptions
        {
            BaseUrl = "https://ai.example.local/prefix",
            TimeoutSeconds = 5
        }));

    var result = await client.SuggestCausesAsync(new RcaAiContextDto
    {
        Incident = new RcaIncidentDto
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Title = "AI RCA"
        }
    });

    if (!result.Success ||
        handler.Requests.Single().RequestUri?.ToString() != "https://ai.example.local/prefix/ai/rca/suggest-causes")
    {
        throw new InvalidOperationException("Expected HTTP AI Gateway client to preserve BaseUrl path prefixes.");
    }
}

static async Task AssertHttpAiGatewayClientRejectsInvalidBaseUrlAsync()
{
    var client = new HttpRcaAiGatewayClient(
        new HttpClient(new RecordingHttpMessageHandler(HttpStatusCode.OK)),
        Options.Create(new RcaAiGatewayOptions
        {
            BaseUrl = "not-a-url"
        }));

    var result = await client.SuggestCausesAsync(new RcaAiContextDto
    {
        Incident = new RcaIncidentDto
        {
            Id = Guid.NewGuid(),
            Title = "AI RCA"
        }
    });

    AssertContains(result.Errors.Select(x => x.Code).ToList(), "AI_GATEWAY_CONFIGURATION_INVALID");
}

static async Task AssertHttpAiGatewayClientReturnsUnavailableForNonSuccessStatusAsync()
{
    var client = new HttpRcaAiGatewayClient(
        new HttpClient(new RecordingHttpMessageHandler(HttpStatusCode.BadGateway)),
        Options.Create(new RcaAiGatewayOptions
        {
            BaseUrl = "https://ai.example.local",
            TimeoutSeconds = 5
        }));

    var result = await client.SuggestCausesAsync(new RcaAiContextDto
    {
        Incident = new RcaIncidentDto
        {
            Id = Guid.NewGuid(),
            Title = "AI RCA"
        }
    });

    AssertContains(result.Errors.Select(x => x.Code).ToList(), "AI_GATEWAY_UNAVAILABLE");
}

static async Task AssertHttpAiGatewayClientReturnsInvalidResponseForInvalidJsonAsync()
{
    var handler = new RecordingHttpMessageHandler(HttpStatusCode.OK)
    {
        ResponseContent = "{ invalid json"
    };
    var client = new HttpRcaAiGatewayClient(
        new HttpClient(handler),
        Options.Create(new RcaAiGatewayOptions
        {
            BaseUrl = "https://ai.example.local",
            TimeoutSeconds = 5
        }));

    var result = await client.SuggestCausesAsync(new RcaAiContextDto
    {
        Incident = new RcaIncidentDto
        {
            Id = Guid.NewGuid(),
            Title = "AI RCA"
        }
    });

    AssertContains(result.Errors.Select(x => x.Code).ToList(), "AI_GATEWAY_INVALID_RESPONSE");
}

static async Task AssertHttpAiGatewayClientReturnsUnavailableWhenResponseReadTimesOutAsync()
{
    var handler = new RecordingHttpMessageHandler(HttpStatusCode.OK)
    {
        ResponseBodyFactory = _ => new DelayedReadJsonContent(
            """
            {
              "incidentId":"11111111-1111-1111-1111-111111111111",
              "summary":"Gateway causes",
              "suggestions":[],
              "metadata":{
                "provider":"Gateway",
                "model":"rca-v1",
                "isFallback":false,
                "generatedAt":"2026-06-12T00:00:00Z"
              }
            }
            """,
            TimeSpan.FromMilliseconds(1500))
    };
    var client = new HttpRcaAiGatewayClient(
        new HttpClient(handler),
        Options.Create(new RcaAiGatewayOptions
        {
            BaseUrl = "https://ai.example.local",
            TimeoutSeconds = 1
        }));

    var result = await client.SuggestCausesAsync(new RcaAiContextDto
    {
        Incident = new RcaIncidentDto
        {
            Id = Guid.NewGuid(),
            Title = "AI RCA"
        }
    });

    AssertContains(result.Errors.Select(x => x.Code).ToList(), "AI_GATEWAY_UNAVAILABLE");
}

static async Task AssertHttpAiGatewayClientPostsRecurrenceContextAsync()
{
    var incidentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    var handler = new RecordingHttpMessageHandler(HttpStatusCode.OK)
    {
        ResponseContent = """
        {
          "incidentId":"11111111-1111-1111-1111-111111111111",
          "isLikelyRecurring":true,
          "confidenceScore":88,
          "rationale":"Repeated machine pattern.",
          "similarSignals":["Same line"],
          "metadata":{
            "provider":"Gateway",
            "model":"rca-v1",
            "isFallback":false,
            "generatedAt":"2026-06-12T00:00:00Z"
          }
        }
        """
    };
    var client = new HttpRcaAiGatewayClient(
        new HttpClient(handler),
        Options.Create(new RcaAiGatewayOptions
        {
            BaseUrl = "https://ai.example.local",
            TimeoutSeconds = 5
        }));

    var result = await client.DetectRecurrenceAsync(new RcaAiContextDto
    {
        Incident = new RcaIncidentDto
        {
            Id = incidentId,
            Title = "AI RCA recurrence"
        }
    });

    if (!result.Success ||
        result.Data?.IncidentId != incidentId ||
        handler.Requests.Single().RequestUri?.ToString() != "https://ai.example.local/ai/rca/detect-recurrence")
    {
        throw new InvalidOperationException("Expected HTTP AI Gateway client to POST recurrence context to the recurrence endpoint.");
    }
}

static async Task AssertStubAiGatewayReturnsRecurrenceAndEightDWithoutMutatingContextAsync()
{
    var incidentId = Guid.NewGuid();
    var context = new RcaAiContextDto
    {
        Incident = new RcaIncidentDto
        {
            Id = incidentId,
            Title = "Recurring defect",
            ProblemDescription = "Repeated weld deviation"
        },
        Canvas = new IshikawaCanvasDto
        {
            Causes =
            [
                new IshikawaCauseDto { Id = Guid.NewGuid(), Title = "Cause 1" },
                new IshikawaCauseDto { Id = Guid.NewGuid(), Title = "Cause 2" },
                new IshikawaCauseDto { Id = Guid.NewGuid(), Title = "Cause 3" }
            ]
        }
    };

    var originalCauseCount = context.Canvas.Causes.Count;
    var stub = new StubRcaAiGatewayClient();

    var recurrence = await stub.DetectRecurrenceAsync(context);
    var eightD = await stub.GenerateEightDDraftAsync(context);

    if (!recurrence.Success ||
        recurrence.Data?.IncidentId != incidentId ||
        recurrence.Data.Metadata.IsFallback != true ||
        recurrence.Data.IsLikelyRecurring != true ||
        recurrence.Data.SimilarSignals.Count != 2)
    {
        throw new InvalidOperationException("Expected stub recurrence suggestion with fallback metadata.");
    }

    if (!eightD.Success ||
        eightD.Data?.IncidentId != incidentId ||
        eightD.Data.Metadata.IsFallback != true ||
        eightD.Data.ProblemStatement != "Repeated weld deviation")
    {
        throw new InvalidOperationException("Expected stub 8D draft with fallback metadata.");
    }

    if (context.Canvas.Causes.Count != originalCauseCount)
    {
        throw new InvalidOperationException("Expected stub AI calls to avoid mutating the RCA context.");
    }
}

static async Task AssertStubAiGatewayMetadataIsDeterministicAsync()
{
    var context = new RcaAiContextDto
    {
        Incident = new RcaIncidentDto
        {
            Id = Guid.NewGuid(),
            Title = "Deterministic stub",
            ProblemDescription = "Same input must keep fallback metadata stable"
        },
        Canvas = new IshikawaCanvasDto()
    };

    var stub = new StubRcaAiGatewayClient();

    var first = await stub.DetectRecurrenceAsync(context);
    var second = await stub.DetectRecurrenceAsync(context);
    var firstDraft = await stub.GenerateEightDDraftAsync(context);
    var secondDraft = await stub.GenerateEightDDraftAsync(context);

    if (!first.Success || !second.Success || !firstDraft.Success || !secondDraft.Success)
    {
        throw new InvalidOperationException("Expected stub AI calls to succeed for deterministic metadata validation.");
    }

    if (first.Data?.Metadata.GeneratedAt != second.Data?.Metadata.GeneratedAt ||
        firstDraft.Data?.Metadata.GeneratedAt != secondDraft.Data?.Metadata.GeneratedAt)
    {
        throw new InvalidOperationException("Expected stub AI fallback metadata to be deterministic for identical calls.");
    }
}

static async Task AssertConfiguredAiGatewayFallsBackWhenHttpFailsAsync()
{
    var fallback = new StubRcaAiGatewayClient();
    var failing = new FailingAiGatewayClient();
    var client = new ConfiguredRcaAiGatewayClient(
        failing,
        fallback,
        Options.Create(new RcaAiGatewayOptions
        {
            Mode = "Http",
            UseFallbackOnFailure = true
        }));

    var result = await client.SuggestCausesAsync(new RcaAiContextDto
    {
        Incident = new RcaIncidentDto { Id = Guid.NewGuid(), Title = "Fallback", Severity = "High" },
        Canvas = new IshikawaCanvasDto()
    });

    if (!result.Success || result.Data?.Metadata.IsFallback != true)
    {
        throw new InvalidOperationException("Expected configured AI client to fall back to stub when HTTP mode fails.");
    }
}

static async Task AssertConfiguredAiGatewayUsesStubModeEvenWhenHttpWouldFailAsync()
{
    var fallback = new StubRcaAiGatewayClient();
    var failing = new FailingAiGatewayClient();
    var client = new ConfiguredRcaAiGatewayClient(
        failing,
        fallback,
        Options.Create(new RcaAiGatewayOptions
        {
            Mode = "Stub",
            UseFallbackOnFailure = false
        }));

    var result = await client.SuggestActionsAsync(new RcaAiContextDto
    {
        Incident = new RcaIncidentDto { Id = Guid.NewGuid(), Title = "Stub mode", Severity = "Medium" },
        Canvas = new IshikawaCanvasDto()
    });

    if (!result.Success || result.Data?.Metadata.IsFallback != true)
    {
        throw new InvalidOperationException("Expected configured AI client to use stub mode directly.");
    }
}

static async Task AssertConfiguredAiGatewayReturnsHttpFailureWhenFallbackIsDisabledAsync()
{
    var fallback = new StubRcaAiGatewayClient();
    var failing = new FailingAiGatewayClient();
    var client = new ConfiguredRcaAiGatewayClient(
        failing,
        fallback,
        Options.Create(new RcaAiGatewayOptions
        {
            Mode = "Http",
            UseFallbackOnFailure = false
        }));

    var result = await client.SummarizeAsync(new RcaAiContextDto
    {
        Incident = new RcaIncidentDto { Id = Guid.NewGuid(), Title = "No fallback", Severity = "High" },
        Canvas = new IshikawaCanvasDto()
    });

    if (result.Success || result.Errors.All(x => x.Code != "AI_GATEWAY_UNAVAILABLE"))
    {
        throw new InvalidOperationException("Expected configured AI client to return the HTTP failure when fallback is disabled.");
    }
}

static async Task AssertConfiguredAiGatewayFallsBackWhenHttpThrowsAsync()
{
    var fallback = new StubRcaAiGatewayClient();
    var throwing = new ThrowingAiGatewayClient();
    var client = new ConfiguredRcaAiGatewayClient(
        throwing,
        fallback,
        Options.Create(new RcaAiGatewayOptions
        {
            Mode = "Http",
            UseFallbackOnFailure = true
        }));

    var result = await client.SuggestCausesAsync(new RcaAiContextDto
    {
        Incident = new RcaIncidentDto { Id = Guid.NewGuid(), Title = "Thrown fallback", Severity = "High" },
        Canvas = new IshikawaCanvasDto()
    });

    if (!result.Success || result.Data?.Metadata.IsFallback != true)
    {
        throw new InvalidOperationException("Expected configured AI client to fall back to stub when HTTP mode throws.");
    }
}

static async Task AssertConfiguredAiGatewayReturnsUnavailableWhenHttpThrowsAndFallbackIsDisabledAsync()
{
    var fallback = new StubRcaAiGatewayClient();
    var throwing = new ThrowingAiGatewayClient();
    var client = new ConfiguredRcaAiGatewayClient(
        throwing,
        fallback,
        Options.Create(new RcaAiGatewayOptions
        {
            Mode = "Http",
            UseFallbackOnFailure = false
        }));

    var result = await client.SuggestCausesAsync(new RcaAiContextDto
    {
        Incident = new RcaIncidentDto { Id = Guid.NewGuid(), Title = "Thrown no fallback", Severity = "High" },
        Canvas = new IshikawaCanvasDto()
    });

    if (result.Success || result.Errors.All(x => x.Code != "AI_GATEWAY_UNAVAILABLE"))
    {
        throw new InvalidOperationException("Expected configured AI client to convert thrown HTTP failures into AI_GATEWAY_UNAVAILABLE when fallback is disabled.");
    }
}

static void AssertInfrastructureResolvesConfiguredAiGatewayClient()
{
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:IshikawaRca"] = "Server=localhost;Database=ishikawa_test;Uid=test;Pwd=test;",
            ["AiGateway:Mode"] = "Http",
            ["AiGateway:BaseUrl"] = "https://ai.example.local",
            ["AiGateway:UseFallbackOnFailure"] = "true"
        })
        .Build();

    var services = new ServiceCollection();
    services.AddIshikawaRcaInfrastructure(configuration);

    using var provider = services.BuildServiceProvider();
    var client = provider.GetRequiredService<IRcaAiGatewayClient>();
    var closureDocumentService = provider.GetRequiredService<IRcaClosureDocumentService>();

    if (client is not ConfiguredRcaAiGatewayClient)
    {
        throw new InvalidOperationException("Expected infrastructure DI to resolve IRcaAiGatewayClient as ConfiguredRcaAiGatewayClient.");
    }

    if (closureDocumentService is not EfRcaClosureDocumentService)
    {
        throw new InvalidOperationException("Expected infrastructure DI to resolve IRcaClosureDocumentService as EfRcaClosureDocumentService.");
    }
}

static async Task AssertHttpWebhookSenderPostsPayloadAsync()
{
    var payload = "{\"id\":\"event-001\",\"type\":\"RcaClosed\"}";
    var handler = new RecordingHttpMessageHandler(HttpStatusCode.Accepted);
    var sender = new RcaHttpWebhookSender(new HttpClient(handler));
    var outboxEvent = new RcaOutboxEvent
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        EventId = "event-001",
        EventType = "RcaClosed",
        IncidentId = Guid.NewGuid(),
        OccurredAt = DateTimeOffset.UtcNow,
        PayloadJson = payload
    };

    var result = await sender.SendAsync(
        new RcaWebhookOptions
        {
            Name = "test-http",
            Url = "https://example.local/rca/events"
        },
        outboxEvent);

    var hasEventId = handler.Requests[0].Headers.TryGetValues("X-RCA-Event-Id", out var eventIds);
    var hasEventType = handler.Requests[0].Headers.TryGetValues("X-RCA-Event-Type", out var eventTypes);
    var actualEventId = hasEventId ? eventIds?.SingleOrDefault() : null;
    var actualEventType = hasEventType ? eventTypes?.SingleOrDefault() : null;

    if (!result.Success ||
        handler.Requests.Count != 1 ||
        handler.Requests[0].Method != HttpMethod.Post ||
        handler.Requests[0].RequestUri?.ToString() != "https://example.local/rca/events" ||
        handler.Bodies.SingleOrDefault() != payload ||
        !hasEventId ||
        actualEventId != "event-001" ||
        !hasEventType ||
        actualEventType != "RcaClosed")
    {
        throw new InvalidOperationException("Expected HTTP webhook sender to POST the outbox payload with event headers.");
    }
}

static async Task AssertHttpWebhookSenderSignsPayloadWhenSecretExistsAsync()
{
    var payload = "{\"id\":\"event-002\",\"type\":\"RcaClosed\"}";
    var secret = "local-secret";
    var handler = new RecordingHttpMessageHandler(HttpStatusCode.OK);
    var sender = new RcaHttpWebhookSender(new HttpClient(handler));
    var outboxEvent = new RcaOutboxEvent
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        EventId = "event-002",
        EventType = "RcaClosed",
        IncidentId = Guid.NewGuid(),
        OccurredAt = DateTimeOffset.UtcNow,
        PayloadJson = payload
    };

    var result = await sender.SendAsync(
        new RcaWebhookOptions
        {
            Name = "signed-http",
            Url = "https://example.local/rca/events",
            Secret = secret
        },
        outboxEvent);

    var expectedSignature = Convert.ToHexString(
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload)))
        .ToLowerInvariant();
    var hasSignature = handler.Requests[0].Headers.TryGetValues("X-RCA-Signature", out var signatures);
    var actualSignature = hasSignature ? signatures?.SingleOrDefault() : null;

    if (!result.Success || actualSignature != "sha256=" + expectedSignature)
    {
        throw new InvalidOperationException("Expected HTTP webhook sender to sign payloads when a secret exists.");
    }
}

static async Task AssertHttpWebhookSenderFailsWhenConfiguredTimeoutExpiresAsync()
{
    var handler = new SlowHttpMessageHandler(TimeSpan.FromMilliseconds(1500));
    var sender = new RcaHttpWebhookSender(
        new HttpClient(handler),
        Options.Create(new RcaIntegrationOptions { PublishTimeoutSeconds = 1 }));
    var outboxEvent = new RcaOutboxEvent
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        EventId = "event-timeout",
        EventType = "RcaClosed",
        IncidentId = Guid.NewGuid(),
        OccurredAt = DateTimeOffset.UtcNow,
        PayloadJson = "{\"type\":\"RcaClosed\"}"
    };

    var result = await sender.SendAsync(
        new RcaWebhookOptions
        {
            Name = "slow-http",
            Url = "https://example.local/rca/events"
        },
        outboxEvent);

    if (result.Success || !handler.WasCanceled)
    {
        throw new InvalidOperationException("Expected HTTP webhook sender to fail and cancel slow requests when configured timeout expires.");
    }
}

static async Task AssertInMemoryAuditRecordsAsync()
{
    var service = new InMemoryRcaIncidentService();
    var created = await service.CreateAsync(new CreateRcaIncidentRequest
    {
        TenantId = Guid.NewGuid(),
        Title = "Audit records",
        ProblemDescription = "Test RCA",
        SourceSystem = "TEST",
        ReportedBy = "tests"
    });

    if (created.Data is null)
    {
        throw new InvalidOperationException("Expected test incident to be created.");
    }

    var action = await service.AddCorrectiveActionAsync(created.Data.Id, new AddCorrectiveActionRequest
    {
        Title = "Audit action",
        ActionType = "Corrective",
        ResolutionScope = "RootCause"
    });

    if (action.Data is null)
    {
        throw new InvalidOperationException("Expected test action to be created.");
    }

    await service.UpdateCorrectiveActionStatusAsync(created.Data.Id, action.Data.Id, new UpdateCorrectiveActionStatusRequest
    {
        Status = "Completed",
        CompletedByUserId = "quality",
        ValidationNotes = "Audit record validation."
    });

    var audit = await service.ListAuditRecordsAsync(created.Data.Id);
    var record = audit.Data?.FirstOrDefault(x => x.Action == "CorrectiveActionStatusChanged");

    if (record is null)
    {
        throw new InvalidOperationException("Expected corrective action status change audit record.");
    }

    if (record.EntityType != nameof(CorrectiveAction) || record.UserId != "quality")
    {
        throw new InvalidOperationException("Expected audit record to preserve entity type and user.");
    }
}

static async Task AssertEvidenceStorageRejectsOversizedFilesAsync()
{
    var root = CreateTempDirectory();
    try
    {
        var storage = new EvidenceFileStorage(
            new TestWebHostEnvironment(root),
            Options.Create(new EvidenceStorageOptions { RootPath = "evidence", MaxFileSizeMb = 1 }));
        await using var stream = new MemoryStream(new byte[(1024 * 1024) + 1]);
        var file = new FormFile(stream, 0, stream.Length, "Attachment", "oversized.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        await AssertThrowsAsync<InvalidOperationException>(() => storage.SaveAsync(Guid.NewGuid(), file, CancellationToken.None));
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

static void AssertEvidenceStorageRejectsUnsafeKeys()
{
    var root = CreateTempDirectory();
    try
    {
        var storage = new EvidenceFileStorage(
            new TestWebHostEnvironment(root),
            Options.Create(new EvidenceStorageOptions { RootPath = "evidence" }));

        AssertThrows<InvalidOperationException>(() => storage.Resolve("../outside.pdf", "outside.pdf", "application/pdf"));
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

static void AssertContains(IReadOnlyList<string> values, string expected)
{
    if (!values.Contains(expected, StringComparer.Ordinal))
    {
        throw new InvalidOperationException($"Expected blocker '{expected}'. Actual: {string.Join(" | ", values)}");
    }
}

static void AssertEmpty(IReadOnlyList<string> values)
{
    if (values.Count != 0)
    {
        throw new InvalidOperationException($"Expected no blockers. Actual: {string.Join(" | ", values)}");
    }
}

static void AssertEventTypes(IReadOnlyList<RcaDomainEventDto> events, IReadOnlyList<string> expectedTypes)
{
    foreach (var expectedType in expectedTypes)
    {
        if (!events.Any(x => x.Type == expectedType))
        {
            throw new InvalidOperationException($"Expected integration event type '{expectedType}'. Actual: {string.Join(" | ", events.Select(x => x.Type))}");
        }
    }
}

static RcaDomainEventDto RequireEvent(IReadOnlyList<RcaDomainEventDto> events, string type)
{
    return events.FirstOrDefault(x => x.Type == type)
        ?? throw new InvalidOperationException($"Expected integration event type '{type}'.");
}

static void AssertDataValue(RcaDomainEventDto integrationEvent, string key, string expected)
{
    if (!integrationEvent.Data.TryGetValue(key, out var actual) || actual != expected)
    {
        throw new InvalidOperationException($"Expected event '{integrationEvent.Type}' data '{key}' to be '{expected}'. Actual: '{actual}'.");
    }
}

static async Task AssertThrowsAsync<TException>(Func<Task> action)
    where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected exception {typeof(TException).Name}.");
}

static void AssertThrows<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected exception {typeof(TException).Name}.");
}

static string CreateTempDirectory()
{
    var path = Path.Combine(Path.GetTempPath(), "ishikawa-rca-tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(path);

    return path;
}

internal sealed class TestWebHostEnvironment : IWebHostEnvironment
{
    public TestWebHostEnvironment(string contentRootPath)
    {
        ContentRootPath = contentRootPath;
        WebRootPath = contentRootPath;
        ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
        WebRootFileProvider = ContentRootFileProvider;
    }

    public string ApplicationName { get; set; } = "IshikawaRca.Tests";

    public IFileProvider ContentRootFileProvider { get; set; }

    public string ContentRootPath { get; set; }

    public string EnvironmentName { get; set; } = "Development";

    public string WebRootPath { get; set; }

    public IFileProvider WebRootFileProvider { get; set; }
}

internal sealed class ThrowingOutboxService : IRcaOutboxService
{
    public int ListPendingCalls { get; private set; }

    public Task<RcaOutboxEvent> EnqueueAsync(RcaDomainEventDto integrationEvent, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<IReadOnlyList<RcaOutboxEvent>> ListPendingAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        ListPendingCalls++;
        throw new InvalidOperationException("ListPendingAsync should not be called when no webhooks are enabled.");
    }

    public Task<RcaOutboxStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<IReadOnlyList<RcaOutboxEventDto>> ListDeadLettersAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ApiResult<RcaOutboxEventDto>> ScheduleRetryAsync(Guid id, RetryRcaOutboxEventRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task MarkPublishedAsync(Guid id, DateTimeOffset publishedAt, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task MarkFailedAsync(Guid id, string error, DateTimeOffset nextAttemptAt, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task MarkDeadLetterAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}

internal sealed class RecordingOutboxService : IRcaOutboxService
{
    private readonly IReadOnlyList<RcaOutboxEvent> _pendingEvents;

    public RecordingOutboxService(IReadOnlyList<RcaOutboxEvent> pendingEvents)
    {
        _pendingEvents = pendingEvents;
    }

    public List<Guid> PublishedEventIds { get; } = [];
    public List<Guid> FailedEventIds { get; } = [];
    public List<Guid> DeadLetterEventIds { get; } = [];
    public string? LastFailureError { get; private set; }
    public DateTimeOffset? LastFailureNextAttemptAt { get; private set; }

    public Task<RcaOutboxEvent> EnqueueAsync(RcaDomainEventDto integrationEvent, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<IReadOnlyList<RcaOutboxEvent>> ListPendingAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_pendingEvents);
    }

    public Task<RcaOutboxStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<IReadOnlyList<RcaOutboxEventDto>> ListDeadLettersAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ApiResult<RcaOutboxEventDto>> ScheduleRetryAsync(Guid id, RetryRcaOutboxEventRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task MarkPublishedAsync(Guid id, DateTimeOffset publishedAt, CancellationToken cancellationToken = default)
    {
        PublishedEventIds.Add(id);
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(Guid id, string error, DateTimeOffset nextAttemptAt, CancellationToken cancellationToken = default)
    {
        FailedEventIds.Add(id);
        LastFailureError = error;
        LastFailureNextAttemptAt = nextAttemptAt;
        return Task.CompletedTask;
    }

    public Task MarkDeadLetterAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        DeadLetterEventIds.Add(id);
        LastFailureError = error;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingWebhookSender : IRcaWebhookSender
{
    public List<Guid> SentEventIds { get; } = [];

    public Task<RcaWebhookSendResult> SendAsync(RcaWebhookOptions webhook, RcaOutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        SentEventIds.Add(outboxEvent.Id);
        return Task.FromResult(RcaWebhookSendResult.Succeeded());
    }
}

internal sealed class RecordingOutboxPublisher : IRcaOutboxPublisher
{
    public bool WasCalled { get; private set; }

    public Task<ApiResult<RcaOutboxPublishResultDto>> PublishPendingAsync(CancellationToken cancellationToken = default)
    {
        WasCalled = true;
        return Task.FromResult(ApiResult<RcaOutboxPublishResultDto>.Ok(new RcaOutboxPublishResultDto
        {
            EnabledWebhookCount = 1,
            AttemptedEventCount = 2,
            PublishedEventCount = 2
        }));
    }
}

internal sealed class FailingWebhookSender : IRcaWebhookSender
{
    private readonly string _error;

    public FailingWebhookSender(string error)
    {
        _error = error;
    }

    public Task<RcaWebhookSendResult> SendAsync(RcaWebhookOptions webhook, RcaOutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(RcaWebhookSendResult.Failed(_error));
    }
}

internal sealed class FailingAiGatewayClient : IRcaAiGatewayClient
{
    public Task<ApiResult<RcaAiCauseSuggestionResultDto>> SuggestCausesAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResult<RcaAiCauseSuggestionResultDto>.Fail("Gateway down", new ApiError { Code = "AI_GATEWAY_UNAVAILABLE", Message = "Gateway down" }));
    }

    public Task<ApiResult<RcaAiActionSuggestionResultDto>> SuggestActionsAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResult<RcaAiActionSuggestionResultDto>.Fail("Gateway down", new ApiError { Code = "AI_GATEWAY_UNAVAILABLE", Message = "Gateway down" }));
    }

    public Task<ApiResult<RcaAiSummaryResultDto>> SummarizeAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResult<RcaAiSummaryResultDto>.Fail("Gateway down", new ApiError { Code = "AI_GATEWAY_UNAVAILABLE", Message = "Gateway down" }));
    }

    public Task<ApiResult<RcaAiRecurrenceResultDto>> DetectRecurrenceAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResult<RcaAiRecurrenceResultDto>.Fail("Gateway down", new ApiError { Code = "AI_GATEWAY_UNAVAILABLE", Message = "Gateway down" }));
    }

    public Task<ApiResult<RcaAiEightDDraftResultDto>> GenerateEightDDraftAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResult<RcaAiEightDDraftResultDto>.Fail("Gateway down", new ApiError { Code = "AI_GATEWAY_UNAVAILABLE", Message = "Gateway down" }));
    }
}

internal sealed class ThrowingAiGatewayClient : IRcaAiGatewayClient
{
    public Task<ApiResult<RcaAiCauseSuggestionResultDto>> SuggestCausesAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        throw new HttpRequestException("Gateway down");
    }

    public Task<ApiResult<RcaAiActionSuggestionResultDto>> SuggestActionsAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        throw new HttpRequestException("Gateway down");
    }

    public Task<ApiResult<RcaAiSummaryResultDto>> SummarizeAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        throw new HttpRequestException("Gateway down");
    }

    public Task<ApiResult<RcaAiRecurrenceResultDto>> DetectRecurrenceAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        throw new HttpRequestException("Gateway down");
    }

    public Task<ApiResult<RcaAiEightDDraftResultDto>> GenerateEightDDraftAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        throw new HttpRequestException("Gateway down");
    }
}

internal sealed class RecordingAiAssistantService : IRcaAiAssistantService
{
    private readonly ApiResult<RcaAiCauseSuggestionResultDto>? _causeResult;
    private readonly ApiResult<RcaAiActionSuggestionResultDto>? _actionResult;
    private readonly ApiResult<RcaAiSummaryResultDto>? _summaryResult;
    private readonly ApiResult<RcaAiRecurrenceResultDto>? _recurrenceResult;
    private readonly ApiResult<RcaAiEightDDraftResultDto>? _eightDResult;

    public RecordingAiAssistantService(
        ApiResult<RcaAiCauseSuggestionResultDto>? causeResult = null,
        ApiResult<RcaAiActionSuggestionResultDto>? actionResult = null,
        ApiResult<RcaAiSummaryResultDto>? summaryResult = null,
        ApiResult<RcaAiRecurrenceResultDto>? recurrenceResult = null,
        ApiResult<RcaAiEightDDraftResultDto>? eightDResult = null)
    {
        _causeResult = causeResult;
        _actionResult = actionResult;
        _summaryResult = summaryResult;
        _recurrenceResult = recurrenceResult;
        _eightDResult = eightDResult;
    }

    public bool DetectRecurrenceCalled { get; private set; }
    public bool GenerateEightDCalled { get; private set; }
    public bool ListSuggestionsCalled { get; private set; }
    public bool AcceptSuggestionCalled { get; private set; }
    public bool RejectSuggestionCalled { get; private set; }
    public string? LastAcceptedReviewedByUserId { get; private set; }
    public string? LastRejectedReviewedByUserId { get; private set; }

    public Task<ApiResult<RcaAiCauseSuggestionResultDto>> SuggestCausesAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        if (_causeResult is not null)
        {
            return Task.FromResult(_causeResult);
        }

        return Task.FromResult(ApiResult<RcaAiCauseSuggestionResultDto>.Ok(new RcaAiCauseSuggestionResultDto
        {
            IncidentId = incidentId,
            Summary = "ok",
            Suggestions = [],
            Metadata = new RcaAiSuggestionMetadataDto()
        }));
    }

    public Task<ApiResult<RcaAiActionSuggestionResultDto>> SuggestActionsAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        if (_actionResult is not null)
        {
            return Task.FromResult(_actionResult);
        }

        return Task.FromResult(ApiResult<RcaAiActionSuggestionResultDto>.Ok(new RcaAiActionSuggestionResultDto
        {
            IncidentId = incidentId,
            Summary = "ok",
            Suggestions = [],
            Metadata = new RcaAiSuggestionMetadataDto()
        }));
    }

    public Task<ApiResult<RcaAiSummaryResultDto>> SummarizeAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        if (_summaryResult is not null)
        {
            return Task.FromResult(_summaryResult);
        }

        return Task.FromResult(ApiResult<RcaAiSummaryResultDto>.Ok(new RcaAiSummaryResultDto
        {
            IncidentId = incidentId,
            ExecutiveSummary = "ok",
            RiskAssessment = "ok",
            RecommendedNextSteps = [],
            Metadata = new RcaAiSuggestionMetadataDto()
        }));
    }

    public Task<ApiResult<RcaAiRecurrenceResultDto>> DetectRecurrenceAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        DetectRecurrenceCalled = true;
        if (_recurrenceResult is not null)
        {
            return Task.FromResult(_recurrenceResult);
        }

        return Task.FromResult(ApiResult<RcaAiRecurrenceResultDto>.Ok(new RcaAiRecurrenceResultDto
        {
            IncidentId = incidentId,
            Metadata = new RcaAiSuggestionMetadataDto()
        }));
    }

    public Task<ApiResult<RcaAiEightDDraftResultDto>> GenerateEightDDraftAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        GenerateEightDCalled = true;
        if (_eightDResult is not null)
        {
            return Task.FromResult(_eightDResult);
        }

        return Task.FromResult(ApiResult<RcaAiEightDDraftResultDto>.Ok(new RcaAiEightDDraftResultDto
        {
            IncidentId = incidentId,
            Metadata = new RcaAiSuggestionMetadataDto()
        }));
    }

    public Task<ApiResult<IReadOnlyList<RcaAiSuggestionDto>>> ListSuggestionsAsync(Guid incidentId, string? status, CancellationToken cancellationToken = default)
    {
        ListSuggestionsCalled = true;
        return Task.FromResult(ApiResult<IReadOnlyList<RcaAiSuggestionDto>>.Ok([]));
    }

    public Task<ApiResult<RcaAiSuggestionDto>> AcceptSuggestionAsync(Guid incidentId, Guid suggestionId, AcceptRcaAiSuggestionRequest request, CancellationToken cancellationToken = default)
    {
        AcceptSuggestionCalled = true;
        LastAcceptedReviewedByUserId = request.ReviewedByUserId;
        return Task.FromResult(ApiResult<RcaAiSuggestionDto>.Ok(new RcaAiSuggestionDto
        {
            Id = suggestionId,
            RcaIncidentId = incidentId,
            Status = nameof(RcaAiSuggestionStatus.Accepted)
        }));
    }

    public Task<ApiResult<RcaAiSuggestionDto>> RejectSuggestionAsync(Guid incidentId, Guid suggestionId, RejectRcaAiSuggestionRequest request, CancellationToken cancellationToken = default)
    {
        RejectSuggestionCalled = true;
        LastRejectedReviewedByUserId = request.ReviewedByUserId;
        return Task.FromResult(ApiResult<RcaAiSuggestionDto>.Ok(new RcaAiSuggestionDto
        {
            Id = suggestionId,
            RcaIncidentId = incidentId,
            Status = nameof(RcaAiSuggestionStatus.Rejected)
        }));
    }
}

internal sealed class FixedCurrentRcaUserContext : ICurrentRcaUserContext
{
    public FixedCurrentRcaUserContext(string userId = "test-user", Guid? tenantId = null)
    {
        UserId = userId;
        TenantId = tenantId ?? Guid.NewGuid();
    }

    public Guid TenantId { get; }

    public string UserId { get; }

    public bool IsInRole(string role)
    {
        return true;
    }
}

internal sealed class RecordingAiSuggestionStore : IRcaAiSuggestionStore
{
    public List<SavedAiSuggestion> Saved { get; } = [];
    public List<RcaAiSuggestionDto> Suggestions { get; } = [];
    public List<RecordedAiSuggestionAudit> AuditRecords { get; } = [];
    public int SaveBatchCallCount { get; private set; }
    public int ReviewTransactionCallCount { get; private set; }

    public Task SavePendingBatchAsync(
        Guid tenantId,
        Guid incidentId,
        IReadOnlyList<RcaAiPendingSuggestionInput> suggestions,
        string createdByUserId,
        CancellationToken cancellationToken = default)
    {
        SaveBatchCallCount++;
        Saved.AddRange(suggestions.Select(x => new SavedAiSuggestion(
            tenantId,
            incidentId,
            x.Type,
            x.Title,
            x.Summary,
            x.Payload,
            x.Metadata,
            createdByUserId)));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RcaAiSuggestionDto>> ListAsync(Guid tenantId, Guid incidentId, RcaAiSuggestionStatus? status, CancellationToken cancellationToken = default)
    {
        var suggestions = Suggestions
            .Where(x => x.TenantId == tenantId && x.RcaIncidentId == incidentId)
            .Where(x => !status.HasValue || x.Status == status.Value.ToString())
            .ToList();

        return Task.FromResult<IReadOnlyList<RcaAiSuggestionDto>>(suggestions);
    }

    public Task<RcaAiSuggestionDto?> GetAsync(Guid tenantId, Guid incidentId, Guid suggestionId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Suggestions.FirstOrDefault(x =>
            x.TenantId == tenantId &&
            x.RcaIncidentId == incidentId &&
            x.Id == suggestionId));
    }

    public async Task<ApiResult<RcaAiSuggestionDto>> ExecuteReviewTransactionAsync(
        Func<CancellationToken, Task<ApiResult<RcaAiSuggestionDto>>> operation,
        CancellationToken cancellationToken = default)
    {
        ReviewTransactionCallCount++;
        return await operation(cancellationToken);
    }

    public Task<RcaAiSuggestionDto?> ClaimAcceptedAsync(
        Guid tenantId,
        Guid incidentId,
        Guid suggestionId,
        string reviewedByUserId,
        string reviewNotes,
        CancellationToken cancellationToken = default)
    {
        var suggestion = Suggestions.SingleOrDefault(x =>
            x.TenantId == tenantId &&
            x.RcaIncidentId == incidentId &&
            x.Id == suggestionId &&
            x.Status == nameof(RcaAiSuggestionStatus.Pending));
        if (suggestion is null)
        {
            return Task.FromResult<RcaAiSuggestionDto?>(null);
        }

        suggestion.Status = nameof(RcaAiSuggestionStatus.Accepted);
        suggestion.ReviewedByUserId = reviewedByUserId;
        suggestion.ReviewNotes = reviewNotes;
        suggestion.ReviewedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<RcaAiSuggestionDto?>(suggestion);
    }

    public Task<RcaAiSuggestionDto?> CompleteAcceptedAsync(
        Guid tenantId,
        Guid incidentId,
        Guid suggestionId,
        string appliedEntityType,
        Guid? appliedEntityId,
        CancellationToken cancellationToken = default)
    {
        var suggestion = Suggestions.SingleOrDefault(x =>
            x.TenantId == tenantId &&
            x.RcaIncidentId == incidentId &&
            x.Id == suggestionId &&
            x.Status == nameof(RcaAiSuggestionStatus.Accepted));
        if (suggestion is null)
        {
            return Task.FromResult<RcaAiSuggestionDto?>(null);
        }

        suggestion.AppliedEntityType = appliedEntityType;
        suggestion.AppliedEntityId = appliedEntityId;
        AuditRecords.Add(new RecordedAiSuggestionAudit(tenantId, incidentId, suggestionId, "AiSuggestionAccepted"));
        return Task.FromResult<RcaAiSuggestionDto?>(suggestion);
    }

    public Task<RcaAiSuggestionDto?> MarkRejectedAsync(
        Guid tenantId,
        Guid incidentId,
        Guid suggestionId,
        string reviewedByUserId,
        string reviewNotes,
        CancellationToken cancellationToken = default)
    {
        var suggestion = Suggestions.SingleOrDefault(x =>
            x.TenantId == tenantId &&
            x.RcaIncidentId == incidentId &&
            x.Id == suggestionId &&
            x.Status == nameof(RcaAiSuggestionStatus.Pending));
        if (suggestion is null)
        {
            return Task.FromResult<RcaAiSuggestionDto?>(null);
        }

        suggestion.Status = nameof(RcaAiSuggestionStatus.Rejected);
        suggestion.ReviewedByUserId = reviewedByUserId;
        suggestion.ReviewNotes = reviewNotes;
        suggestion.ReviewedAt = DateTimeOffset.UtcNow;
        AuditRecords.Add(new RecordedAiSuggestionAudit(tenantId, incidentId, suggestionId, "AiSuggestionRejected"));
        return Task.FromResult<RcaAiSuggestionDto?>(suggestion);
    }
}

internal sealed record SavedAiSuggestion(
    Guid TenantId,
    Guid IncidentId,
    RcaAiSuggestionType Type,
    string Title,
    string Summary,
    object Payload,
    RcaAiSuggestionMetadataDto Metadata,
    string CreatedByUserId);

internal sealed record RecordedAiSuggestionAudit(Guid TenantId, Guid IncidentId, Guid SuggestionId, string Action);

internal sealed class FixedAiGatewayClient : IRcaAiGatewayClient
{
    public Task<ApiResult<RcaAiCauseSuggestionResultDto>> SuggestCausesAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResult<RcaAiCauseSuggestionResultDto>.Ok(new RcaAiCauseSuggestionResultDto
        {
            IncidentId = context.Incident.Id,
            Summary = "Gateway causes",
            Suggestions =
            [
                new RcaAiCauseSuggestionDto { BranchName = "Metodo", Title = "Review setup", ConfidenceScore = 80 },
                new RcaAiCauseSuggestionDto { BranchName = "Material", Title = "Check batch", ConfidenceScore = 70 }
            ],
            Metadata = new RcaAiSuggestionMetadataDto
            {
                Provider = "Test",
                Model = "fixed",
                IsFallback = true
            }
        }));
    }

    public Task<ApiResult<RcaAiActionSuggestionResultDto>> SuggestActionsAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResult<RcaAiActionSuggestionResultDto>.Ok(new RcaAiActionSuggestionResultDto
        {
            IncidentId = context.Incident.Id,
            Summary = "Gateway actions",
            Suggestions = [],
            Metadata = new RcaAiSuggestionMetadataDto()
        }));
    }

    public Task<ApiResult<RcaAiSummaryResultDto>> SummarizeAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResult<RcaAiSummaryResultDto>.Ok(new RcaAiSummaryResultDto
        {
            IncidentId = context.Incident.Id,
            ExecutiveSummary = "Summary",
            RiskAssessment = "Risk",
            RecommendedNextSteps = [],
            Metadata = new RcaAiSuggestionMetadataDto()
        }));
    }

    public Task<ApiResult<RcaAiRecurrenceResultDto>> DetectRecurrenceAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResult<RcaAiRecurrenceResultDto>.Ok(new RcaAiRecurrenceResultDto
        {
            IncidentId = context.Incident.Id,
            Metadata = new RcaAiSuggestionMetadataDto()
        }));
    }

    public Task<ApiResult<RcaAiEightDDraftResultDto>> GenerateEightDDraftAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResult<RcaAiEightDDraftResultDto>.Ok(new RcaAiEightDDraftResultDto
        {
            IncidentId = context.Incident.Id,
            Metadata = new RcaAiSuggestionMetadataDto()
        }));
    }
}

internal sealed class FixedRcaIncidentService : IRcaIncidentService
{
    private readonly Guid _tenantId;
    private readonly Guid _incidentId;
    private readonly Guid _branchId;

    public FixedRcaIncidentService(Guid tenantId, Guid incidentId, Guid? branchId = null)
    {
        _tenantId = tenantId;
        _incidentId = incidentId;
        _branchId = branchId ?? Guid.NewGuid();
    }

    public List<AddIshikawaCauseRequest> AddedCauses { get; } = [];
    public List<AddCorrectiveActionRequest> AddedActions { get; } = [];

    public Task<ApiResult<RcaIncidentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResult<RcaIncidentDto>.Ok(new RcaIncidentDto
        {
            Id = _incidentId,
            TenantId = _tenantId,
            Title = "AI persistence test",
            Severity = "High",
            Status = "Open"
        }));
    }

    public Task<ApiResult<IshikawaCanvasDto>> GetCanvasAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResult<IshikawaCanvasDto>.Ok(new IshikawaCanvasDto
        {
            RcaIncidentId = incidentId,
            Branches =
            [
                new IshikawaBranchDto { Id = _branchId, Name = "Metodo", Order = 1 }
            ]
        }));
    }

    public Task<ApiResult<IReadOnlyList<CorrectiveActionDto>>> ListCorrectiveActionsAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResult<IReadOnlyList<CorrectiveActionDto>>.Ok([]));
    }

    public Task<ApiResult<RcaIncidentDto>> CreateAsync(CreateRcaIncidentRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ApiResult<IReadOnlyList<RcaIncidentDto>>> ListAsync(string? sourceSystem = null, string? externalTaskId = null, string? status = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ApiResult<IshikawaCauseDto>> AddCauseAsync(Guid incidentId, AddIshikawaCauseRequest request, CancellationToken cancellationToken = default)
    {
        AddedCauses.Add(request);
        return Task.FromResult(ApiResult<IshikawaCauseDto>.Ok(new IshikawaCauseDto
        {
            Id = Guid.NewGuid(),
            BranchId = request.BranchId,
            Title = request.Title
        }));
    }

    public Task<ApiResult<CorrectiveActionDto>> AddCorrectiveActionAsync(Guid incidentId, AddCorrectiveActionRequest request, CancellationToken cancellationToken = default)
    {
        AddedActions.Add(request);
        return Task.FromResult(ApiResult<CorrectiveActionDto>.Ok(new CorrectiveActionDto
        {
            Id = Guid.NewGuid(),
            RcaIncidentId = incidentId,
            Title = request.Title
        }));
    }
    public Task<ApiResult<CorrectiveActionDto>> UpdateCorrectiveActionStatusAsync(Guid incidentId, Guid actionId, UpdateCorrectiveActionStatusRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ApiResult<IReadOnlyList<RcaEvidenceDto>>> ListEvidenceAsync(Guid incidentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ApiResult<RcaEvidenceDto>> AddEvidenceAsync(Guid incidentId, AddRcaEvidenceRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ApiResult<RcaEvidenceDto>> UpdateEvidenceAsync(Guid incidentId, Guid evidenceId, UpdateRcaEvidenceRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ApiResult<RcaEvidenceDto>> ReplaceEvidenceAttachmentAsync(Guid incidentId, Guid evidenceId, ReplaceRcaEvidenceAttachmentRequest request, string? replacedByUserId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ApiResult<RcaEvidenceDto>> DeleteEvidenceAsync(Guid incidentId, Guid evidenceId, string? deletedByUserId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ApiResult<IReadOnlyList<RcaFactDto>>> ListFactsAsync(Guid incidentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ApiResult<RcaFactDto>> AddFactAsync(Guid incidentId, AddRcaFactRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ApiResult<IReadOnlyList<RcaAuditRecordDto>>> ListAuditRecordsAsync(Guid incidentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ApiResult<RcaIncidentDto>> CloseAsync(Guid incidentId, CloseRcaIncidentRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ApiResult<RcaIncidentDto>> EscalateTo8DAsync(Guid incidentId, EscalateRcaIncidentTo8DRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ApiResult<RcaIncidentDto>> CompleteWizardStepAsync(Guid incidentId, CompleteRcaWizardStepRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ApiResult<RcaWizardProgressDto>> GetWizardProgressAsync(Guid incidentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ApiResult<RcaIntegrationSnapshotDto>> GetIntegrationSnapshotAsync(Guid incidentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ApiResult<IReadOnlyList<RcaIntegrationSnapshotDto>>> ListIntegrationSnapshotsAsync(string? sourceSystem = null, string? externalTaskId = null, string? status = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ApiResult<IReadOnlyList<RcaDomainEventDto>>> ListIntegrationEventsAsync(Guid? incidentId = null, DateTimeOffset? since = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;

    public RecordingHttpMessageHandler(HttpStatusCode statusCode)
    {
        _statusCode = statusCode;
    }

    public List<HttpRequestMessage> Requests { get; } = [];
    public List<string> Bodies { get; } = [];
    public string ResponseContent { get; set; } = "{}";
    public Func<HttpRequestMessage, HttpContent>? ResponseBodyFactory { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        Bodies.Add(request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken));

        return new HttpResponseMessage(_statusCode)
        {
            Content = ResponseBodyFactory?.Invoke(request) ?? new StringContent(ResponseContent, Encoding.UTF8, "application/json")
        };
    }
}

internal sealed class SlowHttpMessageHandler : HttpMessageHandler
{
    private readonly TimeSpan _delay;

    public SlowHttpMessageHandler(TimeSpan delay)
    {
        _delay = delay;
    }

    public bool WasCanceled { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_delay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            WasCanceled = true;
            throw;
        }

        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}

internal sealed class DelayedReadJsonContent : HttpContent
{
    private readonly byte[] _bytes;
    private readonly TimeSpan _delay;

    public DelayedReadJsonContent(string json, TimeSpan delay)
    {
        _bytes = Encoding.UTF8.GetBytes(json);
        _delay = delay;
        Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        return SerializeToStreamAsync(stream, context, CancellationToken.None);
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        await Task.Delay(_delay, cancellationToken);
        await stream.WriteAsync(_bytes, cancellationToken);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _bytes.Length;
        return true;
    }

    protected override Task<Stream> CreateContentReadStreamAsync()
    {
        return Task.FromResult<Stream>(new DelayedReadMemoryStream(_bytes, _delay));
    }
}

internal sealed class DelayedReadMemoryStream : MemoryStream
{
    private readonly TimeSpan _delay;

    public DelayedReadMemoryStream(byte[] buffer, TimeSpan delay)
        : base(buffer, writable: false)
    {
        _delay = delay;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await Task.Delay(_delay, cancellationToken);
        return await base.ReadAsync(buffer, cancellationToken);
    }
}
