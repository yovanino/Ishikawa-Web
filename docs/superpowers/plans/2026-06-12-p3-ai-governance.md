# P3 AI Governance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement governed AI assistance for Ishikawa RCA with real AI Gateway HTTP mode, recurrence/8D suggestions, persisted suggestion review, human accept/reject workflow, auditability and UI review.

**Architecture:** Keep `IRcaAiGatewayClient` as the application boundary. Infrastructure selects stub or HTTP gateway by `AiGateway` configuration, while application services build RCA context and persist pending AI suggestions. Official RCA data only changes through explicit accept endpoints and existing RCA services.

**Tech Stack:** ASP.NET Core MVC/API, C#/.NET 9, EF Core/MySQL, existing console-style tests in `tests/IshikawaRca.Tests`, Markdown docs.

---

## File Map

- Modify `src/IshikawaRca.Application/Ai/IRcaAiGatewayClient.cs`: add recurrence and 8D gateway methods.
- Modify `src/IshikawaRca.Application/Ai/IRcaAiAssistantService.cs`: add recurrence, 8D, list suggestions, accept, reject.
- Modify `src/IshikawaRca.Application/Ai/RcaAiAssistantService.cs`: build context once, call gateway, persist suggestions and apply accepted cause/action suggestions.
- Create `src/IshikawaRca.Application/Ai/RcaAiGatewayOptions.cs`: configuration object for `AiGateway`.
- Create `src/IshikawaRca.Infrastructure/Ai/HttpRcaAiGatewayClient.cs`: HTTP JSON client for gateway endpoints.
- Create `src/IshikawaRca.Infrastructure/Ai/ConfiguredRcaAiGatewayClient.cs`: mode/fallback wrapper around HTTP and stub clients.
- Modify `src/IshikawaRca.Infrastructure/Ai/StubRcaAiGatewayClient.cs`: add recurrence and 8D deterministic results.
- Create contract DTOs in `src/IshikawaRca.Contracts/Rca`: recurrence result, 8D draft result, suggestion record DTO, accept/reject requests.
- Create domain entities/enums in `src/IshikawaRca.Domain`: `RcaAiSuggestion`, `RcaAiSuggestionType`, `RcaAiSuggestionStatus`.
- Modify `src/IshikawaRca.Infrastructure/Data/RcaDbContext.cs`: add DbSet and mapping.
- Add EF migration under `src/IshikawaRca.Infrastructure/Data/Migrations`: create `rca_ai_suggestions`.
- Modify `src/IshikawaRca.Infrastructure/DependencyInjection.cs`: bind options and register AI client wrapper.
- Modify `src/IshikawaRca.Web/Controllers/Api/RcaAiController.cs`: add endpoints for recurrence, 8D, suggestions, accept and reject.
- Modify `src/IshikawaRca.Web/Models/Rca/RcaIncidentDetailsViewModel.cs` and `src/IshikawaRca.Web/Views/Rca/Details.cshtml`: add compact AI governance panel.
- Modify docs: `docs/AI_INTEGRATION.md`, `docs/API_CONTRACTS.md`, `docs/backend.md`, `docs/chats/BACKEND.md`, `docs/ROADMAP.md`, `docs/STATUS_AND_NEXT_STEPS.md`, `docs/VALIDATION_LOG.md`.
- Modify `tests/IshikawaRca.Tests/Program.cs`: add all lightweight tests.

---

### Task 1: AI Gateway Options and HTTP Client Base

**Files:**
- Create: `src/IshikawaRca.Application/Ai/RcaAiGatewayOptions.cs`
- Create: `src/IshikawaRca.Infrastructure/Ai/HttpRcaAiGatewayClient.cs`
- Modify: `src/IshikawaRca.Infrastructure/DependencyInjection.cs`
- Test: `tests/IshikawaRca.Tests/Program.cs`

- [ ] **Step 1: Write the failing test**

Add a test call near the existing top-level assertions:

```csharp
await AssertHttpAiGatewayClientPostsCauseContextAsync();
```

Add this test and helper:

```csharp
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
```

Extend `RecordingHttpMessageHandler`:

```csharp
public string ResponseContent { get; set; } = "{}";
```

and return:

```csharp
return new HttpResponseMessage(_statusCode)
{
    Content = new StringContent(ResponseContent, Encoding.UTF8, "application/json")
};
```

- [ ] **Step 2: Run RED**

Run:

```powershell
dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj
```

Expected: compile failure because `HttpRcaAiGatewayClient` and `RcaAiGatewayOptions` do not exist.

- [ ] **Step 3: Implement minimal options and HTTP client**

Create `RcaAiGatewayOptions.cs`:

```csharp
namespace IshikawaRca.Application.Ai;

public class RcaAiGatewayOptions
{
    public const string SectionName = "AiGateway";

    public string Mode { get; set; } = "Stub";
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public string ApiKey { get; set; } = string.Empty;
    public bool UseFallbackOnFailure { get; set; } = true;
}
```

Create `HttpRcaAiGatewayClient.cs` with:

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IshikawaRca.Application.Ai;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using Microsoft.Extensions.Options;

namespace IshikawaRca.Infrastructure.Ai;

public class HttpRcaAiGatewayClient : IRcaAiGatewayClient
{
    private readonly HttpClient _httpClient;
    private readonly RcaAiGatewayOptions _options;

    public HttpRcaAiGatewayClient(HttpClient httpClient, IOptions<RcaAiGatewayOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public Task<ApiResult<RcaAiCauseSuggestionResultDto>> SuggestCausesAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return PostAsync<RcaAiCauseSuggestionResultDto>("/ai/rca/suggest-causes", context, cancellationToken);
    }

    public Task<ApiResult<RcaAiActionSuggestionResultDto>> SuggestActionsAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return PostAsync<RcaAiActionSuggestionResultDto>("/ai/rca/suggest-actions", context, cancellationToken);
    }

    public Task<ApiResult<RcaAiSummaryResultDto>> SummarizeAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return PostAsync<RcaAiSummaryResultDto>("/ai/rca/summarize", context, cancellationToken);
    }

    private async Task<ApiResult<T>> PostAsync<T>(string path, RcaAiContextDto context, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return ApiResult<T>.Fail("AI Gateway BaseUrl no es valido.", new ApiError
            {
                Code = "AI_GATEWAY_CONFIGURATION_INVALID",
                Message = "AiGateway:BaseUrl debe ser una URL absoluta.",
                Field = "AiGateway.BaseUrl"
            });
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)));

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, path))
        {
            Content = JsonContent.Create(context)
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        using var response = await _httpClient.SendAsync(request, timeout.Token);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<T>.Fail($"AI Gateway respondio HTTP {(int)response.StatusCode}.", new ApiError
            {
                Code = "AI_GATEWAY_UNAVAILABLE",
                Message = "AI Gateway no esta disponible.",
                Field = "AiGateway"
            });
        }

        var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: timeout.Token);
        return data is null
            ? ApiResult<T>.Fail("AI Gateway devolvio una respuesta vacia.", new ApiError { Code = "AI_GATEWAY_INVALID_RESPONSE", Message = "Respuesta IA vacia.", Field = "AiGateway" })
            : ApiResult<T>.Ok(data);
    }
}
```

- [ ] **Step 4: Run GREEN**

Run tests and build:

```powershell
dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj
dotnet build IshikawaRca.sln /m:1
git diff --check
```

Expected: all pass.

- [ ] **Step 5: Update docs and commit**

Update `docs/AI_INTEGRATION.md`, `docs/backend.md`, `docs/chats/BACKEND.md`, `docs/VALIDATION_LOG.md`.

Commit:

```powershell
git add src/IshikawaRca.Application/Ai/RcaAiGatewayOptions.cs src/IshikawaRca.Infrastructure/Ai/HttpRcaAiGatewayClient.cs src/IshikawaRca.Infrastructure/DependencyInjection.cs tests/IshikawaRca.Tests/Program.cs docs/AI_INTEGRATION.md docs/backend.md docs/chats/BACKEND.md docs/VALIDATION_LOG.md
git commit -m "feat(ai): add HTTP RCA AI gateway client"
```

---

### Task 2: AI Gateway Mode Selection and Fallback

**Files:**
- Create: `src/IshikawaRca.Infrastructure/Ai/ConfiguredRcaAiGatewayClient.cs`
- Modify: `src/IshikawaRca.Infrastructure/DependencyInjection.cs`
- Test: `tests/IshikawaRca.Tests/Program.cs`

- [ ] **Step 1: Write RED test**

Add:

```csharp
await AssertConfiguredAiGatewayFallsBackWhenHttpFailsAsync();
```

Test:

```csharp
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
```

Helper:

```csharp
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
```

- [ ] **Step 2: Run RED**

Expected: compile failure because `ConfiguredRcaAiGatewayClient` does not exist.

- [ ] **Step 3: Implement wrapper and DI**

Create wrapper:

```csharp
using IshikawaRca.Application.Ai;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using Microsoft.Extensions.Options;

namespace IshikawaRca.Infrastructure.Ai;

public class ConfiguredRcaAiGatewayClient : IRcaAiGatewayClient
{
    private readonly IRcaAiGatewayClient _httpClient;
    private readonly IRcaAiGatewayClient _stubClient;
    private readonly RcaAiGatewayOptions _options;

    public ConfiguredRcaAiGatewayClient(IRcaAiGatewayClient httpClient, IRcaAiGatewayClient stubClient, IOptions<RcaAiGatewayOptions> options)
    {
        _httpClient = httpClient;
        _stubClient = stubClient;
        _options = options.Value;
    }

    public Task<ApiResult<RcaAiCauseSuggestionResultDto>> SuggestCausesAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(client => client.SuggestCausesAsync(context, cancellationToken), cancellationToken);
    }

    public Task<ApiResult<RcaAiActionSuggestionResultDto>> SuggestActionsAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(client => client.SuggestActionsAsync(context, cancellationToken), cancellationToken);
    }

    public Task<ApiResult<RcaAiSummaryResultDto>> SummarizeAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(client => client.SummarizeAsync(context, cancellationToken), cancellationToken);
    }

    private async Task<ApiResult<T>> ExecuteAsync<T>(Func<IRcaAiGatewayClient, Task<ApiResult<T>>> action, CancellationToken cancellationToken)
    {
        if (!string.Equals(_options.Mode, "Http", StringComparison.OrdinalIgnoreCase))
        {
            return await action(_stubClient);
        }

        var result = await action(_httpClient);
        if (result.Success || !_options.UseFallbackOnFailure)
        {
            return result;
        }

        return await action(_stubClient);
    }
}
```

In DI, configure options and register concrete clients:

```csharp
services.Configure<RcaAiGatewayOptions>(configuration.GetSection(RcaAiGatewayOptions.SectionName));
services.AddScoped<StubRcaAiGatewayClient>();
services.AddScoped<HttpRcaAiGatewayClient>(provider => new HttpRcaAiGatewayClient(new HttpClient(), provider.GetRequiredService<IOptions<RcaAiGatewayOptions>>()));
services.AddScoped<IRcaAiGatewayClient>(provider => new ConfiguredRcaAiGatewayClient(
    provider.GetRequiredService<HttpRcaAiGatewayClient>(),
    provider.GetRequiredService<StubRcaAiGatewayClient>(),
    provider.GetRequiredService<IOptions<RcaAiGatewayOptions>>()));
```

- [ ] **Step 4: Validate and commit**

Run tests/build/diff-check. Commit:

```powershell
git add src/IshikawaRca.Infrastructure/Ai/ConfiguredRcaAiGatewayClient.cs src/IshikawaRca.Infrastructure/DependencyInjection.cs tests/IshikawaRca.Tests/Program.cs docs
git commit -m "feat(ai): select AI gateway mode with fallback"
```

---

### Task 3: Recurrence and 8D Draft Contracts and Endpoints

**Files:**
- Create: `src/IshikawaRca.Contracts/Rca/RcaAiRecurrenceResultDto.cs`
- Create: `src/IshikawaRca.Contracts/Rca/RcaAiEightD﻿DraftResultDto.cs`
- Modify: `IRcaAiGatewayClient.cs`, `IRcaAiAssistantService.cs`, `RcaAiAssistantService.cs`, `StubRcaAiGatewayClient.cs`, `HttpRcaAiGatewayClient.cs`, `ConfiguredRcaAiGatewayClient.cs`, `RcaAiController.cs`
- Test: `tests/IshikawaRca.Tests/Program.cs`

- [ ] **Step 1: Write RED controller test**

Add:

```csharp
await AssertAiControllerExposesRecurrenceAndEightDAsync();
```

Test:

```csharp
static async Task AssertAiControllerExposesRecurrenceAndEightDAsync()
{
    var service = new RecordingAiAssistantService();
    var controller = new RcaAiController(service);
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
```

Implement `RecordingAiAssistantService` with all interface methods returning successful minimal DTOs.

- [ ] **Step 2: Run RED**

Expected: compile errors for missing DTOs/interface methods/controller actions.

- [ ] **Step 3: Add DTOs**

`RcaAiRecurrenceResultDto.cs`:

```csharp
namespace IshikawaRca.Contracts.Rca;

public class RcaAiRecurrenceResultDto
{
    public Guid IncidentId { get; set; }
    public bool IsLikelyRecurring { get; set; }
    public int ConfidenceScore { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public IReadOnlyList<string> SimilarSignals { get; set; } = [];
    public RcaAiSuggestionMetadataDto Metadata { get; set; } = new();
}
```

`RcaAiEightDDraftResultDto.cs`:

```csharp
namespace IshikawaRca.Contracts.Rca;

public class RcaAiEightDDraftResultDto
{
    public Guid IncidentId { get; set; }
    public string ProblemStatement { get; set; } = string.Empty;
    public string ContainmentActions { get; set; } = string.Empty;
    public string RootCauseAnalysis { get; set; } = string.Empty;
    public string CorrectiveActions { get; set; } = string.Empty;
    public string VerificationPlan { get; set; } = string.Empty;
    public RcaAiSuggestionMetadataDto Metadata { get; set; } = new();
}
```

- [ ] **Step 4: Extend interfaces and services**

Add methods:

```csharp
Task<ApiResult<RcaAiRecurrenceResultDto>> DetectRecurrenceAsync(Guid incidentId, CancellationToken cancellationToken = default);
Task<ApiResult<RcaAiEightDDraftResultDto>> GenerateEightDDraftAsync(Guid incidentId, CancellationToken cancellationToken = default);
```

Gateway interface uses `RcaAiContextDto` instead of `Guid`.

Stub recurrence:

```csharp
return ApiResult<RcaAiRecurrenceResultDto>.Ok(new RcaAiRecurrenceResultDto
{
    IncidentId = context.Incident.Id,
    IsLikelyRecurring = context.Canvas.Causes.Count > 2,
    ConfidenceScore = 62,
    Rationale = "Modo stub: recurrencia estimada por cantidad de causas y acciones abiertas.",
    SimilarSignals = ["Misma linea o maquina", "Acciones abiertas"],
    Metadata = CreateMetadata()
});
```

Stub 8D:

```csharp
return ApiResult<RcaAiEightDDraftResultDto>.Ok(new RcaAiEightDDraftResultDto
{
    IncidentId = context.Incident.Id,
    ProblemStatement = context.Incident.ProblemDescription,
    ContainmentActions = "Definir contencion temporal y responsable.",
    RootCauseAnalysis = "Completar causa raiz con evidencia validada.",
    CorrectiveActions = "Convertir acciones RCA aceptadas en plan 8D.",
    VerificationPlan = "Verificar eficacia antes de cierre formal.",
    Metadata = CreateMetadata()
});
```

- [ ] **Step 5: Add controller actions**

Add:

```csharp
[HttpPost("detect-recurrence")]
public async Task<ActionResult<ApiResult<RcaAiRecurrenceResultDto>>> DetectRecurrence(Guid id, CancellationToken cancellationToken)
{
    var result = await _aiAssistantService.DetectRecurrenceAsync(id, cancellationToken);
    return result.Success ? Ok(result) : NotFound(result);
}

[HttpPost("generate-8d-draft")]
public async Task<ActionResult<ApiResult<RcaAiEightDDraftResultDto>>> GenerateEightDDraft(Guid id, CancellationToken cancellationToken)
{
    var result = await _aiAssistantService.GenerateEightDDraftAsync(id, cancellationToken);
    return result.Success ? Ok(result) : NotFound(result);
}
```

- [ ] **Step 6: Validate and commit**

Run tests/build/diff-check. Commit:

```powershell
git add src tests docs
git commit -m "feat(ai): add recurrence and 8D draft suggestions"
```

---

### Task 4: AI Suggestion Domain, EF Mapping and Migration

**Files:**
- Create: `src/IshikawaRca.Domain/Enums/RcaAiSuggestionType.cs`
- Create: `src/IshikawaRca.Domain/Enums/RcaAiSuggestionStatus.cs`
- Create: `src/IshikawaRca.Domain/Entities/RcaAiSuggestion.cs`
- Modify: `src/IshikawaRca.Infrastructure/Data/RcaDbContext.cs`
- Add migration.
- Test: `tests/IshikawaRca.Tests/Program.cs`

- [ ] **Step 1: Write RED domain test**

Add:

```csharp
AssertAiSuggestionDefaults();
```

Test:

```csharp
static void AssertAiSuggestionDefaults()
{
    var suggestion = new RcaAiSuggestion
    {
        TenantId = Guid.NewGuid(),
        IncidentId = Guid.NewGuid(),
        SuggestionType = RcaAiSuggestionType.Cause,
        Title = "AI cause",
        PayloadJson = "{}",
        CreatedAt = DateTimeOffset.UtcNow
    };

    if (suggestion.Status != RcaAiSuggestionStatus.Pending ||
        suggestion.Id == Guid.Empty ||
        suggestion.IsFallback)
    {
        throw new InvalidOperationException("Expected AI suggestions to default to pending, non-fallback review records.");
    }
}
```

- [ ] **Step 2: Run RED**

Expected: missing types.

- [ ] **Step 3: Implement domain types**

Enums:

```csharp
namespace IshikawaRca.Domain.Enums;

public enum RcaAiSuggestionType { Cause = 0, Action = 1, Summary = 2, Recurrence = 3, EightD = 4 }
public enum RcaAiSuggestionStatus { Pending = 0, Accepted = 1, Rejected = 2, Expired = 3 }
```

Entity:

```csharp
using IshikawaRca.Domain.Enums;

namespace IshikawaRca.Domain.Entities;

public class RcaAiSuggestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid IncidentId { get; set; }
    public RcaAiSuggestionType SuggestionType { get; set; }
    public RcaAiSuggestionStatus Status { get; set; } = RcaAiSuggestionStatus.Pending;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool IsFallback { get; set; }
    public int? Confidence { get; set; }
    public string GatewayCorrelationId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTimeOffset? ReviewedAt { get; set; }
    public string ReviewedByUserId { get; set; } = string.Empty;
    public string ReviewNotes { get; set; } = string.Empty;
    public string AppliedEntityType { get; set; } = string.Empty;
    public Guid? AppliedEntityId { get; set; }
}
```

- [ ] **Step 4: Map EF and migration**

Add `DbSet<RcaAiSuggestion> RcaAiSuggestions`.

Map table:

```csharp
modelBuilder.Entity<RcaAiSuggestion>(entity =>
{
    entity.ToTable("rca_ai_suggestions");
    entity.HasKey(x => x.Id);
    entity.Property(x => x.SuggestionType).HasConversion<string>().HasMaxLength(32);
    entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
    entity.Property(x => x.Title).HasMaxLength(300);
    entity.Property(x => x.Provider).HasMaxLength(100);
    entity.Property(x => x.Model).HasMaxLength(100);
    entity.Property(x => x.GatewayCorrelationId).HasMaxLength(200);
    entity.Property(x => x.CreatedByUserId).HasMaxLength(100);
    entity.Property(x => x.ReviewedByUserId).HasMaxLength(100);
    entity.Property(x => x.AppliedEntityType).HasMaxLength(100);
    entity.HasIndex(x => new { x.TenantId, x.IncidentId, x.Status });
    entity.HasIndex(x => new { x.TenantId, x.CreatedAt });
});
```

Run:

```powershell
dotnet build IshikawaRca.sln /m:1
dotnet ef migrations add AddRcaAiSuggestions --project src\IshikawaRca.Infrastructure\IshikawaRca.Infrastructure.csproj --startup-project src\IshikawaRca.Web\IshikawaRca.Web.csproj --no-build
```

- [ ] **Step 5: Validate and commit**

Run tests/build/diff-check. Commit:

```powershell
git add src tests docs
git commit -m "feat(ai): add RCA AI suggestion persistence"
```

---

### Task 5: Persist Pending Suggestions

**Files:**
- Create contract DTO: `RcaAiSuggestionDto.cs`
- Modify `RcaAiAssistantService.cs`
- Modify `IRcaAiAssistantService.cs`
- Modify EF service or create `IRcaAiSuggestionStore`/`EfRcaAiSuggestionStore` in Application/Infrastructure.
- Test: `tests/IshikawaRca.Tests/Program.cs`

- [ ] **Step 1: Write RED test**

Add a test using an in-memory fake suggestion store:

```csharp
await AssertAiAssistantPersistsPendingCauseSuggestionsAsync();
```

Expected behavior: after `SuggestCausesAsync`, the store has one pending `Cause` suggestion per returned cause.

- [ ] **Step 2: Implement store boundary**

Application interface:

```csharp
public interface IRcaAiSuggestionStore
{
    Task SavePendingAsync(Guid tenantId, Guid incidentId, RcaAiSuggestionType type, string title, string summary, object payload, RcaAiSuggestionMetadataDto metadata, string createdByUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RcaAiSuggestionDto>> ListAsync(Guid incidentId, string? status, CancellationToken cancellationToken = default);
}
```

Infrastructure store serializes payload JSON and writes `RcaAiSuggestion`.

- [ ] **Step 3: Call store after successful gateway result**

In `SuggestCausesAsync`, save each suggestion as `Cause`.

In `SuggestActionsAsync`, save each suggestion as `Action`.

In summary/recurrence/8D, save one `Summary`, `Recurrence`, or `EightD`.

Use `createdByUserId = "ai-request"` until UI/API user context is wired to request identity.

- [ ] **Step 4: Validate and commit**

Run tests/build/diff-check. Commit:

```powershell
git add src tests docs
git commit -m "feat(ai): persist pending RCA AI suggestions"
```

---

### Task 6: Accept and Reject Suggestion API

**Files:**
- Create contracts: `AcceptRcaAiSuggestionRequest.cs`, `RejectRcaAiSuggestionRequest.cs`
- Modify `IRcaAiAssistantService.cs`, `RcaAiAssistantService.cs`
- Modify `RcaAiController.cs`
- Modify `IRcaAiSuggestionStore` and EF implementation.
- Test: `tests/IshikawaRca.Tests/Program.cs`

- [ ] **Step 1: Write RED tests**

Add:

```csharp
await AssertAcceptingCauseSuggestionCreatesCauseAndAuditsAsync();
await AssertRejectingSuggestionDoesNotCreateOfficialEntityAsync();
```

The accept test should create an RCA with canvas, save a pending cause suggestion payload, call accept with `targetBranchId`, then assert a new cause exists and suggestion status is `Accepted`.

- [ ] **Step 2: Add request DTOs**

```csharp
public class AcceptRcaAiSuggestionRequest
{
    public string ReviewedByUserId { get; set; } = string.Empty;
    public string ReviewNotes { get; set; } = string.Empty;
    public Guid? TargetBranchId { get; set; }
}

public class RejectRcaAiSuggestionRequest
{
    public string ReviewedByUserId { get; set; } = string.Empty;
    public string ReviewNotes { get; set; } = string.Empty;
}
```

- [ ] **Step 3: Implement service behavior**

Accept:

- Load suggestion by incident and id.
- If not found, return `AI_SUGGESTION_NOT_FOUND`.
- If not pending, return `AI_SUGGESTION_NOT_PENDING`.
- If `Cause`, require `TargetBranchId`, deserialize `RcaAiCauseSuggestionDto`, call `AddCauseAsync`.
- If `Action`, deserialize `RcaAiActionSuggestionDto`, call `AddCorrectiveActionAsync`.
- Mark accepted with applied entity info.
- Write `RcaAuditRecord` through existing audit mechanism if available in EF service; if not, extend store to create audit record.

Reject:

- Load pending suggestion.
- Mark rejected with reviewed user/date/notes.
- Do not call RCA mutation services.
- Write audit record.

- [ ] **Step 4: Add API endpoints**

```csharp
[HttpGet("suggestions")]
public async Task<ActionResult<ApiResult<IReadOnlyList<RcaAiSuggestionDto>>>> ListSuggestions(Guid id, [FromQuery] string? status, CancellationToken cancellationToken)

[HttpPost("suggestions/{suggestionId:guid}/accept")]
[Authorize(Roles = RcaRoleNames.QualityGovernance)]
public async Task<ActionResult<ApiResult<RcaAiSuggestionDto>>> AcceptSuggestion(Guid id, Guid suggestionId, AcceptRcaAiSuggestionRequest request, CancellationToken cancellationToken)

[HttpPost("suggestions/{suggestionId:guid}/reject")]
[Authorize(Roles = RcaRoleNames.QualityGovernance)]
public async Task<ActionResult<ApiResult<RcaAiSuggestionDto>>> RejectSuggestion(Guid id, Guid suggestionId, RejectRcaAiSuggestionRequest request, CancellationToken cancellationToken)
```

- [ ] **Step 5: Validate and commit**

Run tests/build/diff-check. Commit:

```powershell
git add src tests docs
git commit -m "feat(ai): govern RCA AI suggestion review"
```

---

### Task 7: MVC AI Review Panel

**Files:**
- Modify `src/IshikawaRca.Web/Models/Rca/RcaIncidentDetailsViewModel.cs`
- Modify `src/IshikawaRca.Web/Controllers/RcaController.cs`
- Modify `src/IshikawaRca.Web/Views/Rca/Details.cshtml`
- Test: build and optional targeted smoke.

- [ ] **Step 1: Add view model fields**

Add:

```csharp
public IReadOnlyList<RcaAiSuggestionDto> AiSuggestions { get; set; } = [];
```

- [ ] **Step 2: Populate details**

In `RcaController.Details`, call `IRcaAiAssistantService.ListSuggestionsAsync(id, "Pending", cancellationToken)` and put results on the view model.

- [ ] **Step 3: Render compact panel**

Add a section near existing AI/timeline content:

```cshtml
<section class="ai-review-panel">
  @foreach (var suggestion in Model.AiSuggestions)
  {
      <article class="ai-suggestion-card">
          <h3>@suggestion.Title</h3>
          <p>@suggestion.Summary</p>
          <small>@suggestion.Provider / @suggestion.Model @(suggestion.IsFallback ? "Fallback" : "")</small>
          <form method="post" asp-action="AcceptAiSuggestion" asp-route-id="@Model.Incident.Id" asp-route-suggestionId="@suggestion.Id">
              <button type="submit">Aceptar</button>
          </form>
          <form method="post" asp-action="RejectAiSuggestion" asp-route-id="@Model.Incident.Id" asp-route-suggestionId="@suggestion.Id">
              <button type="submit">Rechazar</button>
          </form>
      </article>
  }
</section>
```

Use existing button classes/styles rather than introducing a new visual language.

- [ ] **Step 4: Add MVC post actions**

Add wrapper actions that call the same application service accept/reject methods and redirect back to Details.

- [ ] **Step 5: Validate and commit**

Run:

```powershell
dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj
dotnet build IshikawaRca.sln /m:1
git diff --check
```

Commit:

```powershell
git add src tests docs
git commit -m "feat(ui): add RCA AI suggestion review panel"
```

---

### Task 8: Documentation and P3 Closure

**Files:**
- Modify: `docs/AI_INTEGRATION.md`
- Modify: `docs/API_CONTRACTS.md`
- Modify: `docs/backend.md`
- Modify: `docs/chats/BACKEND.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/STATUS_AND_NEXT_STEPS.md`
- Modify: `docs/VALIDATION_LOG.md`

- [ ] **Step 1: Update docs**

Document:

- `AiGateway` options.
- HTTP/fallback behavior.
- New recurrence and 8D endpoints.
- Suggestion list/accept/reject endpoints.
- Human approval rule.
- P3 closure state.

- [ ] **Step 2: Validate**

Run:

```powershell
dotnet run --project tests\IshikawaRca.Tests\IshikawaRca.Tests.csproj
dotnet build IshikawaRca.sln /m:1
git diff --check
```

- [ ] **Step 3: Commit**

```powershell
git add docs
git commit -m "docs(ai): close P3 governance scope"
```

---

## Self-Review

- Spec coverage: HTTP client, fallback, recurrence, 8D, persistence, accept/reject, audit, UI and docs are covered by Tasks 1-8.
- Scope: one coherent P3 plan; later tenant-specific AI policy and production external gateway smoke remain outside this plan until Identity/tenant and real gateway exist.
- TDD: Tasks 1-6 require RED/GREEN tests before implementation. Task 7 is UI integration with build/smoke validation. Task 8 is documentation closure.
- No placeholders: every task has concrete file paths, commands and expected behavior.
