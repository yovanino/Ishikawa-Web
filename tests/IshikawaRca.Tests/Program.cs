using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Entities;
using IshikawaRca.Domain.Enums;
using IshikawaRca.Domain.Services;
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
