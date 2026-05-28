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
}
