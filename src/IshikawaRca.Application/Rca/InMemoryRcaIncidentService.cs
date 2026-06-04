using System.Collections.Concurrent;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Entities;
using IshikawaRca.Domain.Enums;

namespace IshikawaRca.Application.Rca;

public class InMemoryRcaIncidentService : IRcaIncidentService
{
    private static readonly HashSet<string> EvidenceValidationStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "PendingReview",
        "Validated",
        "Rejected",
        "Expired"
    };

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

    public Task<ApiResult<CorrectiveActionDto>> UpdateCorrectiveActionStatusAsync(Guid incidentId, Guid actionId, UpdateCorrectiveActionStatusRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateUpdateCorrectiveActionStatusRequest(request);
        if (validationErrors.Count > 0)
        {
            return Task.FromResult(ApiResult<CorrectiveActionDto>.Fail("No se pudo actualizar la accion correctiva.", validationErrors.ToArray()));
        }

        if (!_incidents.TryGetValue(incidentId, out var incident) || incident.IsDeleted)
        {
            return Task.FromResult(ApiResult<CorrectiveActionDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." }));
        }

        var action = incident.CorrectiveActions.FirstOrDefault(x => x.Id == actionId && !x.IsDeleted);
        if (action is null)
        {
            return Task.FromResult(ApiResult<CorrectiveActionDto>.Fail(
                "No se encontro la accion correctiva.",
                new ApiError { Field = nameof(actionId), Code = "ACTION_NOT_FOUND", Message = "La accion seleccionada no corresponde al incidente RCA." }));
        }

        var status = ParseCorrectiveActionStatus(request.Status);
        var now = DateTimeOffset.UtcNow;

        action.Status = status;
        action.ValidationNotes = Normalize(request.ValidationNotes);
        action.UpdatedAt = now;
        action.UpdatedByUserId = Normalize(request.CompletedByUserId);

        if (status == CorrectiveActionStatus.Completed)
        {
            action.CompletedAt ??= now;
            action.CompletedByUserId = Normalize(request.CompletedByUserId);
        }
        else
        {
            action.CompletedAt = null;
            action.CompletedByUserId = null;
        }

        return Task.FromResult(ApiResult<CorrectiveActionDto>.Ok(ToActionDto(action), "Estado de accion actualizado."));
    }

    public Task<ApiResult<IReadOnlyList<RcaEvidenceDto>>> ListEvidenceAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        if (!_incidents.TryGetValue(incidentId, out var incident) || incident.IsDeleted)
        {
            return Task.FromResult(ApiResult<IReadOnlyList<RcaEvidenceDto>>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." }));
        }

        var evidence = incident.Evidence
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CapturedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(ToEvidenceDto)
            .ToList();

        return Task.FromResult(ApiResult<IReadOnlyList<RcaEvidenceDto>>.Ok(evidence));
    }

    public Task<ApiResult<RcaEvidenceDto>> AddEvidenceAsync(Guid incidentId, AddRcaEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateAddEvidenceRequest(request);
        if (validationErrors.Count > 0)
        {
            return Task.FromResult(ApiResult<RcaEvidenceDto>.Fail("No se pudo agregar la evidencia.", validationErrors.ToArray()));
        }

        if (!_incidents.TryGetValue(incidentId, out var incident) || incident.IsDeleted)
        {
            return Task.FromResult(ApiResult<RcaEvidenceDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." }));
        }

        var evidence = new RcaEvidence
        {
            TenantId = incident.TenantId,
            RcaIncidentId = incident.Id,
            CauseId = request.CauseId,
            ExternalIntakeId = request.ExternalIntakeId,
            Title = request.Title.Trim(),
            EvidenceType = Normalize(request.EvidenceType) ?? "Observation",
            Source = Normalize(request.Source) ?? "Manual",
            SourceDetail = Normalize(request.SourceDetail),
            Tags = NormalizeTags(request.Tags),
            Summary = Normalize(request.Summary),
            ReferenceUri = Normalize(request.ReferenceUri),
            AttachmentFileName = Normalize(request.AttachmentFileName),
            AttachmentContentType = Normalize(request.AttachmentContentType),
            AttachmentSizeBytes = request.AttachmentSizeBytes,
            AttachmentStorageProvider = Normalize(request.AttachmentStorageProvider),
            AttachmentStorageKey = Normalize(request.AttachmentStorageKey),
            AttachmentSha256 = Normalize(request.AttachmentSha256),
            CapturedAt = request.CapturedAt ?? DateTimeOffset.UtcNow,
            CapturedByUserId = Normalize(request.CapturedByUserId),
            ValidationStatus = NormalizeValidationStatus(request.ValidationStatus),
            ValidatedAt = ResolveEvidenceValidatedAt(request.ValidationStatus, request.ValidatedAt),
            ValidatedByUserId = Normalize(request.ValidatedByUserId),
            ValidationNotes = Normalize(request.ValidationNotes)
        };

        incident.Evidence.Add(evidence);

        return Task.FromResult(ApiResult<RcaEvidenceDto>.Ok(ToEvidenceDto(evidence), "Evidencia agregada."));
    }

    public Task<ApiResult<RcaEvidenceDto>> UpdateEvidenceAsync(Guid incidentId, Guid evidenceId, UpdateRcaEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateUpdateEvidenceRequest(request);
        if (validationErrors.Count > 0)
        {
            return Task.FromResult(ApiResult<RcaEvidenceDto>.Fail("No se pudo actualizar la evidencia.", validationErrors.ToArray()));
        }

        if (!_incidents.TryGetValue(incidentId, out var incident) || incident.IsDeleted)
        {
            return Task.FromResult(ApiResult<RcaEvidenceDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." }));
        }

        var evidence = incident.Evidence.FirstOrDefault(x => x.Id == evidenceId && !x.IsDeleted);
        if (evidence is null)
        {
            return Task.FromResult(ApiResult<RcaEvidenceDto>.Fail(
                "No se encontro la evidencia RCA.",
                new ApiError { Field = nameof(evidenceId), Code = "EVIDENCE_NOT_FOUND", Message = "La evidencia no corresponde al incidente RCA." }));
        }

        evidence.CauseId = request.CauseId;
        evidence.Title = request.Title.Trim();
        evidence.EvidenceType = Normalize(request.EvidenceType) ?? "Observation";
        evidence.Source = Normalize(request.Source) ?? "Manual";
        evidence.SourceDetail = Normalize(request.SourceDetail);
        evidence.Tags = NormalizeTags(request.Tags);
        evidence.Summary = Normalize(request.Summary);
        evidence.ReferenceUri = Normalize(request.ReferenceUri);
        evidence.CapturedAt = request.CapturedAt ?? evidence.CapturedAt;
        evidence.CapturedByUserId = Normalize(request.CapturedByUserId);
        evidence.ValidationStatus = NormalizeValidationStatus(request.ValidationStatus);
        evidence.ValidatedAt = ResolveEvidenceValidatedAt(request.ValidationStatus, request.ValidatedAt);
        evidence.ValidatedByUserId = Normalize(request.ValidatedByUserId);
        evidence.ValidationNotes = Normalize(request.ValidationNotes);
        evidence.UpdatedAt = DateTimeOffset.UtcNow;

        return Task.FromResult(ApiResult<RcaEvidenceDto>.Ok(ToEvidenceDto(evidence), "Evidencia actualizada."));
    }

    public Task<ApiResult<RcaEvidenceDto>> ReplaceEvidenceAttachmentAsync(Guid incidentId, Guid evidenceId, ReplaceRcaEvidenceAttachmentRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateReplaceEvidenceAttachmentRequest(request);
        if (validationErrors.Count > 0)
        {
            return Task.FromResult(ApiResult<RcaEvidenceDto>.Fail("No se pudo reemplazar el adjunto.", validationErrors.ToArray()));
        }

        if (!_incidents.TryGetValue(incidentId, out var incident) || incident.IsDeleted)
        {
            return Task.FromResult(ApiResult<RcaEvidenceDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." }));
        }

        var evidence = incident.Evidence.FirstOrDefault(x => x.Id == evidenceId && !x.IsDeleted);
        if (evidence is null)
        {
            return Task.FromResult(ApiResult<RcaEvidenceDto>.Fail(
                "No se encontro la evidencia RCA.",
                new ApiError { Field = nameof(evidenceId), Code = "EVIDENCE_NOT_FOUND", Message = "La evidencia no corresponde al incidente RCA." }));
        }

        evidence.AttachmentFileName = Normalize(request.AttachmentFileName);
        evidence.AttachmentContentType = Normalize(request.AttachmentContentType);
        evidence.AttachmentSizeBytes = request.AttachmentSizeBytes;
        evidence.AttachmentStorageProvider = Normalize(request.AttachmentStorageProvider);
        evidence.AttachmentStorageKey = Normalize(request.AttachmentStorageKey);
        evidence.AttachmentSha256 = Normalize(request.AttachmentSha256);
        evidence.UpdatedAt = DateTimeOffset.UtcNow;

        return Task.FromResult(ApiResult<RcaEvidenceDto>.Ok(ToEvidenceDto(evidence), "Adjunto reemplazado."));
    }

    public Task<ApiResult<RcaEvidenceDto>> DeleteEvidenceAsync(Guid incidentId, Guid evidenceId, CancellationToken cancellationToken = default)
    {
        if (!_incidents.TryGetValue(incidentId, out var incident) || incident.IsDeleted)
        {
            return Task.FromResult(ApiResult<RcaEvidenceDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." }));
        }

        var evidence = incident.Evidence.FirstOrDefault(x => x.Id == evidenceId && !x.IsDeleted);
        if (evidence is null)
        {
            return Task.FromResult(ApiResult<RcaEvidenceDto>.Fail(
                "No se encontro la evidencia RCA.",
                new ApiError { Field = nameof(evidenceId), Code = "EVIDENCE_NOT_FOUND", Message = "La evidencia no corresponde al incidente RCA." }));
        }

        evidence.IsDeleted = true;
        evidence.UpdatedAt = DateTimeOffset.UtcNow;

        return Task.FromResult(ApiResult<RcaEvidenceDto>.Ok(ToEvidenceDto(evidence), "Evidencia eliminada."));
    }

    public Task<ApiResult<RcaIncidentDto>> CloseAsync(Guid incidentId, CloseRcaIncidentRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateCloseRequest(request);
        if (validationErrors.Count > 0)
        {
            return Task.FromResult(ApiResult<RcaIncidentDto>.Fail("No se pudo cerrar el incidente RCA.", validationErrors.ToArray()));
        }

        if (!_incidents.TryGetValue(incidentId, out var incident) || incident.IsDeleted)
        {
            return Task.FromResult(ApiResult<RcaIncidentDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." }));
        }

        if (incident.Status == RcaIncidentStatus.Closed)
        {
            return Task.FromResult(ApiResult<RcaIncidentDto>.Ok(ToDto(incident), "El incidente RCA ya estaba cerrado."));
        }

        var hasRootCause = incident.Branches
            .SelectMany(x => x.Causes)
            .Any(x => x.IsRootCause && !x.IsDeleted);

        if (!hasRootCause)
        {
            return Task.FromResult(ApiResult<RcaIncidentDto>.Fail(
                "No se pudo cerrar el incidente RCA.",
                new ApiError { Field = "RootCause", Code = "ROOT_CAUSE_REQUIRED", Message = "Debe existir una causa raiz antes del cierre." }));
        }

        var hasOpenActions = incident.CorrectiveActions.Any(x =>
            x.Status is not CorrectiveActionStatus.Completed and not CorrectiveActionStatus.Cancelled &&
            !x.IsDeleted);

        if (hasOpenActions)
        {
            return Task.FromResult(ApiResult<RcaIncidentDto>.Fail(
                "No se pudo cerrar el incidente RCA.",
                new ApiError { Field = "CorrectiveActions", Code = "OPEN_ACTIONS_EXIST", Message = "Todas las acciones deben estar completadas o canceladas antes del cierre." }));
        }

        var now = DateTimeOffset.UtcNow;
        incident.Status = RcaIncidentStatus.Closed;
        incident.ClosedAt = now;
        incident.ClosedByUserId = Normalize(request.ClosedByUserId);
        incident.ClosureSummary = request.ClosureSummary.Trim();
        incident.UpdatedAt = now;
        incident.UpdatedByUserId = Normalize(request.ClosedByUserId);

        return Task.FromResult(ApiResult<RcaIncidentDto>.Ok(ToDto(incident), "Incidente RCA cerrado."));
    }

    public Task<ApiResult<RcaIncidentDto>> EscalateTo8DAsync(Guid incidentId, EscalateRcaIncidentTo8DRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateEscalateTo8DRequest(request);
        if (validationErrors.Count > 0)
        {
            return Task.FromResult(ApiResult<RcaIncidentDto>.Fail("No se pudo escalar el incidente RCA a 8D.", validationErrors.ToArray()));
        }

        if (!_incidents.TryGetValue(incidentId, out var incident) || incident.IsDeleted)
        {
            return Task.FromResult(ApiResult<RcaIncidentDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." }));
        }

        if (incident.Status == RcaIncidentStatus.Closed)
        {
            return Task.FromResult(ApiResult<RcaIncidentDto>.Fail(
                "No se pudo escalar el incidente RCA a 8D.",
                new ApiError { Field = nameof(incident.Status), Code = "RCA_ALREADY_CLOSED", Message = "Un RCA cerrado no puede escalarse a 8D." }));
        }

        var now = DateTimeOffset.UtcNow;
        incident.EscalatedTo8D = true;
        incident.EscalatedTo8DAt ??= now;
        incident.EscalatedTo8DByUserId = Normalize(request.EscalatedByUserId);
        incident.EscalationReason = request.EscalationReason.Trim();
        incident.Status = RcaIncidentStatus.EscalatedTo8D;
        incident.UpdatedAt = now;
        incident.UpdatedByUserId = Normalize(request.EscalatedByUserId);

        return Task.FromResult(ApiResult<RcaIncidentDto>.Ok(ToDto(incident), "Incidente RCA escalado a 8D."));
    }

    public Task<ApiResult<RcaIncidentDto>> CompleteWizardStepAsync(Guid incidentId, CompleteRcaWizardStepRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateCompleteWizardStepRequest(request);
        if (validationErrors.Count > 0)
        {
            return Task.FromResult(ApiResult<RcaIncidentDto>.Fail("No se pudo completar la etapa del wizard RCA.", validationErrors.ToArray()));
        }

        if (!_incidents.TryGetValue(incidentId, out var incident) || incident.IsDeleted)
        {
            return Task.FromResult(ApiResult<RcaIncidentDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." }));
        }

        var step = ParseWizardStep(request.Step);
        var prerequisiteErrors = ValidateWizardPrerequisites(incident, step);
        if (prerequisiteErrors.Count > 0)
        {
            return Task.FromResult(ApiResult<RcaIncidentDto>.Fail("No se pudo completar la etapa del wizard RCA.", prerequisiteErrors.ToArray()));
        }

        var now = DateTimeOffset.UtcNow;
        if (step >= incident.WizardStep)
        {
            incident.WizardStep = step;
        }

        incident.WizardStepCompletedAt = now;
        incident.WizardStepCompletedByUserId = Normalize(request.CompletedByUserId);
        incident.WizardStepNotes = Normalize(request.Notes);
        incident.UpdatedAt = now;
        incident.UpdatedByUserId = Normalize(request.CompletedByUserId);

        return Task.FromResult(ApiResult<RcaIncidentDto>.Ok(ToDto(incident), "Etapa del wizard RCA completada."));
    }

    public Task<ApiResult<RcaWizardProgressDto>> GetWizardProgressAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        if (!_incidents.TryGetValue(incidentId, out var incident) || incident.IsDeleted)
        {
            return Task.FromResult(ApiResult<RcaWizardProgressDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." }));
        }

        var causes = incident.Branches.SelectMany(x => x.Causes).Where(x => !x.IsDeleted).ToList();
        var actions = incident.CorrectiveActions.Where(x => !x.IsDeleted).ToList();
        var evidence = incident.Evidence.Where(x => !x.IsDeleted).ToList();

        return Task.FromResult(ApiResult<RcaWizardProgressDto>.Ok(BuildWizardProgress(incident, causes, actions, evidence)));
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

    private static List<ApiError> ValidateUpdateCorrectiveActionStatusRequest(UpdateCorrectiveActionStatusRequest request)
    {
        var errors = new List<ApiError>();

        if (!Enum.TryParse<CorrectiveActionStatus>(request.Status, true, out var status))
        {
            errors.Add(new ApiError { Field = nameof(request.Status), Code = "INVALID_ACTION_STATUS", Message = "Status debe ser Open, InProgress, WaitingValidation, Completed o Cancelled." });
        }

        if (status == CorrectiveActionStatus.Completed && string.IsNullOrWhiteSpace(request.ValidationNotes))
        {
            errors.Add(new ApiError { Field = nameof(request.ValidationNotes), Code = "VALIDATION_NOTES_REQUIRED", Message = "Para completar una accion se requiere evidencia o nota de validacion." });
        }

        return errors;
    }

    private static List<ApiError> ValidateAddEvidenceRequest(AddRcaEvidenceRequest request)
    {
        var errors = new List<ApiError>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add(new ApiError { Field = nameof(request.Title), Code = "EVIDENCE_TITLE_REQUIRED", Message = "El titulo de la evidencia es obligatorio." });
        }

        if (string.IsNullOrWhiteSpace(request.EvidenceType))
        {
            errors.Add(new ApiError { Field = nameof(request.EvidenceType), Code = "EVIDENCE_TYPE_REQUIRED", Message = "El tipo de evidencia es obligatorio." });
        }

        if (string.IsNullOrWhiteSpace(request.Source))
        {
            errors.Add(new ApiError { Field = nameof(request.Source), Code = "EVIDENCE_SOURCE_REQUIRED", Message = "El origen de la evidencia es obligatorio." });
        }

        ValidateEvidenceValidationMetadata(
            request.ValidationStatus,
            request.ValidatedByUserId,
            request.ValidationNotes,
            errors);

        return errors;
    }

    private static List<ApiError> ValidateUpdateEvidenceRequest(UpdateRcaEvidenceRequest request)
    {
        var errors = new List<ApiError>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add(new ApiError { Field = nameof(request.Title), Code = "EVIDENCE_TITLE_REQUIRED", Message = "El titulo de la evidencia es obligatorio." });
        }

        if (string.IsNullOrWhiteSpace(request.EvidenceType))
        {
            errors.Add(new ApiError { Field = nameof(request.EvidenceType), Code = "EVIDENCE_TYPE_REQUIRED", Message = "El tipo de evidencia es obligatorio." });
        }

        if (string.IsNullOrWhiteSpace(request.Source))
        {
            errors.Add(new ApiError { Field = nameof(request.Source), Code = "EVIDENCE_SOURCE_REQUIRED", Message = "El origen de la evidencia es obligatorio." });
        }

        ValidateEvidenceValidationMetadata(
            request.ValidationStatus,
            request.ValidatedByUserId,
            request.ValidationNotes,
            errors);

        return errors;
    }

    private static void ValidateEvidenceValidationMetadata(string? status, string? validatedByUserId, string? validationNotes, List<ApiError> errors)
    {
        var requestedStatus = Normalize(status) ?? "PendingReview";
        if (!EvidenceValidationStatuses.Contains(requestedStatus))
        {
            errors.Add(new ApiError { Field = nameof(status), Code = "INVALID_EVIDENCE_VALIDATION_STATUS", Message = "El estado de validacion debe ser PendingReview, Validated, Rejected o Expired." });
            return;
        }

        var normalizedStatus = NormalizeValidationStatus(requestedStatus);
        if (string.Equals(normalizedStatus, "Validated", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(validatedByUserId))
        {
            errors.Add(new ApiError { Field = nameof(validatedByUserId), Code = "EVIDENCE_VALIDATOR_REQUIRED", Message = "Para validar una evidencia debe indicar el usuario validador." });
        }

        if ((string.Equals(normalizedStatus, "Rejected", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(normalizedStatus, "Expired", StringComparison.OrdinalIgnoreCase)) &&
            string.IsNullOrWhiteSpace(validationNotes))
        {
            errors.Add(new ApiError { Field = nameof(validationNotes), Code = "EVIDENCE_VALIDATION_NOTES_REQUIRED", Message = "Para rechazar o vencer una evidencia debe cargar una nota." });
        }
    }

    private static List<ApiError> ValidateReplaceEvidenceAttachmentRequest(ReplaceRcaEvidenceAttachmentRequest request)
    {
        var errors = new List<ApiError>();

        if (string.IsNullOrWhiteSpace(request.AttachmentFileName))
        {
            errors.Add(new ApiError { Field = nameof(request.AttachmentFileName), Code = "ATTACHMENT_FILE_NAME_REQUIRED", Message = "El nombre del adjunto es obligatorio." });
        }

        if (string.IsNullOrWhiteSpace(request.AttachmentStorageKey))
        {
            errors.Add(new ApiError { Field = nameof(request.AttachmentStorageKey), Code = "ATTACHMENT_STORAGE_KEY_REQUIRED", Message = "La ubicacion del adjunto es obligatoria." });
        }

        if (request.AttachmentSizeBytes <= 0)
        {
            errors.Add(new ApiError { Field = nameof(request.AttachmentSizeBytes), Code = "ATTACHMENT_SIZE_REQUIRED", Message = "El adjunto debe tener contenido." });
        }

        return errors;
    }

    private static List<ApiError> ValidateCloseRequest(CloseRcaIncidentRequest request)
    {
        var errors = new List<ApiError>();

        if (string.IsNullOrWhiteSpace(request.ClosureSummary))
        {
            errors.Add(new ApiError { Field = nameof(request.ClosureSummary), Code = "CLOSURE_SUMMARY_REQUIRED", Message = "El resumen de cierre es obligatorio." });
        }

        return errors;
    }

    private static List<ApiError> ValidateEscalateTo8DRequest(EscalateRcaIncidentTo8DRequest request)
    {
        var errors = new List<ApiError>();

        if (string.IsNullOrWhiteSpace(request.EscalationReason))
        {
            errors.Add(new ApiError { Field = nameof(request.EscalationReason), Code = "ESCALATION_REASON_REQUIRED", Message = "El motivo de escalamiento a 8D es obligatorio." });
        }

        return errors;
    }

    private static List<ApiError> ValidateCompleteWizardStepRequest(CompleteRcaWizardStepRequest request)
    {
        var errors = new List<ApiError>();

        if (string.IsNullOrWhiteSpace(request.Step))
        {
            errors.Add(new ApiError { Field = nameof(request.Step), Code = "WIZARD_STEP_REQUIRED", Message = "La etapa del wizard es obligatoria." });
        }
        else if (!Enum.TryParse<RcaWizardStep>(request.Step, true, out _))
        {
            errors.Add(new ApiError { Field = nameof(request.Step), Code = "INVALID_WIZARD_STEP", Message = "Step debe ser Problem, Causes, Evidence, Actions, Validation o Closed." });
        }

        return errors;
    }

    private static List<ApiError> ValidateWizardPrerequisites(RcaIncident incident, RcaWizardStep step)
    {
        var errors = new List<ApiError>();
        var causes = incident.Branches.SelectMany(x => x.Causes).Where(x => !x.IsDeleted).ToList();
        var actions = incident.CorrectiveActions.Where(x => !x.IsDeleted).ToList();
        var evidence = incident.Evidence.Where(x => !x.IsDeleted).ToList();

        if (step >= RcaWizardStep.Causes && causes.Count == 0)
        {
            errors.Add(new ApiError { Field = "Causes", Code = "CAUSE_REQUIRED", Message = "Debe cargar al menos una causa para avanzar el wizard." });
        }

        if (step >= RcaWizardStep.Evidence && evidence.Count == 0)
        {
            errors.Add(new ApiError { Field = "Evidence", Code = "EVIDENCE_REQUIRED", Message = "Debe registrar al menos una evidencia para avanzar el wizard." });
        }

        if (step >= RcaWizardStep.Actions && actions.Count == 0)
        {
            errors.Add(new ApiError { Field = "CorrectiveActions", Code = "ACTION_REQUIRED", Message = "Debe registrar al menos una accion correctiva para avanzar el wizard." });
        }

        if (step >= RcaWizardStep.Actions && !causes.Any(x => x.IsRootCause))
        {
            errors.Add(new ApiError { Field = "RootCause", Code = "ROOT_CAUSE_REQUIRED", Message = "Debe marcar una causa raiz para avanzar a acciones." });
        }

        if (step >= RcaWizardStep.Validation &&
            actions.Any(x => x.Status is not CorrectiveActionStatus.Completed and not CorrectiveActionStatus.Cancelled))
        {
            errors.Add(new ApiError { Field = "CorrectiveActions", Code = "OPEN_ACTIONS_EXIST", Message = "Todas las acciones deben estar completadas o canceladas para validar el wizard." });
        }

        if (step >= RcaWizardStep.Validation &&
            !evidence.Any(x => string.Equals(x.ValidationStatus, "Validated", StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add(new ApiError { Field = "Evidence", Code = "VALIDATED_EVIDENCE_REQUIRED", Message = "Debe existir al menos una evidencia validada para avanzar a validacion." });
        }

        if (step == RcaWizardStep.Closed && incident.Status != RcaIncidentStatus.Closed)
        {
            errors.Add(new ApiError { Field = nameof(incident.Status), Code = "RCA_NOT_CLOSED", Message = "El RCA debe estar cerrado para completar la etapa Closed del wizard." });
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

    private static CorrectiveActionStatus ParseCorrectiveActionStatus(string status)
    {
        return Enum.TryParse<CorrectiveActionStatus>(status, true, out var parsed)
            ? parsed
            : CorrectiveActionStatus.Open;
    }

    private static RcaWizardStep ParseWizardStep(string step)
    {
        return Enum.TryParse<RcaWizardStep>(step, true, out var parsed)
            ? parsed
            : RcaWizardStep.Problem;
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

    private static string NormalizeValidationStatus(string? value)
    {
        var normalized = Normalize(value);
        return EvidenceValidationStatuses.FirstOrDefault(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase))
            ?? "PendingReview";
    }

    private static DateTimeOffset? ResolveEvidenceValidatedAt(string? status, DateTimeOffset? validatedAt)
    {
        var normalizedStatus = NormalizeValidationStatus(status);
        return string.Equals(normalizedStatus, "PendingReview", StringComparison.OrdinalIgnoreCase)
            ? null
            : validatedAt ?? DateTimeOffset.UtcNow;
    }

    private static string? NormalizeTags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var tags = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.Trim().TrimStart('#'))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();

        return tags.Count == 0 ? null : string.Join(", ", tags);
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
            ClosedByUserId = incident.ClosedByUserId,
            ClosureSummary = incident.ClosureSummary,
            SourceSystem = incident.SourceSystem,
            ExternalTaskId = incident.ExternalTaskId,
            ExternalEventId = incident.ExternalEventId,
            ExternalWorkOrderId = incident.ExternalWorkOrderId,
            MachineCode = incident.MachineCode,
            LineCode = incident.LineCode,
            WorkOrderCode = incident.WorkOrderCode,
            EscalatedTo8D = incident.EscalatedTo8D,
            EscalatedTo8DAt = incident.EscalatedTo8DAt,
            EscalatedTo8DByUserId = incident.EscalatedTo8DByUserId,
            EscalationReason = incident.EscalationReason,
            WizardStep = incident.WizardStep.ToString(),
            WizardStepCompletedAt = incident.WizardStepCompletedAt,
            WizardStepCompletedByUserId = incident.WizardStepCompletedByUserId,
            WizardStepNotes = incident.WizardStepNotes
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
            CompletedAt = action.CompletedAt,
            CompletedByUserId = action.CompletedByUserId,
            ValidationNotes = action.ValidationNotes
        };
    }

    private static RcaEvidenceDto ToEvidenceDto(RcaEvidence evidence)
    {
        return new RcaEvidenceDto
        {
            Id = evidence.Id,
            RcaIncidentId = evidence.RcaIncidentId,
            CauseId = evidence.CauseId,
            ExternalIntakeId = evidence.ExternalIntakeId,
            Title = evidence.Title,
            EvidenceType = evidence.EvidenceType,
            Source = evidence.Source,
            SourceDetail = evidence.SourceDetail,
            Tags = evidence.Tags,
            Summary = evidence.Summary,
            ReferenceUri = evidence.ReferenceUri,
            AttachmentFileName = evidence.AttachmentFileName,
            AttachmentContentType = evidence.AttachmentContentType,
            AttachmentSizeBytes = evidence.AttachmentSizeBytes,
            AttachmentStorageProvider = evidence.AttachmentStorageProvider,
            AttachmentStorageKey = evidence.AttachmentStorageKey,
            AttachmentSha256 = evidence.AttachmentSha256,
            CapturedAt = evidence.CapturedAt,
            CapturedByUserId = evidence.CapturedByUserId,
            ValidationStatus = evidence.ValidationStatus,
            ValidatedAt = evidence.ValidatedAt,
            ValidatedByUserId = evidence.ValidatedByUserId,
            ValidationNotes = evidence.ValidationNotes,
            CreatedAt = evidence.CreatedAt
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
            LastUpdatedAt = GetLastUpdatedAt(incident, causes, incident.CorrectiveActions, incident.Evidence),
            SourceSystem = incident.SourceSystem,
            ExternalTaskId = incident.ExternalTaskId,
            ExternalEventId = incident.ExternalEventId,
            ExternalWorkOrderId = incident.ExternalWorkOrderId,
            MachineCode = incident.MachineCode,
            LineCode = incident.LineCode,
            WorkOrderCode = incident.WorkOrderCode,
            EscalatedTo8D = incident.EscalatedTo8D,
            WizardStep = incident.WizardStep.ToString(),
            RootCauseTitle = rootCause?.Title,
            RootCauseEvidenceSummary = rootCause?.EvidenceSummary,
            CauseCount = causes.Count,
            EvidenceCount = incident.Evidence.Count(x => !x.IsDeleted),
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

    private static RcaWizardProgressDto BuildWizardProgress(
        RcaIncident incident,
        IReadOnlyList<IshikawaCause> causes,
        IReadOnlyList<CorrectiveAction> actions,
        IReadOnlyList<RcaEvidence> evidence)
    {
        var steps = new[]
        {
            (Step: RcaWizardStep.Problem, Label: "Problema", Requirements: new[] { "Problema, alcance, severidad y origen definidos." }),
            (Step: RcaWizardStep.Causes, Label: "Causas", Requirements: new[] { "Al menos una causa cargada en el Ishikawa." }),
            (Step: RcaWizardStep.Evidence, Label: "Evidencias", Requirements: new[] { "Al menos una evidencia asociada al analisis." }),
            (Step: RcaWizardStep.Actions, Label: "Acciones", Requirements: new[] { "Causa raiz marcada.", "Al menos una accion correctiva cargada." }),
            (Step: RcaWizardStep.Validation, Label: "Validacion", Requirements: new[] { "Acciones completadas o canceladas.", "Al menos una evidencia validada." }),
            (Step: RcaWizardStep.Closed, Label: "Cierre", Requirements: new[] { "RCA cerrado formalmente." })
        };

        var current = incident.WizardStep;
        var currentIndex = Array.FindIndex(steps, x => x.Step == current);
        currentIndex = currentIndex < 0 ? 0 : currentIndex;
        var nextRecommended = current < RcaWizardStep.Closed
            ? (RcaWizardStep)((int)current + 1)
            : RcaWizardStep.Closed;

        var checklist = steps.Select((step, index) =>
        {
            var blockers = GetWizardBlockingReasons(step.Step, incident, causes, actions, evidence);
            var isCompleted = index < currentIndex;
            var isCurrent = index == currentIndex;
            var isBlocked = !isCompleted && blockers.Count > 0;

            return new RcaWizardStepChecklistItemDto
            {
                Step = step.Step.ToString(),
                Label = step.Label,
                Status = isCompleted ? "Done" : isCurrent ? "Current" : isBlocked ? "Blocked" : "Ready",
                IsCompleted = isCompleted,
                IsCurrent = isCurrent,
                IsBlocked = isBlocked,
                Requirements = step.Requirements.ToList(),
                BlockingReasons = blockers,
                Metrics = BuildWizardStepMetrics(step.Step, incident, causes, actions, evidence)
            };
        }).ToList();

        return new RcaWizardProgressDto
        {
            IncidentId = incident.Id,
            CurrentStep = current.ToString(),
            NextRecommendedStep = nextRecommended.ToString(),
            CompletionPercent = (int)Math.Round(currentIndex / (double)(steps.Length - 1) * 100, MidpointRounding.AwayFromZero),
            Steps = checklist
        };
    }

    private static List<string> GetWizardBlockingReasons(
        RcaWizardStep step,
        RcaIncident incident,
        IReadOnlyList<IshikawaCause> causes,
        IReadOnlyList<CorrectiveAction> actions,
        IReadOnlyList<RcaEvidence> evidence)
    {
        var blockers = new List<string>();

        if (step >= RcaWizardStep.Causes && causes.Count == 0)
        {
            blockers.Add("Falta cargar al menos una causa.");
        }

        if (step >= RcaWizardStep.Evidence && evidence.Count == 0)
        {
            blockers.Add("Falta registrar evidencia.");
        }

        if (step >= RcaWizardStep.Actions && !causes.Any(x => x.IsRootCause))
        {
            blockers.Add("Falta marcar una causa raiz.");
        }

        if (step >= RcaWizardStep.Actions && actions.Count == 0)
        {
            blockers.Add("Falta cargar una accion correctiva.");
        }

        if (step >= RcaWizardStep.Validation &&
            actions.Any(x => x.Status is not CorrectiveActionStatus.Completed and not CorrectiveActionStatus.Cancelled))
        {
            blockers.Add("Hay acciones correctivas abiertas.");
        }

        if (step >= RcaWizardStep.Validation &&
            !evidence.Any(x => string.Equals(x.ValidationStatus, "Validated", StringComparison.OrdinalIgnoreCase)))
        {
            blockers.Add("Falta evidencia validada.");
        }

        if (step == RcaWizardStep.Closed && incident.Status != RcaIncidentStatus.Closed)
        {
            blockers.Add("El RCA todavia no esta cerrado.");
        }

        return blockers;
    }

    private static Dictionary<string, string> BuildWizardStepMetrics(
        RcaWizardStep step,
        RcaIncident incident,
        IReadOnlyList<IshikawaCause> causes,
        IReadOnlyList<CorrectiveAction> actions,
        IReadOnlyList<RcaEvidence> evidence)
    {
        return step switch
        {
            RcaWizardStep.Problem => new Dictionary<string, string>
            {
                ["Severidad"] = incident.Severity.ToString(),
                ["Origen"] = incident.ClaimActorType.ToString()
            },
            RcaWizardStep.Causes => new Dictionary<string, string>
            {
                ["Causas"] = causes.Count.ToString(),
                ["Raiz"] = causes.Count(x => x.IsRootCause).ToString()
            },
            RcaWizardStep.Evidence => new Dictionary<string, string>
            {
                ["Evidencias"] = evidence.Count.ToString(),
                ["Validadas"] = evidence.Count(x => string.Equals(x.ValidationStatus, "Validated", StringComparison.OrdinalIgnoreCase)).ToString()
            },
            RcaWizardStep.Actions => new Dictionary<string, string>
            {
                ["Acciones"] = actions.Count.ToString(),
                ["Abiertas"] = actions.Count(x => x.Status is not CorrectiveActionStatus.Completed and not CorrectiveActionStatus.Cancelled).ToString()
            },
            RcaWizardStep.Validation => new Dictionary<string, string>
            {
                ["Acciones cerradas"] = actions.Count(x => x.Status is CorrectiveActionStatus.Completed or CorrectiveActionStatus.Cancelled).ToString(),
                ["Evidencias validadas"] = evidence.Count(x => string.Equals(x.ValidationStatus, "Validated", StringComparison.OrdinalIgnoreCase)).ToString()
            },
            RcaWizardStep.Closed => new Dictionary<string, string>
            {
                ["Estado"] = incident.Status.ToString(),
                ["Cerrado"] = incident.ClosedAt.HasValue ? "Si" : "No"
            },
            _ => []
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

        if (incident.ClosedAt.HasValue)
        {
            events.Add(CreateEvent(
                $"rca-incident-closed:{incident.Id}",
                "RcaClosed",
                incident.ClosedAt.Value,
                incident,
                new Dictionary<string, string?>
                {
                    ["title"] = incident.Title,
                    ["status"] = incident.Status.ToString(),
                    ["closedByUserId"] = incident.ClosedByUserId,
                    ["closureSummary"] = incident.ClosureSummary
                }));
        }

        if (incident.EscalatedTo8D && incident.EscalatedTo8DAt.HasValue)
        {
            events.Add(CreateEvent(
                $"rca-incident-escalated-8d:{incident.Id}",
                "RcaEscalatedTo8D",
                incident.EscalatedTo8DAt.Value,
                incident,
                new Dictionary<string, string?>
                {
                    ["title"] = incident.Title,
                    ["status"] = incident.Status.ToString(),
                    ["escalatedByUserId"] = incident.EscalatedTo8DByUserId,
                    ["escalationReason"] = incident.EscalationReason
                }));
        }

        if (incident.WizardStepCompletedAt.HasValue)
        {
            events.Add(CreateEvent(
                $"rca-wizard-step-completed:{incident.Id}:{incident.WizardStep}",
                "RcaWizardStepCompleted",
                incident.WizardStepCompletedAt.Value,
                incident,
                new Dictionary<string, string?>
                {
                    ["title"] = incident.Title,
                    ["step"] = incident.WizardStep.ToString(),
                    ["completedByUserId"] = incident.WizardStepCompletedByUserId,
                    ["notes"] = incident.WizardStepNotes
                }));
        }

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
                    ["parentCauseId"] = cause.ParentCauseId?.ToString(),
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

            if (action.CompletedAt.HasValue)
            {
                events.Add(CreateEvent(
                    $"rca-action-completed:{action.Id}",
                    "RcaCorrectiveActionCompleted",
                    action.CompletedAt.Value,
                    incident,
                    new Dictionary<string, string?>
                    {
                        ["actionId"] = action.Id.ToString(),
                        ["causeId"] = action.CauseId?.ToString(),
                        ["title"] = action.Title,
                        ["status"] = action.Status.ToString(),
                        ["completedByUserId"] = action.CompletedByUserId,
                        ["validationNotes"] = action.ValidationNotes
                    }));
            }
        }

        foreach (var evidence in incident.Evidence.Where(x => !x.IsDeleted))
        {
            events.Add(CreateEvent(
                $"rca-evidence-attached:{evidence.Id}",
                "RcaEvidenceAttached",
                evidence.CreatedAt,
                incident,
                new Dictionary<string, string?>
                {
                    ["evidenceId"] = evidence.Id.ToString(),
                    ["causeId"] = evidence.CauseId?.ToString(),
                    ["externalIntakeId"] = evidence.ExternalIntakeId?.ToString(),
                    ["title"] = evidence.Title,
                    ["evidenceType"] = evidence.EvidenceType,
                    ["source"] = evidence.Source,
                    ["sourceDetail"] = evidence.SourceDetail,
                    ["tags"] = evidence.Tags,
                    ["validationStatus"] = evidence.ValidationStatus,
                    ["validatedByUserId"] = evidence.ValidatedByUserId,
                    ["referenceUri"] = evidence.ReferenceUri,
                    ["attachmentFileName"] = evidence.AttachmentFileName,
                    ["attachmentContentType"] = evidence.AttachmentContentType,
                    ["attachmentSizeBytes"] = evidence.AttachmentSizeBytes?.ToString(),
                    ["attachmentStorageProvider"] = evidence.AttachmentStorageProvider,
                    ["attachmentSha256"] = evidence.AttachmentSha256
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

    private static DateTimeOffset GetLastUpdatedAt(RcaIncident incident, IReadOnlyList<IshikawaCause> causes, IEnumerable<CorrectiveAction> actions, IEnumerable<RcaEvidence> evidence)
    {
        return new[]
            {
                incident.UpdatedAt ?? incident.CreatedAt,
                causes.Select(x => x.UpdatedAt ?? x.CreatedAt).DefaultIfEmpty(incident.CreatedAt).Max(),
                actions.Select(x => x.UpdatedAt ?? x.CreatedAt).DefaultIfEmpty(incident.CreatedAt).Max(),
                evidence.Select(x => x.UpdatedAt ?? x.CreatedAt).DefaultIfEmpty(incident.CreatedAt).Max()
            }
            .Max();
    }
}
