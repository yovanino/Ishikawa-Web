using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Entities;
using IshikawaRca.Domain.Enums;
using IshikawaRca.Domain.Services;
using IshikawaRca.Infrastructure.Services;
using IshikawaRca.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

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
}
