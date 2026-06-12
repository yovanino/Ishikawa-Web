using IshikawaRca.Application.Ai;
using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Entities;
using IshikawaRca.Domain.Enums;
using IshikawaRca.Domain.Services;
using IshikawaRca.Infrastructure.Ai;
using IshikawaRca.Infrastructure.Services;
using IshikawaRca.Web.Controllers.Api;
using IshikawaRca.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
await AssertConfiguredAiGatewayFallsBackWhenHttpFailsAsync();
await AssertConfiguredAiGatewayUsesStubModeEvenWhenHttpWouldFailAsync();
await AssertConfiguredAiGatewayReturnsHttpFailureWhenFallbackIsDisabledAsync();
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
