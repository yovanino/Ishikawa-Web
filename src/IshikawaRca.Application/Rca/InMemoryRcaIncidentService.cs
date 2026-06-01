using System.Collections.Concurrent;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Entities;
using IshikawaRca.Domain.Enums;

namespace IshikawaRca.Application.Rca;

public class InMemoryRcaIncidentService : IRcaIncidentService
{
    private readonly ConcurrentDictionary<Guid, RcaIncident> _incidents = new();

    public Task<ApiResult<RcaIncidentDto>> CreateAsync(CreateRcaIncidentRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateCreateRequest(request);
        if (validationErrors.Count > 0)
        {
            return Task.FromResult(ApiResult<RcaIncidentDto>.Fail("No se pudo crear el incidente RCA.", validationErrors.ToArray()));
        }

        var incident = new RcaIncident
        {
            TenantId = request.TenantId,
            Title = request.Title.Trim(),
            ProblemDescription = Normalize(request.ProblemDescription),
            Severity = ParseSeverity(request.Severity),
            Status = RcaIncidentStatus.Open,
            ClaimActorType = ResolveClaimActorType(request),
            ClaimScope = ResolveClaimScope(request),
            ClaimOwnerName = Normalize(request.ClaimOwnerName),
            OccurredAt = request.OccurredAt,
            SourceSystem = Normalize(request.SourceSystem) ?? "MANUAL",
            ExternalTaskId = Normalize(request.ExternalTaskId),
            ExternalEventId = Normalize(request.ExternalEventId),
            ExternalWorkOrderId = Normalize(request.ExternalWorkOrderId),
            MachineCode = Normalize(request.MachineCode),
            LineCode = Normalize(request.LineCode),
            WorkOrderCode = Normalize(request.WorkOrderCode),
            ReportedBy = Normalize(request.ReportedBy),
            TaskSnapshotJson = Normalize(request.TaskSnapshotJson),
            ContextSnapshotJson = Normalize(request.ContextSnapshotJson)
        };

        AddDefaultBranches(incident);

        _incidents[incident.Id] = incident;

        return Task.FromResult(ApiResult<RcaIncidentDto>.Ok(ToDto(incident), "Incidente RCA creado."));
    }

    public Task<ApiResult<IReadOnlyList<RcaIncidentDto>>> ListAsync(string? sourceSystem = null, string? externalTaskId = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _incidents.Values.Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(sourceSystem))
        {
            query = query.Where(x => string.Equals(x.SourceSystem, sourceSystem.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(externalTaskId))
        {
            query = query.Where(x => string.Equals(x.ExternalTaskId, externalTaskId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RcaIncidentStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        var data = query
            .OrderByDescending(x => x.CreatedAt)
            .Select(ToDto)
            .ToList();

        return Task.FromResult(ApiResult<IReadOnlyList<RcaIncidentDto>>.Ok(data));
    }

    public Task<ApiResult<RcaIncidentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_incidents.TryGetValue(id, out var incident) || incident.IsDeleted)
        {
            return Task.FromResult(ApiResult<RcaIncidentDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(id), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." }));
        }

        return Task.FromResult(ApiResult<RcaIncidentDto>.Ok(ToDto(incident)));
    }

    public Task<ApiResult<IshikawaCanvasDto>> GetCanvasAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        if (!_incidents.TryGetValue(incidentId, out var incident) || incident.IsDeleted)
        {
            return Task.FromResult(ApiResult<IshikawaCanvasDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." }));
        }

        var canvas = new IshikawaCanvasDto
        {
            RcaIncidentId = incident.Id,
            ProblemTitle = incident.Title,
            Branches = incident.Branches.OrderBy(x => x.Order).Select(ToBranchDto).ToList(),
            Causes = incident.Branches
                .SelectMany(x => x.Causes)
                .OrderByDescending(x => x.IsRootCause)
                .ThenByDescending(x => x.ImpactScore + x.ProbabilityScore + x.FrequencyScore)
                .ThenBy(x => x.CreatedAt)
                .Select(ToCauseDto)
                .ToList()
        };

        return Task.FromResult(ApiResult<IshikawaCanvasDto>.Ok(canvas));
    }

    public Task<ApiResult<IshikawaCauseDto>> AddCauseAsync(Guid incidentId, AddIshikawaCauseRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateAddCauseRequest(request);
        if (validationErrors.Count > 0)
        {
            return Task.FromResult(ApiResult<IshikawaCauseDto>.Fail("No se pudo agregar la causa.", validationErrors.ToArray()));
        }

        if (!_incidents.TryGetValue(incidentId, out var incident) || incident.IsDeleted)
        {
            return Task.FromResult(ApiResult<IshikawaCauseDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." }));
        }

        var branch = incident.Branches.FirstOrDefault(x => x.Id == request.BranchId && !x.IsDeleted);
        if (branch is null)
        {
            return Task.FromResult(ApiResult<IshikawaCauseDto>.Fail(
                "No se pudo agregar la causa.",
                new ApiError { Field = nameof(request.BranchId), Code = "BRANCH_NOT_FOUND", Message = "La rama seleccionada no corresponde al incidente RCA." }));
        }

        var cause = new IshikawaCause
        {
            TenantId = incident.TenantId,
            RcaIncidentId = incident.Id,
            BranchId = branch.Id,
            ParentCauseId = request.ParentCauseId,
            Title = request.Title.Trim(),
            Description = Normalize(request.Description),
            X = request.X,
            Y = request.Y,
            ProbabilityScore = request.ProbabilityScore,
            ImpactScore = request.ImpactScore,
            FrequencyScore = request.FrequencyScore,
            IsRootCause = request.IsRootCause,
            EvidenceSummary = Normalize(request.EvidenceSummary)
        };

        branch.Causes.Add(cause);

        return Task.FromResult(ApiResult<IshikawaCauseDto>.Ok(ToCauseDto(cause), "Causa agregada."));
    }

    public Task<ApiResult<IReadOnlyList<CorrectiveActionDto>>> ListCorrectiveActionsAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        if (!_incidents.TryGetValue(incidentId, out var incident) || incident.IsDeleted)
        {
            return Task.FromResult(ApiResult<IReadOnlyList<CorrectiveActionDto>>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." }));
        }

        var actions = incident.CorrectiveActions
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Status)
            .ThenBy(x => x.DueDate)
            .ThenByDescending(x => x.CreatedAt)
            .Select(ToActionDto)
            .ToList();

        return Task.FromResult(ApiResult<IReadOnlyList<CorrectiveActionDto>>.Ok(actions));
    }

    public Task<ApiResult<CorrectiveActionDto>> AddCorrectiveActionAsync(Guid incidentId, AddCorrectiveActionRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateAddCorrectiveActionRequest(request);
        if (validationErrors.Count > 0)
        {
            return Task.FromResult(ApiResult<CorrectiveActionDto>.Fail("No se pudo agregar la accion correctiva.", validationErrors.ToArray()));
        }

        if (!_incidents.TryGetValue(incidentId, out var incident) || incident.IsDeleted)
        {
            return Task.FromResult(ApiResult<CorrectiveActionDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." }));
        }

        var action = new CorrectiveAction
        {
            TenantId = incident.TenantId,
            RcaIncidentId = incident.Id,
            CauseId = request.CauseId,
            Title = request.Title.Trim(),
            Description = Normalize(request.Description),
            AssignedToUserId = Normalize(request.AssignedToUserId),
            DueDate = request.DueDate,
            Status = CorrectiveActionStatus.Open
        };

        incident.CorrectiveActions.Add(action);

        return Task.FromResult(ApiResult<CorrectiveActionDto>.Ok(ToActionDto(action), "Accion correctiva agregada."));
    }

    public Task<ApiResult<RcaIntegrationSnapshotDto>> GetIntegrationSnapshotAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        if (!_incidents.TryGetValue(incidentId, out var incident) || incident.IsDeleted)
        {
            return Task.FromResult(ApiResult<RcaIntegrationSnapshotDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." }));
        }

        return Task.FromResult(ApiResult<RcaIntegrationSnapshotDto>.Ok(ToIntegrationSnapshot(incident)));
    }

    public Task<ApiResult<IReadOnlyList<RcaIntegrationSnapshotDto>>> ListIntegrationSnapshotsAsync(string? sourceSystem = null, string? externalTaskId = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _incidents.Values.Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(sourceSystem))
        {
            query = query.Where(x => string.Equals(x.SourceSystem, sourceSystem.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(externalTaskId))
        {
            query = query.Where(x => string.Equals(x.ExternalTaskId, externalTaskId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RcaIncidentStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        var snapshots = query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Select(ToIntegrationSnapshot)
            .ToList();

        return Task.FromResult(ApiResult<IReadOnlyList<RcaIntegrationSnapshotDto>>.Ok(snapshots));
    }

    public Task<ApiResult<IReadOnlyList<RcaDomainEventDto>>> ListIntegrationEventsAsync(Guid? incidentId = null, DateTimeOffset? since = null, CancellationToken cancellationToken = default)
    {
        var events = _incidents.Values
            .Where(x => !x.IsDeleted && (!incidentId.HasValue || x.Id == incidentId.Value))
            .SelectMany(ToIntegrationEvents)
            .Where(x => !since.HasValue || x.OccurredAt >= since.Value)
            .OrderBy(x => x.OccurredAt)
            .ToList();

        return Task.FromResult(ApiResult<IReadOnlyList<RcaDomainEventDto>>.Ok(events));
    }

    private static List<ApiError> ValidateCreateRequest(CreateRcaIncidentRequest request)
    {
        var errors = new List<ApiError>();

        if (request.TenantId == Guid.Empty)
        {
            errors.Add(new ApiError { Field = nameof(request.TenantId), Code = "TENANT_REQUIRED", Message = "TenantId es obligatorio." });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add(new ApiError { Field = nameof(request.Title), Code = "TITLE_REQUIRED", Message = "El titulo del problema es obligatorio." });
        }

        if (!Enum.TryParse<RcaSeverity>(request.Severity, true, out _))
        {
            errors.Add(new ApiError { Field = nameof(request.Severity), Code = "INVALID_SEVERITY", Message = "Severity debe ser Low, Medium, High o Critical." });
        }

        if (!Enum.TryParse<RcaClaimScope>(request.ClaimScope, true, out _))
        {
            errors.Add(new ApiError { Field = nameof(request.ClaimScope), Code = "INVALID_CLAIM_SCOPE", Message = "ClaimScope debe ser Internal o External." });
        }

        if (!string.IsNullOrWhiteSpace(request.ClaimActorType) && !Enum.TryParse<RcaClaimActorType>(request.ClaimActorType, true, out _))
        {
            errors.Add(new ApiError { Field = nameof(request.ClaimActorType), Code = "INVALID_CLAIM_ACTOR_TYPE", Message = "ClaimActorType debe ser InternalArea, Customer o Supplier." });
        }

        return errors;
    }

    private static List<ApiError> ValidateAddCauseRequest(AddIshikawaCauseRequest request)
    {
        var errors = new List<ApiError>();

        if (request.BranchId == Guid.Empty)
        {
            errors.Add(new ApiError { Field = nameof(request.BranchId), Code = "BRANCH_REQUIRED", Message = "La rama es obligatoria." });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add(new ApiError { Field = nameof(request.Title), Code = "CAUSE_TITLE_REQUIRED", Message = "El titulo de la causa es obligatorio." });
        }

        ValidateScore(request.ProbabilityScore, nameof(request.ProbabilityScore), "probabilidad", errors);
        ValidateScore(request.ImpactScore, nameof(request.ImpactScore), "impacto", errors);
        ValidateScore(request.FrequencyScore, nameof(request.FrequencyScore), "frecuencia", errors);

        return errors;
    }

    private static List<ApiError> ValidateAddCorrectiveActionRequest(AddCorrectiveActionRequest request)
    {
        var errors = new List<ApiError>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add(new ApiError { Field = nameof(request.Title), Code = "ACTION_TITLE_REQUIRED", Message = "El titulo de la accion es obligatorio." });
        }

        return errors;
    }

    private static void ValidateScore(int value, string field, string label, List<ApiError> errors)
    {
        if (value is < 0 or > 5)
        {
            errors.Add(new ApiError { Field = field, Code = "INVALID_SCORE", Message = $"El puntaje de {label} debe estar entre 0 y 5." });
        }
    }

    private static RcaSeverity ParseSeverity(string severity)
    {
        return Enum.TryParse<RcaSeverity>(severity, true, out var parsed)
            ? parsed
            : RcaSeverity.Medium;
    }

    private static RcaClaimScope ResolveClaimScope(CreateRcaIncidentRequest request)
    {
        var actorType = ResolveClaimActorType(request);

        return actorType == RcaClaimActorType.InternalArea
            ? RcaClaimScope.Internal
            : RcaClaimScope.External;
    }

    private static RcaClaimActorType ResolveClaimActorType(CreateRcaIncidentRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.ClaimActorType) &&
            Enum.TryParse<RcaClaimActorType>(request.ClaimActorType, true, out var parsedActorType))
        {
            return parsedActorType;
        }

        return Enum.TryParse<RcaClaimScope>(request.ClaimScope, true, out var parsedScope) &&
               parsedScope == RcaClaimScope.External
            ? RcaClaimActorType.Customer
            : RcaClaimActorType.InternalArea;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void AddDefaultBranches(RcaIncident incident)
    {
        var branches = new[]
        {
            "Metodo",
            "Maquina",
            "Material",
            "Mano de obra",
            "Medicion",
            "Medio ambiente"
        };

        for (var i = 0; i < branches.Length; i++)
        {
            incident.Branches.Add(new IshikawaBranch
            {
                TenantId = incident.TenantId,
                RcaIncidentId = incident.Id,
                Name = branches[i],
                Order = i + 1
            });
        }
    }

    private static RcaIncidentDto ToDto(RcaIncident incident)
    {
        return new RcaIncidentDto
        {
            Id = incident.Id,
            TenantId = incident.TenantId,
            Title = incident.Title,
            ProblemDescription = incident.ProblemDescription,
            Severity = incident.Severity.ToString(),
            Status = incident.Status.ToString(),
            ClaimScope = incident.ClaimScope.ToString(),
            ClaimActorType = incident.ClaimActorType.ToString(),
            ClaimOwnerName = incident.ClaimOwnerName,
            OccurredAt = incident.OccurredAt,
            CreatedAt = incident.CreatedAt,
            ClosedAt = incident.ClosedAt,
            SourceSystem = incident.SourceSystem,
            ExternalTaskId = incident.ExternalTaskId,
            ExternalEventId = incident.ExternalEventId,
            ExternalWorkOrderId = incident.ExternalWorkOrderId,
            MachineCode = incident.MachineCode,
            LineCode = incident.LineCode,
            WorkOrderCode = incident.WorkOrderCode,
            EscalatedTo8D = incident.EscalatedTo8D
        };
    }

    private static IshikawaBranchDto ToBranchDto(IshikawaBranch branch)
    {
        return new IshikawaBranchDto
        {
            Id = branch.Id,
            Name = branch.Name,
            Description = branch.Description,
            Order = branch.Order,
            Color = branch.Color
        };
    }

    private static IshikawaCauseDto ToCauseDto(IshikawaCause cause)
    {
        return new IshikawaCauseDto
        {
            Id = cause.Id,
            BranchId = cause.BranchId,
            ParentCauseId = cause.ParentCauseId,
            Title = cause.Title,
            Description = cause.Description,
            X = cause.X,
            Y = cause.Y,
            ProbabilityScore = cause.ProbabilityScore,
            ImpactScore = cause.ImpactScore,
            FrequencyScore = cause.FrequencyScore,
            IsRootCause = cause.IsRootCause,
            EvidenceSummary = cause.EvidenceSummary
        };
    }

    private static CorrectiveActionDto ToActionDto(CorrectiveAction action)
    {
        return new CorrectiveActionDto
        {
            Id = action.Id,
            RcaIncidentId = action.RcaIncidentId,
            CauseId = action.CauseId,
            Title = action.Title,
            Description = action.Description,
            Status = action.Status.ToString(),
            AssignedToUserId = action.AssignedToUserId,
            DueDate = action.DueDate,
            CompletedAt = action.CompletedAt
        };
    }

    private static RcaIntegrationSnapshotDto ToIntegrationSnapshot(RcaIncident incident)
    {
        var causes = incident.Branches
            .SelectMany(x => x.Causes)
            .Where(x => !x.IsDeleted)
            .ToList();

        var rootCause = causes
            .Where(x => x.IsRootCause)
            .OrderByDescending(x => x.ImpactScore + x.ProbabilityScore + x.FrequencyScore)
            .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .FirstOrDefault();

        var openActions = incident.CorrectiveActions
            .Where(x => !x.IsDeleted && x.Status is not CorrectiveActionStatus.Completed and not CorrectiveActionStatus.Cancelled)
            .OrderBy(x => x.DueDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToList();

        var now = DateTimeOffset.UtcNow;

        return new RcaIntegrationSnapshotDto
        {
            IncidentId = incident.Id,
            TenantId = incident.TenantId,
            Title = incident.Title,
            Status = incident.Status.ToString(),
            Severity = incident.Severity.ToString(),
            ClaimScope = incident.ClaimScope.ToString(),
            ClaimActorType = incident.ClaimActorType.ToString(),
            ClaimOwnerName = incident.ClaimOwnerName,
            OccurredAt = incident.OccurredAt,
            CreatedAt = incident.CreatedAt,
            ClosedAt = incident.ClosedAt,
            LastUpdatedAt = GetLastUpdatedAt(incident, causes, incident.CorrectiveActions),
            SourceSystem = incident.SourceSystem,
            ExternalTaskId = incident.ExternalTaskId,
            ExternalEventId = incident.ExternalEventId,
            ExternalWorkOrderId = incident.ExternalWorkOrderId,
            MachineCode = incident.MachineCode,
            LineCode = incident.LineCode,
            WorkOrderCode = incident.WorkOrderCode,
            EscalatedTo8D = incident.EscalatedTo8D,
            RootCauseTitle = rootCause?.Title,
            RootCauseEvidenceSummary = rootCause?.EvidenceSummary,
            CauseCount = causes.Count,
            OpenCorrectiveActionsCount = openActions.Count,
            OverdueCorrectiveActionsCount = openActions.Count(x => x.DueDate.HasValue && x.DueDate.Value < now),
            NextActionDueAt = openActions.FirstOrDefault(x => x.DueDate.HasValue)?.DueDate,
            OpenActions = openActions.Select(ToIntegrationActionDto).ToList()
        };
    }

    private static RcaIntegrationActionDto ToIntegrationActionDto(CorrectiveAction action)
    {
        return new RcaIntegrationActionDto
        {
            Id = action.Id,
            CauseId = action.CauseId,
            Title = action.Title,
            Status = action.Status.ToString(),
            AssignedToUserId = action.AssignedToUserId,
            DueDate = action.DueDate
        };
    }

    private static IReadOnlyList<RcaDomainEventDto> ToIntegrationEvents(RcaIncident incident)
    {
        var events = new List<RcaDomainEventDto>
        {
            CreateEvent(
                $"rca-incident-created:{incident.Id}",
                "RcaIncidentCreated",
                incident.CreatedAt,
                incident,
                new Dictionary<string, string?>
                {
                    ["title"] = incident.Title,
                    ["severity"] = incident.Severity.ToString(),
                    ["status"] = incident.Status.ToString(),
                    ["claimScope"] = incident.ClaimScope.ToString(),
                    ["claimActorType"] = incident.ClaimActorType.ToString(),
                    ["claimOwnerName"] = incident.ClaimOwnerName
                })
        };

        foreach (var cause in incident.Branches.SelectMany(x => x.Causes).Where(x => !x.IsDeleted))
        {
            events.Add(CreateEvent(
                $"rca-cause-created:{cause.Id}",
                cause.IsRootCause ? "RcaRootCauseSelected" : "RcaCauseCreated",
                cause.CreatedAt,
                incident,
                new Dictionary<string, string?>
                {
                    ["causeId"] = cause.Id.ToString(),
                    ["branchId"] = cause.BranchId.ToString(),
                    ["title"] = cause.Title,
                    ["isRootCause"] = cause.IsRootCause.ToString()
                }));
        }

        foreach (var action in incident.CorrectiveActions.Where(x => !x.IsDeleted))
        {
            events.Add(CreateEvent(
                $"rca-action-created:{action.Id}",
                "RcaCorrectiveActionCreated",
                action.CreatedAt,
                incident,
                new Dictionary<string, string?>
                {
                    ["actionId"] = action.Id.ToString(),
                    ["causeId"] = action.CauseId?.ToString(),
                    ["title"] = action.Title,
                    ["status"] = action.Status.ToString(),
                    ["dueDate"] = action.DueDate?.ToString("O")
                }));
        }

        return events;
    }

    private static RcaDomainEventDto CreateEvent(string id, string type, DateTimeOffset occurredAt, RcaIncident incident, Dictionary<string, string?> data)
    {
        return new RcaDomainEventDto
        {
            Id = id,
            Type = type,
            OccurredAt = occurredAt,
            IncidentId = incident.Id,
            TenantId = incident.TenantId,
            SourceSystem = incident.SourceSystem,
            ExternalTaskId = incident.ExternalTaskId,
            ExternalEventId = incident.ExternalEventId,
            ExternalWorkOrderId = incident.ExternalWorkOrderId,
            Data = data
        };
    }

    private static DateTimeOffset GetLastUpdatedAt(RcaIncident incident, IReadOnlyList<IshikawaCause> causes, IEnumerable<CorrectiveAction> actions)
    {
        return new[]
            {
                incident.UpdatedAt ?? incident.CreatedAt,
                causes.Select(x => x.UpdatedAt ?? x.CreatedAt).DefaultIfEmpty(incident.CreatedAt).Max(),
                actions.Select(x => x.UpdatedAt ?? x.CreatedAt).DefaultIfEmpty(incident.CreatedAt).Max()
            }
            .Max();
    }
}
