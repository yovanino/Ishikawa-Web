using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Entities;
using IshikawaRca.Domain.Enums;
using IshikawaRca.Domain.Services;

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
