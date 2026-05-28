using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Entities;
using IshikawaRca.Domain.Enums;
using IshikawaRca.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IshikawaRca.Infrastructure.Services;

public class EfRcaIncidentService : IRcaIncidentService
{
    private readonly RcaDbContext _dbContext;

    public EfRcaIncidentService(RcaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResult<RcaIncidentDto>> CreateAsync(CreateRcaIncidentRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateCreateRequest(request);
        if (validationErrors.Count > 0)
        {
            return ApiResult<RcaIncidentDto>.Fail("No se pudo crear el incidente RCA.", validationErrors.ToArray());
        }

        var incident = new RcaIncident
        {
            TenantId = request.TenantId,
            Title = request.Title.Trim(),
            ProblemDescription = Normalize(request.ProblemDescription),
            Severity = ParseSeverity(request.Severity),
            Status = RcaIncidentStatus.Open,
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

        _dbContext.RcaIncidents.Add(incident);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<RcaIncidentDto>.Ok(ToDto(incident), "Incidente RCA creado.");
    }

    public async Task<ApiResult<IReadOnlyList<RcaIncidentDto>>> ListAsync(string? sourceSystem = null, string? externalTaskId = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RcaIncidents
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(sourceSystem))
        {
            query = query.Where(x => x.SourceSystem == sourceSystem.Trim());
        }

        if (!string.IsNullOrWhiteSpace(externalTaskId))
        {
            query = query.Where(x => x.ExternalTaskId == externalTaskId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RcaIncidentStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        var data = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

        return ApiResult<IReadOnlyList<RcaIncidentDto>>.Ok(data);
    }

    public async Task<ApiResult<RcaIncidentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var incident = await _dbContext.RcaIncidents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (incident is null)
        {
            return ApiResult<RcaIncidentDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(id), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
        }

        return ApiResult<RcaIncidentDto>.Ok(ToDto(incident));
    }

    public async Task<ApiResult<IshikawaCanvasDto>> GetCanvasAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var incident = await _dbContext.RcaIncidents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

        if (incident is null)
        {
            return NotFoundCanvas(incidentId);
        }

        var branches = await _dbContext.IshikawaBranches
            .AsNoTracking()
            .Where(x => x.RcaIncidentId == incidentId && !x.IsDeleted)
            .OrderBy(x => x.Order)
            .Select(x => ToBranchDto(x))
            .ToListAsync(cancellationToken);

        var causes = await _dbContext.IshikawaCauses
            .AsNoTracking()
            .Where(x => x.RcaIncidentId == incidentId && !x.IsDeleted)
            .OrderByDescending(x => x.IsRootCause)
            .ThenByDescending(x => x.ImpactScore + x.ProbabilityScore + x.FrequencyScore)
            .ThenBy(x => x.CreatedAt)
            .Select(x => ToCauseDto(x))
            .ToListAsync(cancellationToken);

        var canvas = new IshikawaCanvasDto
        {
            RcaIncidentId = incident.Id,
            ProblemTitle = incident.Title,
            Branches = branches,
            Causes = causes
        };

        return ApiResult<IshikawaCanvasDto>.Ok(canvas);
    }

    public async Task<ApiResult<IshikawaCauseDto>> AddCauseAsync(Guid incidentId, AddIshikawaCauseRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateAddCauseRequest(request);
        if (validationErrors.Count > 0)
        {
            return ApiResult<IshikawaCauseDto>.Fail("No se pudo agregar la causa.", validationErrors.ToArray());
        }

        var branch = await _dbContext.IshikawaBranches
            .FirstOrDefaultAsync(x => x.Id == request.BranchId && x.RcaIncidentId == incidentId && !x.IsDeleted, cancellationToken);

        if (branch is null)
        {
            return ApiResult<IshikawaCauseDto>.Fail(
                "No se pudo agregar la causa.",
                new ApiError { Field = nameof(request.BranchId), Code = "BRANCH_NOT_FOUND", Message = "La rama seleccionada no corresponde al incidente RCA." });
        }

        if (request.ParentCauseId.HasValue)
        {
            var parentExists = await _dbContext.IshikawaCauses
                .AnyAsync(x => x.Id == request.ParentCauseId.Value && x.RcaIncidentId == incidentId && !x.IsDeleted, cancellationToken);

            if (!parentExists)
            {
                return ApiResult<IshikawaCauseDto>.Fail(
                    "No se pudo agregar la causa.",
                    new ApiError { Field = nameof(request.ParentCauseId), Code = "PARENT_CAUSE_NOT_FOUND", Message = "La causa padre no corresponde al incidente RCA." });
            }
        }

        var cause = new IshikawaCause
        {
            TenantId = branch.TenantId,
            RcaIncidentId = incidentId,
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

        _dbContext.IshikawaCauses.Add(cause);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<IshikawaCauseDto>.Ok(ToCauseDto(cause), "Causa agregada.");
    }

    public async Task<ApiResult<IReadOnlyList<CorrectiveActionDto>>> ListCorrectiveActionsAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var incidentExists = await _dbContext.RcaIncidents
            .AsNoTracking()
            .AnyAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

        if (!incidentExists)
        {
            return ApiResult<IReadOnlyList<CorrectiveActionDto>>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
        }

        var actions = await _dbContext.CorrectiveActions
            .AsNoTracking()
            .Where(x => x.RcaIncidentId == incidentId && !x.IsDeleted)
            .OrderBy(x => x.Status)
            .ThenBy(x => x.DueDate)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => ToActionDto(x))
            .ToListAsync(cancellationToken);

        return ApiResult<IReadOnlyList<CorrectiveActionDto>>.Ok(actions);
    }

    public async Task<ApiResult<CorrectiveActionDto>> AddCorrectiveActionAsync(Guid incidentId, AddCorrectiveActionRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateAddCorrectiveActionRequest(request);
        if (validationErrors.Count > 0)
        {
            return ApiResult<CorrectiveActionDto>.Fail("No se pudo agregar la accion correctiva.", validationErrors.ToArray());
        }

        var incident = await _dbContext.RcaIncidents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

        if (incident is null)
        {
            return ApiResult<CorrectiveActionDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
        }

        if (request.CauseId.HasValue)
        {
            var causeExists = await _dbContext.IshikawaCauses
                .AnyAsync(x => x.Id == request.CauseId.Value && x.RcaIncidentId == incidentId && !x.IsDeleted, cancellationToken);

            if (!causeExists)
            {
                return ApiResult<CorrectiveActionDto>.Fail(
                    "No se pudo agregar la accion correctiva.",
                    new ApiError { Field = nameof(request.CauseId), Code = "CAUSE_NOT_FOUND", Message = "La causa seleccionada no corresponde al incidente RCA." });
            }
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

        _dbContext.CorrectiveActions.Add(action);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<CorrectiveActionDto>.Ok(ToActionDto(action), "Accion correctiva agregada.");
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

    private static ApiResult<IshikawaCanvasDto> NotFoundCanvas(Guid incidentId)
    {
        return ApiResult<IshikawaCanvasDto>.Fail(
            "No se encontro el incidente RCA.",
            new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
    }
}
