using IshikawaRca.Application.Ai;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;

namespace IshikawaRca.Infrastructure.Ai;

public class StubRcaAiGatewayClient : IRcaAiGatewayClient
{
    public Task<ApiResult<RcaAiCauseSuggestionResultDto>> SuggestCausesAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        var existingBranches = context.Canvas.Branches
            .OrderBy(x => x.Order)
            .Select(x => x.Name)
            .ToList();

        var branches = existingBranches.Count > 0
            ? existingBranches
            : ["Metodo", "Maquina", "Material"];

        var suggestions = branches.Take(3).Select((branch, index) => new RcaAiCauseSuggestionDto
        {
            BranchName = branch,
            Title = BuildCauseTitle(branch, context),
            Reasoning = $"Sugerencia stub basada en el problema, contexto industrial y causas ya cargadas para la rama {branch}.",
            ConfidenceScore = Math.Max(55, 78 - index * 8),
            SuggestedImpactScore = ClampScore(context.Incident.Severity is "High" or "Critical" ? 4 : 3),
            SuggestedProbabilityScore = ClampScore(3 - index / 2),
            SuggestedFrequencyScore = ClampScore(2 + index)
        }).ToList();

        var result = new RcaAiCauseSuggestionResultDto
        {
            IncidentId = context.Incident.Id,
            Summary = "Modo stub activo: propuestas generadas por reglas simples hasta conectar el AI Gateway compartido.",
            Suggestions = suggestions,
            Metadata = CreateMetadata()
        };

        return Task.FromResult(ApiResult<RcaAiCauseSuggestionResultDto>.Ok(result));
    }

    public Task<ApiResult<RcaAiActionSuggestionResultDto>> SuggestActionsAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        var rootCause = context.Canvas.Causes
            .Where(x => x.IsRootCause)
            .OrderByDescending(x => x.ImpactScore + x.ProbabilityScore + x.FrequencyScore)
            .FirstOrDefault();

        var suggestions = new List<RcaAiActionSuggestionDto>
        {
            new()
            {
                Title = "Validar causa raiz en piso",
                Description = "Revisar la condicion real con operador, mantenimiento y calidad antes de cerrar el analisis.",
                RelatedCauseTitle = rootCause?.Title,
                SuggestedOwnerRole = "Supervisor de turno",
                SuggestedDueDays = 1
            },
            new()
            {
                Title = "Definir contencion temporal",
                Description = "Implementar una accion inmediata para reducir recurrencia mientras se confirma la solucion definitiva.",
                RelatedCauseTitle = rootCause?.Title,
                SuggestedOwnerRole = "Produccion",
                SuggestedDueDays = 2
            },
            new()
            {
                Title = "Actualizar estandar operativo",
                Description = "Ajustar instruccion, checklist o plan de mantenimiento segun la causa confirmada.",
                RelatedCauseTitle = rootCause?.Title,
                SuggestedOwnerRole = "Ingenieria de procesos",
                SuggestedDueDays = 7
            }
        };

        var result = new RcaAiActionSuggestionResultDto
        {
            IncidentId = context.Incident.Id,
            Summary = "Modo stub activo: acciones propuestas para soportar el flujo RCA sin ejecutar cambios automaticos.",
            Suggestions = suggestions,
            Metadata = CreateMetadata()
        };

        return Task.FromResult(ApiResult<RcaAiActionSuggestionResultDto>.Ok(result));
    }

    public Task<ApiResult<RcaAiSummaryResultDto>> SummarizeAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        var rootCause = context.Canvas.Causes.FirstOrDefault(x => x.IsRootCause);
        var openActions = context.CorrectiveActions.Count(x => x.Status is not "Completed" and not "Cancelled");

        var result = new RcaAiSummaryResultDto
        {
            IncidentId = context.Incident.Id,
            ExecutiveSummary = $"{context.Incident.Title}. Estado {context.Incident.Status}, severidad {context.Incident.Severity}, {context.Canvas.Causes.Count} causa(s) y {openActions} accion(es) abierta(s).",
            RiskAssessment = rootCause is null
                ? "Riesgo pendiente de clasificar: aun no hay causa raiz marcada."
                : $"Riesgo asociado a causa raiz propuesta: {rootCause.Title}.",
            RecommendedNextSteps =
            [
                "Confirmar evidencia en piso antes de cerrar la causa raiz.",
                "Asignar responsable y fecha a toda accion correctiva abierta.",
                "Publicar snapshot de integracion para que Gantt o la app global reflejen el estado RCA."
            ],
            Metadata = CreateMetadata()
        };

        return Task.FromResult(ApiResult<RcaAiSummaryResultDto>.Ok(result));
    }

    public Task<ApiResult<RcaAiRecurrenceResultDto>> DetectRecurrenceAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResult<RcaAiRecurrenceResultDto>.Ok(new RcaAiRecurrenceResultDto
        {
            IncidentId = context.Incident.Id,
            IsLikelyRecurring = context.Canvas.Causes.Count > 2,
            ConfidenceScore = 62,
            Rationale = "Modo stub: recurrencia estimada por cantidad de causas y acciones abiertas.",
            SimilarSignals = ["Misma linea o maquina", "Acciones abiertas"],
            Metadata = CreateMetadata()
        }));
    }

    public Task<ApiResult<RcaAiEightDDraftResultDto>> GenerateEightDDraftAsync(RcaAiContextDto context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApiResult<RcaAiEightDDraftResultDto>.Ok(new RcaAiEightDDraftResultDto
        {
            IncidentId = context.Incident.Id,
            ProblemStatement = context.Incident.ProblemDescription ?? string.Empty,
            ContainmentActions = "Definir contencion temporal y responsable.",
            RootCauseAnalysis = "Completar causa raiz con evidencia validada.",
            CorrectiveActions = "Convertir acciones RCA aceptadas en plan 8D.",
            VerificationPlan = "Verificar eficacia antes de cierre formal.",
            Metadata = CreateMetadata()
        }));
    }

    private static string BuildCauseTitle(string branch, RcaAiContextDto context)
    {
        return branch switch
        {
            "Maquina" => $"Verificar condicion de {context.Incident.MachineCode ?? "equipo"}",
            "Metodo" => "Revisar secuencia real contra estandar operativo",
            "Material" => "Validar variacion o condicion del material usado",
            "Mano de obra" => "Confirmar entrenamiento y cambio de turno",
            "Medicion" => "Revisar criterio de medicion y datos disponibles",
            "Medio ambiente" => "Evaluar condicion ambiental o del entorno",
            _ => $"Analizar factor asociado a {branch}"
        };
    }

    private static int ClampScore(int value)
    {
        return Math.Min(5, Math.Max(0, value));
    }

    private static RcaAiSuggestionMetadataDto CreateMetadata()
    {
        return new RcaAiSuggestionMetadataDto
        {
            Provider = "IshikawaRca.Stub",
            Model = "rules-v1",
            IsFallback = true,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }
}
