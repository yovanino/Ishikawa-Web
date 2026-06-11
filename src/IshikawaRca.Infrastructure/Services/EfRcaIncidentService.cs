using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Entities;
using IshikawaRca.Domain.Enums;
using IshikawaRca.Domain.Services;
using IshikawaRca.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IshikawaRca.Infrastructure.Services;

public class EfRcaIncidentService : IRcaIncidentService
{
    private static readonly JsonSerializerOptions OutboxSerializerOptions = new(JsonSerializerDefaults.Web);

    private static readonly HashSet<string> EvidenceValidationStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "PendingReview",
        "Validated",
        "Rejected",
        "Expired"
    };

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

        _dbContext.RcaIncidents.Add(incident);
        await AddOutboxEventAsync(
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
                }),
            cancellationToken);
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

        var incident = await _dbContext.RcaIncidents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

        if (incident is not null)
        {
            await AddOutboxEventAsync(
                CreateEvent(
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
                    }),
                cancellationToken);
        }

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
            ActionType = ParseCorrectiveActionType(request.ActionType),
            ResolutionScope = ParseResolutionScope(request.ResolutionScope),
            AssignedToUserId = Normalize(request.AssignedToUserId),
            DueDate = request.DueDate,
            Status = CorrectiveActionStatus.Open
        };

        _dbContext.CorrectiveActions.Add(action);
        await AddOutboxEventAsync(
            CreateEvent(
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
                }),
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<CorrectiveActionDto>.Ok(ToActionDto(action), "Accion correctiva agregada.");
    }

    public async Task<ApiResult<CorrectiveActionDto>> UpdateCorrectiveActionStatusAsync(Guid incidentId, Guid actionId, UpdateCorrectiveActionStatusRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateUpdateCorrectiveActionStatusRequest(request);
        if (validationErrors.Count > 0)
        {
            return ApiResult<CorrectiveActionDto>.Fail("No se pudo actualizar la accion correctiva.", validationErrors.ToArray());
        }

        var action = await _dbContext.CorrectiveActions
            .FirstOrDefaultAsync(x => x.Id == actionId && x.RcaIncidentId == incidentId && !x.IsDeleted, cancellationToken);

        if (action is null)
        {
            return ApiResult<CorrectiveActionDto>.Fail(
                "No se encontro la accion correctiva.",
                new ApiError { Field = nameof(actionId), Code = "ACTION_NOT_FOUND", Message = "La accion seleccionada no corresponde al incidente RCA." });
        }

        var status = ParseCorrectiveActionStatus(request.Status);
        var now = DateTimeOffset.UtcNow;
        var previousStatus = action.Status;

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

        AddAuditRecord(
            action.TenantId,
            incidentId,
            nameof(CorrectiveAction),
            action.Id,
            "CorrectiveActionStatusChanged",
            action.UpdatedByUserId,
            $"Estado de accion cambiado de {previousStatus} a {status}.",
            new
            {
                previousStatus = previousStatus.ToString(),
                newStatus = status.ToString(),
                action.CompletedByUserId,
                action.ValidationNotes
            });

        if (status == CorrectiveActionStatus.Completed && previousStatus != CorrectiveActionStatus.Completed)
        {
            var incident = await _dbContext.RcaIncidents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

            if (incident is not null)
            {
                await AddOutboxEventAsync(
                    CreateEvent(
                        $"rca-action-completed:{action.Id}",
                        "RcaCorrectiveActionCompleted",
                        action.CompletedAt ?? now,
                        incident,
                        new Dictionary<string, string?>
                        {
                            ["actionId"] = action.Id.ToString(),
                            ["causeId"] = action.CauseId?.ToString(),
                            ["title"] = action.Title,
                            ["status"] = action.Status.ToString(),
                            ["completedByUserId"] = action.CompletedByUserId,
                            ["validationNotes"] = action.ValidationNotes
                        }),
                    cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<CorrectiveActionDto>.Ok(ToActionDto(action), "Estado de accion actualizado.");
    }

    public async Task<ApiResult<IReadOnlyList<RcaEvidenceDto>>> ListEvidenceAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var incidentExists = await _dbContext.RcaIncidents
            .AsNoTracking()
            .AnyAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

        if (!incidentExists)
        {
            return ApiResult<IReadOnlyList<RcaEvidenceDto>>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
        }

        var evidence = await _dbContext.RcaEvidence
            .AsNoTracking()
            .Where(x => x.RcaIncidentId == incidentId && !x.IsDeleted)
            .OrderByDescending(x => x.CapturedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => ToEvidenceDto(x))
            .ToListAsync(cancellationToken);

        return ApiResult<IReadOnlyList<RcaEvidenceDto>>.Ok(evidence);
    }

    public async Task<ApiResult<RcaEvidenceDto>> AddEvidenceAsync(Guid incidentId, AddRcaEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateAddEvidenceRequest(request);
        if (validationErrors.Count > 0)
        {
            return ApiResult<RcaEvidenceDto>.Fail("No se pudo agregar la evidencia.", validationErrors.ToArray());
        }

        var incident = await _dbContext.RcaIncidents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

        if (incident is null)
        {
            return ApiResult<RcaEvidenceDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
        }

        if (request.CauseId.HasValue)
        {
            var causeExists = await _dbContext.IshikawaCauses
                .AnyAsync(x => x.Id == request.CauseId.Value && x.RcaIncidentId == incidentId && !x.IsDeleted, cancellationToken);

            if (!causeExists)
            {
                return ApiResult<RcaEvidenceDto>.Fail(
                    "No se pudo agregar la evidencia.",
                    new ApiError { Field = nameof(request.CauseId), Code = "CAUSE_NOT_FOUND", Message = "La causa seleccionada no corresponde al incidente RCA." });
            }
        }

        if (request.ExternalIntakeId.HasValue)
        {
            var intakeExists = await _dbContext.RcaExternalIntakeRequests
                .AnyAsync(x => x.Id == request.ExternalIntakeId.Value && x.RcaIncidentId == incidentId && !x.IsDeleted, cancellationToken);

            if (!intakeExists)
            {
                return ApiResult<RcaEvidenceDto>.Fail(
                    "No se pudo agregar la evidencia.",
                    new ApiError { Field = nameof(request.ExternalIntakeId), Code = "INTAKE_NOT_FOUND", Message = "El intake externo no corresponde al incidente RCA." });
            }
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

        _dbContext.RcaEvidence.Add(evidence);
        await AddOutboxEventAsync(
            CreateEvent(
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
                }),
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<RcaEvidenceDto>.Ok(ToEvidenceDto(evidence), "Evidencia agregada.");
    }

    public async Task<ApiResult<RcaEvidenceDto>> UpdateEvidenceAsync(Guid incidentId, Guid evidenceId, UpdateRcaEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateUpdateEvidenceRequest(request);
        if (validationErrors.Count > 0)
        {
            return ApiResult<RcaEvidenceDto>.Fail("No se pudo actualizar la evidencia.", validationErrors.ToArray());
        }

        var evidence = await _dbContext.RcaEvidence
            .FirstOrDefaultAsync(x => x.Id == evidenceId && x.RcaIncidentId == incidentId && !x.IsDeleted, cancellationToken);

        if (evidence is null)
        {
            return ApiResult<RcaEvidenceDto>.Fail(
                "No se encontro la evidencia RCA.",
                new ApiError { Field = nameof(evidenceId), Code = "EVIDENCE_NOT_FOUND", Message = "La evidencia no corresponde al incidente RCA." });
        }

        if (request.CauseId.HasValue)
        {
            var causeExists = await _dbContext.IshikawaCauses
                .AnyAsync(x => x.Id == request.CauseId.Value && x.RcaIncidentId == incidentId && !x.IsDeleted, cancellationToken);

            if (!causeExists)
            {
                return ApiResult<RcaEvidenceDto>.Fail(
                    "No se pudo actualizar la evidencia.",
                    new ApiError { Field = nameof(request.CauseId), Code = "CAUSE_NOT_FOUND", Message = "La causa seleccionada no corresponde al incidente RCA." });
            }
        }

        var previousValidationStatus = evidence.ValidationStatus;
        var previousValidatedByUserId = evidence.ValidatedByUserId;

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
        evidence.UpdatedByUserId = evidence.ValidatedByUserId;

        AddAuditRecord(
            evidence.TenantId,
            incidentId,
            nameof(RcaEvidence),
            evidence.Id,
            "RcaEvidenceUpdated",
            evidence.UpdatedByUserId,
            $"Evidencia actualizada: {evidence.Title}.",
            new
            {
                previousValidationStatus,
                newValidationStatus = evidence.ValidationStatus,
                previousValidatedByUserId,
                evidence.ValidatedByUserId,
                evidence.ValidatedAt
            });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<RcaEvidenceDto>.Ok(ToEvidenceDto(evidence), "Evidencia actualizada.");
    }

    public async Task<ApiResult<RcaEvidenceDto>> ReplaceEvidenceAttachmentAsync(Guid incidentId, Guid evidenceId, ReplaceRcaEvidenceAttachmentRequest request, string? replacedByUserId = null, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateReplaceEvidenceAttachmentRequest(request);
        if (validationErrors.Count > 0)
        {
            return ApiResult<RcaEvidenceDto>.Fail("No se pudo reemplazar el adjunto.", validationErrors.ToArray());
        }

        var evidence = await _dbContext.RcaEvidence
            .FirstOrDefaultAsync(x => x.Id == evidenceId && x.RcaIncidentId == incidentId && !x.IsDeleted, cancellationToken);

        if (evidence is null)
        {
            return ApiResult<RcaEvidenceDto>.Fail(
                "No se encontro la evidencia RCA.",
                new ApiError { Field = nameof(evidenceId), Code = "EVIDENCE_NOT_FOUND", Message = "La evidencia no corresponde al incidente RCA." });
        }

        var previousAttachmentFileName = evidence.AttachmentFileName;
        var previousAttachmentSha256 = evidence.AttachmentSha256;

        evidence.AttachmentFileName = Normalize(request.AttachmentFileName);
        evidence.AttachmentContentType = Normalize(request.AttachmentContentType);
        evidence.AttachmentSizeBytes = request.AttachmentSizeBytes;
        evidence.AttachmentStorageProvider = Normalize(request.AttachmentStorageProvider);
        evidence.AttachmentStorageKey = Normalize(request.AttachmentStorageKey);
        evidence.AttachmentSha256 = Normalize(request.AttachmentSha256);
        evidence.UpdatedAt = DateTimeOffset.UtcNow;
        evidence.UpdatedByUserId = Normalize(replacedByUserId);

        AddAuditRecord(
            evidence.TenantId,
            incidentId,
            nameof(RcaEvidence),
            evidence.Id,
            "RcaEvidenceAttachmentReplaced",
            evidence.UpdatedByUserId,
            $"Adjunto de evidencia reemplazado: {evidence.Title}.",
            new
            {
                previousAttachmentFileName,
                previousAttachmentSha256,
                newAttachmentFileName = evidence.AttachmentFileName,
                newAttachmentSha256 = evidence.AttachmentSha256
            });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<RcaEvidenceDto>.Ok(ToEvidenceDto(evidence), "Adjunto reemplazado.");
    }

    public async Task<ApiResult<RcaEvidenceDto>> DeleteEvidenceAsync(Guid incidentId, Guid evidenceId, string? deletedByUserId = null, CancellationToken cancellationToken = default)
    {
        var evidence = await _dbContext.RcaEvidence
            .FirstOrDefaultAsync(x => x.Id == evidenceId && x.RcaIncidentId == incidentId && !x.IsDeleted, cancellationToken);

        if (evidence is null)
        {
            return ApiResult<RcaEvidenceDto>.Fail(
                "No se encontro la evidencia RCA.",
                new ApiError { Field = nameof(evidenceId), Code = "EVIDENCE_NOT_FOUND", Message = "La evidencia no corresponde al incidente RCA." });
        }

        evidence.IsDeleted = true;
        evidence.UpdatedAt = DateTimeOffset.UtcNow;
        evidence.UpdatedByUserId = Normalize(deletedByUserId);

        AddAuditRecord(
            evidence.TenantId,
            incidentId,
            nameof(RcaEvidence),
            evidence.Id,
            "RcaEvidenceDeleted",
            evidence.UpdatedByUserId,
            $"Evidencia eliminada: {evidence.Title}.",
            new
            {
                evidence.Title,
                evidence.AttachmentFileName,
                evidence.AttachmentSha256
            });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<RcaEvidenceDto>.Ok(ToEvidenceDto(evidence), "Evidencia eliminada.");
    }

    public async Task<ApiResult<IReadOnlyList<RcaFactDto>>> ListFactsAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var incidentExists = await _dbContext.RcaIncidents
            .AsNoTracking()
            .AnyAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

        if (!incidentExists)
        {
            return ApiResult<IReadOnlyList<RcaFactDto>>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
        }

        var facts = await _dbContext.RcaFacts
            .AsNoTracking()
            .Where(x => x.RcaIncidentId == incidentId && !x.IsDeleted)
            .OrderBy(x => x.OccurredAt)
            .ThenBy(x => x.CreatedAt)
            .Select(x => ToFactDto(x))
            .ToListAsync(cancellationToken);

        return ApiResult<IReadOnlyList<RcaFactDto>>.Ok(facts);
    }

    public async Task<ApiResult<RcaFactDto>> AddFactAsync(Guid incidentId, AddRcaFactRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateAddFactRequest(request);
        if (validationErrors.Count > 0)
        {
            return ApiResult<RcaFactDto>.Fail("No se pudo agregar el hecho.", validationErrors.ToArray());
        }

        var incident = await _dbContext.RcaIncidents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

        if (incident is null)
        {
            return ApiResult<RcaFactDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
        }

        if (request.CauseId.HasValue)
        {
            var causeExists = await _dbContext.IshikawaCauses
                .AnyAsync(x => x.Id == request.CauseId.Value && x.RcaIncidentId == incidentId && !x.IsDeleted, cancellationToken);

            if (!causeExists)
            {
                return ApiResult<RcaFactDto>.Fail(
                    "No se pudo agregar el hecho.",
                    new ApiError { Field = nameof(request.CauseId), Code = "CAUSE_NOT_FOUND", Message = "La causa seleccionada no corresponde al incidente RCA." });
            }
        }

        if (request.EvidenceId.HasValue)
        {
            var evidenceExists = await _dbContext.RcaEvidence
                .AnyAsync(x => x.Id == request.EvidenceId.Value && x.RcaIncidentId == incidentId && !x.IsDeleted, cancellationToken);

            if (!evidenceExists)
            {
                return ApiResult<RcaFactDto>.Fail(
                    "No se pudo agregar el hecho.",
                    new ApiError { Field = nameof(request.EvidenceId), Code = "EVIDENCE_NOT_FOUND", Message = "La evidencia seleccionada no corresponde al incidente RCA." });
            }
        }

        if (request.CorrectiveActionId.HasValue)
        {
            var actionExists = await _dbContext.CorrectiveActions
                .AnyAsync(x => x.Id == request.CorrectiveActionId.Value && x.RcaIncidentId == incidentId && !x.IsDeleted, cancellationToken);

            if (!actionExists)
            {
                return ApiResult<RcaFactDto>.Fail(
                    "No se pudo agregar el hecho.",
                    new ApiError { Field = nameof(request.CorrectiveActionId), Code = "ACTION_NOT_FOUND", Message = "La accion seleccionada no corresponde al incidente RCA." });
            }
        }

        if (request.ExternalIntakeId.HasValue)
        {
            var intakeExists = await _dbContext.RcaExternalIntakeRequests
                .AnyAsync(x => x.Id == request.ExternalIntakeId.Value && x.RcaIncidentId == incidentId && !x.IsDeleted, cancellationToken);

            if (!intakeExists)
            {
                return ApiResult<RcaFactDto>.Fail(
                    "No se pudo agregar el hecho.",
                    new ApiError { Field = nameof(request.ExternalIntakeId), Code = "INTAKE_NOT_FOUND", Message = "El intake externo no corresponde al incidente RCA." });
            }
        }

        var externalSourceSystem = Normalize(request.ExternalSourceSystem);
        var externalEventId = Normalize(request.ExternalEventId);
        if (!string.IsNullOrWhiteSpace(externalSourceSystem) && !string.IsNullOrWhiteSpace(externalEventId))
        {
            var existingExternalFact = await _dbContext.RcaFacts
                .AsNoTracking()
                .Where(x =>
                    x.RcaIncidentId == incidentId &&
                    x.ExternalSourceSystem == externalSourceSystem &&
                    x.ExternalEventId == externalEventId &&
                    !x.IsDeleted)
                .Select(x => ToFactDto(x))
                .FirstOrDefaultAsync(cancellationToken);

            if (existingExternalFact is not null)
            {
                return ApiResult<RcaFactDto>.Ok(existingExternalFact, "Hecho externo existente.");
            }
        }

        var fact = new RcaFact
        {
            TenantId = incident.TenantId,
            RcaIncidentId = incident.Id,
            CauseId = request.CauseId,
            EvidenceId = request.EvidenceId,
            CorrectiveActionId = request.CorrectiveActionId,
            ExternalIntakeId = request.ExternalIntakeId,
            FactType = Normalize(request.FactType) ?? "Observation",
            Source = Normalize(request.Source) ?? "Manual",
            SourceDetail = Normalize(request.SourceDetail),
            ExternalSourceSystem = externalSourceSystem,
            ExternalEventId = externalEventId,
            ExternalRecordUri = Normalize(request.ExternalRecordUri),
            FactSeverity = Normalize(request.FactSeverity) ?? "Info",
            ShiftCode = Normalize(request.ShiftCode),
            MachineCode = Normalize(request.MachineCode),
            LineCode = Normalize(request.LineCode),
            WorkOrderCode = Normalize(request.WorkOrderCode),
            MaterialCode = Normalize(request.MaterialCode),
            BatchOrLot = Normalize(request.BatchOrLot),
            AlarmCode = Normalize(request.AlarmCode),
            MeasurementName = Normalize(request.MeasurementName),
            MeasurementValue = request.MeasurementValue,
            MeasurementUnit = Normalize(request.MeasurementUnit),
            Title = request.Title.Trim(),
            Description = Normalize(request.Description),
            OccurredAt = request.OccurredAt ?? DateTimeOffset.UtcNow,
            CapturedByUserId = Normalize(request.CapturedByUserId)
        };

        _dbContext.RcaFacts.Add(fact);
        await AddOutboxEventAsync(
            CreateEvent(
                $"rca-fact-recorded:{fact.Id}",
                "RcaFactRecorded",
                fact.OccurredAt,
                incident,
                new Dictionary<string, string?>
                {
                    ["factId"] = fact.Id.ToString(),
                    ["causeId"] = fact.CauseId?.ToString(),
                    ["evidenceId"] = fact.EvidenceId?.ToString(),
                    ["correctiveActionId"] = fact.CorrectiveActionId?.ToString(),
                    ["externalIntakeId"] = fact.ExternalIntakeId?.ToString(),
                    ["title"] = fact.Title,
                    ["factType"] = fact.FactType,
                    ["source"] = fact.Source,
                    ["sourceDetail"] = fact.SourceDetail,
                    ["externalSourceSystem"] = fact.ExternalSourceSystem,
                    ["externalEventId"] = fact.ExternalEventId,
                    ["externalRecordUri"] = fact.ExternalRecordUri,
                    ["factSeverity"] = fact.FactSeverity,
                    ["shiftCode"] = fact.ShiftCode,
                    ["machineCode"] = fact.MachineCode,
                    ["lineCode"] = fact.LineCode,
                    ["workOrderCode"] = fact.WorkOrderCode,
                    ["materialCode"] = fact.MaterialCode,
                    ["batchOrLot"] = fact.BatchOrLot,
                    ["alarmCode"] = fact.AlarmCode,
                    ["measurementName"] = fact.MeasurementName,
                    ["measurementValue"] = fact.MeasurementValue?.ToString(),
                    ["measurementUnit"] = fact.MeasurementUnit,
                    ["capturedByUserId"] = fact.CapturedByUserId
                }),
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<RcaFactDto>.Ok(ToFactDto(fact), "Hecho agregado a la linea RCA.");
    }

    public async Task<ApiResult<IReadOnlyList<RcaAuditRecordDto>>> ListAuditRecordsAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var incident = await _dbContext.RcaIncidents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

        if (incident is null)
        {
            return ApiResult<IReadOnlyList<RcaAuditRecordDto>>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
        }

        var records = await _dbContext.RcaAuditRecords
            .AsNoTracking()
            .Where(x => x.TenantId == incident.TenantId && x.RcaIncidentId == incidentId && !x.IsDeleted)
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => ToAuditRecordDto(x))
            .ToListAsync(cancellationToken);

        return ApiResult<IReadOnlyList<RcaAuditRecordDto>>.Ok(records);
    }

    public async Task<ApiResult<RcaIncidentDto>> CloseAsync(Guid incidentId, CloseRcaIncidentRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateCloseRequest(request);
        if (validationErrors.Count > 0)
        {
            return ApiResult<RcaIncidentDto>.Fail("No se pudo cerrar el incidente RCA.", validationErrors.ToArray());
        }

        var incident = await _dbContext.RcaIncidents
            .FirstOrDefaultAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

        if (incident is null)
        {
            return ApiResult<RcaIncidentDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
        }

        if (incident.Status == RcaIncidentStatus.Closed)
        {
            return ApiResult<RcaIncidentDto>.Ok(ToDto(incident), "El incidente RCA ya estaba cerrado.");
        }

        var hasRootCause = await _dbContext.IshikawaCauses
            .AsNoTracking()
            .AnyAsync(x => x.RcaIncidentId == incidentId && x.IsRootCause && !x.IsDeleted, cancellationToken);

        if (!hasRootCause)
        {
            return ApiResult<RcaIncidentDto>.Fail(
                "No se pudo cerrar el incidente RCA.",
                new ApiError { Field = "RootCause", Code = "ROOT_CAUSE_REQUIRED", Message = "Debe existir una causa raiz antes del cierre." });
        }

        var hasOpenActions = await _dbContext.CorrectiveActions
            .AsNoTracking()
            .AnyAsync(x =>
                x.RcaIncidentId == incidentId &&
                x.Status != CorrectiveActionStatus.Completed &&
                x.Status != CorrectiveActionStatus.Cancelled &&
                !x.IsDeleted,
                cancellationToken);

        if (hasOpenActions)
        {
            return ApiResult<RcaIncidentDto>.Fail(
                "No se pudo cerrar el incidente RCA.",
                new ApiError { Field = "CorrectiveActions", Code = "OPEN_ACTIONS_EXIST", Message = "Todas las acciones deben estar completadas o canceladas antes del cierre." });
        }

        var actions = await _dbContext.CorrectiveActions
            .AsNoTracking()
            .Where(x => x.RcaIncidentId == incidentId && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var resolutionBlockers = RcaResolutionPolicy.GetResolutionBlockers(actions, HasEscapeAnalysis(actions));
        if (resolutionBlockers.Count > 0)
        {
            return ApiResult<RcaIncidentDto>.Fail(
                "No se pudo cerrar el incidente RCA.",
                resolutionBlockers
                    .Select(x => new ApiError { Field = "CorrectiveActions", Code = "RESOLUTION_ACTIONS_REQUIRED", Message = x })
                    .ToArray());
        }

        var now = DateTimeOffset.UtcNow;
        incident.Status = RcaIncidentStatus.Closed;
        incident.ClosedAt = now;
        incident.ClosedByUserId = Normalize(request.ClosedByUserId);
        incident.ClosureSummary = request.ClosureSummary.Trim();
        incident.UpdatedAt = now;
        incident.UpdatedByUserId = Normalize(request.ClosedByUserId);

        AddAuditRecord(
            incident.TenantId,
            incident.Id,
            nameof(RcaIncident),
            incident.Id,
            "RcaClosed",
            incident.ClosedByUserId,
            "Incidente RCA cerrado formalmente.",
            new
            {
                incident.ClosedAt,
                incident.ClosedByUserId,
                incident.ClosureSummary
            });

        await AddOutboxEventAsync(
            CreateEvent(
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
                }),
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<RcaIncidentDto>.Ok(ToDto(incident), "Incidente RCA cerrado.");
    }

    public async Task<ApiResult<RcaIncidentDto>> EscalateTo8DAsync(Guid incidentId, EscalateRcaIncidentTo8DRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateEscalateTo8DRequest(request);
        if (validationErrors.Count > 0)
        {
            return ApiResult<RcaIncidentDto>.Fail("No se pudo escalar el incidente RCA a 8D.", validationErrors.ToArray());
        }

        var incident = await _dbContext.RcaIncidents
            .FirstOrDefaultAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

        if (incident is null)
        {
            return ApiResult<RcaIncidentDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
        }

        if (incident.Status == RcaIncidentStatus.Closed)
        {
            return ApiResult<RcaIncidentDto>.Fail(
                "No se pudo escalar el incidente RCA a 8D.",
                new ApiError { Field = nameof(incident.Status), Code = "RCA_ALREADY_CLOSED", Message = "Un RCA cerrado no puede escalarse a 8D." });
        }

        var now = DateTimeOffset.UtcNow;
        incident.EscalatedTo8D = true;
        incident.EscalatedTo8DAt ??= now;
        incident.EscalatedTo8DByUserId = Normalize(request.EscalatedByUserId);
        incident.EscalationReason = request.EscalationReason.Trim();
        incident.Status = RcaIncidentStatus.EscalatedTo8D;
        incident.UpdatedAt = now;
        incident.UpdatedByUserId = Normalize(request.EscalatedByUserId);

        AddAuditRecord(
            incident.TenantId,
            incident.Id,
            nameof(RcaIncident),
            incident.Id,
            "RcaEscalatedTo8D",
            incident.EscalatedTo8DByUserId,
            "Incidente RCA escalado formalmente a 8D.",
            new
            {
                incident.EscalatedTo8DAt,
                incident.EscalatedTo8DByUserId,
                incident.EscalationReason
            });

        await AddOutboxEventAsync(
            CreateEvent(
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
                }),
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<RcaIncidentDto>.Ok(ToDto(incident), "Incidente RCA escalado a 8D.");
    }

    public async Task<ApiResult<RcaIncidentDto>> CompleteWizardStepAsync(Guid incidentId, CompleteRcaWizardStepRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateCompleteWizardStepRequest(request);
        if (validationErrors.Count > 0)
        {
            return ApiResult<RcaIncidentDto>.Fail("No se pudo completar la etapa del wizard RCA.", validationErrors.ToArray());
        }

        var incident = await _dbContext.RcaIncidents
            .FirstOrDefaultAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

        if (incident is null)
        {
            return ApiResult<RcaIncidentDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
        }

        var step = ParseWizardStep(request.Step);
        var prerequisiteErrors = await ValidateWizardPrerequisitesAsync(incident, step, cancellationToken);
        if (prerequisiteErrors.Count > 0)
        {
            return ApiResult<RcaIncidentDto>.Fail("No se pudo completar la etapa del wizard RCA.", prerequisiteErrors.ToArray());
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

        await AddOutboxEventAsync(
            CreateEvent(
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
                }),
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<RcaIncidentDto>.Ok(ToDto(incident), "Etapa del wizard RCA completada.");
    }

    public async Task<ApiResult<RcaWizardProgressDto>> GetWizardProgressAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var incident = await _dbContext.RcaIncidents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

        if (incident is null)
        {
            return ApiResult<RcaWizardProgressDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
        }

        var causes = await _dbContext.IshikawaCauses
            .AsNoTracking()
            .Where(x => x.RcaIncidentId == incidentId && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var actions = await _dbContext.CorrectiveActions
            .AsNoTracking()
            .Where(x => x.RcaIncidentId == incidentId && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var evidence = await _dbContext.RcaEvidence
            .AsNoTracking()
            .Where(x => x.RcaIncidentId == incidentId && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        return ApiResult<RcaWizardProgressDto>.Ok(BuildWizardProgress(incident, causes, actions, evidence));
    }

    public async Task<ApiResult<RcaIntegrationSnapshotDto>> GetIntegrationSnapshotAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var incident = await _dbContext.RcaIncidents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

        if (incident is null)
        {
            return ApiResult<RcaIntegrationSnapshotDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
        }

        var snapshot = await BuildIntegrationSnapshotAsync(incident, cancellationToken);

        return ApiResult<RcaIntegrationSnapshotDto>.Ok(snapshot);
    }

    public async Task<ApiResult<IReadOnlyList<RcaIntegrationSnapshotDto>>> ListIntegrationSnapshotsAsync(string? sourceSystem = null, string? externalTaskId = null, string? status = null, CancellationToken cancellationToken = default)
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

        var incidents = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        var snapshots = new List<RcaIntegrationSnapshotDto>();
        foreach (var incident in incidents)
        {
            snapshots.Add(await BuildIntegrationSnapshotAsync(incident, cancellationToken));
        }

        return ApiResult<IReadOnlyList<RcaIntegrationSnapshotDto>>.Ok(snapshots);
    }

    public async Task<ApiResult<IReadOnlyList<RcaDomainEventDto>>> ListIntegrationEventsAsync(Guid? incidentId = null, DateTimeOffset? since = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RcaIncidents
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (incidentId.HasValue)
        {
            query = query.Where(x => x.Id == incidentId.Value);
        }

        var incidents = await query
            .OrderBy(x => x.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        var events = new List<RcaDomainEventDto>();
        foreach (var incident in incidents)
        {
            events.Add(CreateEvent(
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
                }));

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

            var causes = await _dbContext.IshikawaCauses
                .AsNoTracking()
                .Where(x => x.RcaIncidentId == incident.Id && !x.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var cause in causes)
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

            var actions = await _dbContext.CorrectiveActions
                .AsNoTracking()
                .Where(x => x.RcaIncidentId == incident.Id && !x.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var action in actions)
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

            var evidence = await _dbContext.RcaEvidence
                .AsNoTracking()
                .Where(x => x.RcaIncidentId == incident.Id && !x.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var evidenceItem in evidence)
            {
                events.Add(CreateEvent(
                    $"rca-evidence-attached:{evidenceItem.Id}",
                    "RcaEvidenceAttached",
                    evidenceItem.CreatedAt,
                    incident,
                    new Dictionary<string, string?>
                    {
                        ["evidenceId"] = evidenceItem.Id.ToString(),
                        ["causeId"] = evidenceItem.CauseId?.ToString(),
                        ["externalIntakeId"] = evidenceItem.ExternalIntakeId?.ToString(),
                        ["title"] = evidenceItem.Title,
                        ["evidenceType"] = evidenceItem.EvidenceType,
                        ["source"] = evidenceItem.Source,
                        ["sourceDetail"] = evidenceItem.SourceDetail,
                        ["tags"] = evidenceItem.Tags,
                        ["validationStatus"] = evidenceItem.ValidationStatus,
                        ["validatedByUserId"] = evidenceItem.ValidatedByUserId,
                        ["referenceUri"] = evidenceItem.ReferenceUri,
                        ["attachmentFileName"] = evidenceItem.AttachmentFileName,
                        ["attachmentContentType"] = evidenceItem.AttachmentContentType,
                        ["attachmentSizeBytes"] = evidenceItem.AttachmentSizeBytes?.ToString(),
                        ["attachmentStorageProvider"] = evidenceItem.AttachmentStorageProvider,
                        ["attachmentSha256"] = evidenceItem.AttachmentSha256
                    }));
            }

            var facts = await _dbContext.RcaFacts
                .AsNoTracking()
                .Where(x => x.RcaIncidentId == incident.Id && !x.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var fact in facts)
            {
                events.Add(CreateEvent(
                    $"rca-fact-recorded:{fact.Id}",
                    "RcaFactRecorded",
                    fact.OccurredAt,
                    incident,
                    new Dictionary<string, string?>
                    {
                        ["factId"] = fact.Id.ToString(),
                        ["causeId"] = fact.CauseId?.ToString(),
                        ["evidenceId"] = fact.EvidenceId?.ToString(),
                        ["correctiveActionId"] = fact.CorrectiveActionId?.ToString(),
                        ["externalIntakeId"] = fact.ExternalIntakeId?.ToString(),
                        ["title"] = fact.Title,
                        ["factType"] = fact.FactType,
                        ["source"] = fact.Source,
                        ["sourceDetail"] = fact.SourceDetail,
                        ["externalSourceSystem"] = fact.ExternalSourceSystem,
                        ["externalEventId"] = fact.ExternalEventId,
                        ["externalRecordUri"] = fact.ExternalRecordUri,
                        ["factSeverity"] = fact.FactSeverity,
                        ["shiftCode"] = fact.ShiftCode,
                        ["machineCode"] = fact.MachineCode,
                        ["lineCode"] = fact.LineCode,
                        ["workOrderCode"] = fact.WorkOrderCode,
                        ["materialCode"] = fact.MaterialCode,
                        ["batchOrLot"] = fact.BatchOrLot,
                        ["alarmCode"] = fact.AlarmCode,
                        ["measurementName"] = fact.MeasurementName,
                        ["measurementValue"] = fact.MeasurementValue?.ToString(),
                        ["measurementUnit"] = fact.MeasurementUnit,
                        ["capturedByUserId"] = fact.CapturedByUserId
                    }));
            }

            var externalIntakes = await _dbContext.RcaExternalIntakeRequests
                .AsNoTracking()
                .Where(x => x.RcaIncidentId == incident.Id && !x.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var intake in externalIntakes)
            {
                AddExternalIntakeEvents(events, incident, intake);
            }
        }

        var filteredEvents = events
            .Where(x => !since.HasValue || x.OccurredAt >= since.Value)
            .OrderBy(x => x.OccurredAt)
            .ToList();

        return ApiResult<IReadOnlyList<RcaDomainEventDto>>.Ok(filteredEvents);
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

        if (!Enum.TryParse<CorrectiveActionType>(request.ActionType, true, out _))
        {
            errors.Add(new ApiError { Field = nameof(request.ActionType), Code = "INVALID_ACTION_TYPE", Message = "El tipo debe ser Corrective, Preventive o RecurrencePreventive." });
        }

        if (!Enum.TryParse<RcaResolutionScope>(request.ResolutionScope, true, out _))
        {
            errors.Add(new ApiError { Field = nameof(request.ResolutionScope), Code = "INVALID_RESOLUTION_SCOPE", Message = "El ambito debe ser RootCause o Escape." });
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

    private static List<ApiError> ValidateAddFactRequest(AddRcaFactRequest request)
    {
        var errors = new List<ApiError>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add(new ApiError { Field = nameof(request.Title), Code = "FACT_TITLE_REQUIRED", Message = "El titulo del hecho es obligatorio." });
        }

        var hasExternalSource = !string.IsNullOrWhiteSpace(request.ExternalSourceSystem);
        var hasExternalEvent = !string.IsNullOrWhiteSpace(request.ExternalEventId);
        if (hasExternalSource != hasExternalEvent)
        {
            errors.Add(new ApiError { Field = nameof(request.ExternalEventId), Code = "EXTERNAL_FACT_CORRELATION_INCOMPLETE", Message = "Para hechos externos se requieren ExternalSourceSystem y ExternalEventId." });
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

    private async Task<List<ApiError>> ValidateWizardPrerequisitesAsync(RcaIncident incident, RcaWizardStep step, CancellationToken cancellationToken)
    {
        var errors = new List<ApiError>();
        var incidentId = incident.Id;

        if (step >= RcaWizardStep.Causes)
        {
            var hasCause = await _dbContext.IshikawaCauses
                .AsNoTracking()
                .AnyAsync(x => x.RcaIncidentId == incidentId && !x.IsDeleted, cancellationToken);

            if (!hasCause)
            {
                errors.Add(new ApiError { Field = "Causes", Code = "CAUSE_REQUIRED", Message = "Debe cargar al menos una causa para avanzar el wizard." });
            }
        }

        if (step >= RcaWizardStep.Evidence)
        {
            var hasEvidence = await _dbContext.RcaEvidence
                .AsNoTracking()
                .AnyAsync(x => x.RcaIncidentId == incidentId && !x.IsDeleted, cancellationToken);

            if (!hasEvidence)
            {
                errors.Add(new ApiError { Field = "Evidence", Code = "EVIDENCE_REQUIRED", Message = "Debe registrar al menos una evidencia para avanzar el wizard." });
            }
        }

        if (step >= RcaWizardStep.Actions)
        {
            var hasRootCause = await _dbContext.IshikawaCauses
                .AsNoTracking()
                .AnyAsync(x => x.RcaIncidentId == incidentId && x.IsRootCause && !x.IsDeleted, cancellationToken);

            if (!hasRootCause)
            {
                errors.Add(new ApiError { Field = "RootCause", Code = "ROOT_CAUSE_REQUIRED", Message = "Debe marcar una causa raiz para avanzar a acciones." });
            }

            var hasAction = await _dbContext.CorrectiveActions
                .AsNoTracking()
                .AnyAsync(x => x.RcaIncidentId == incidentId && !x.IsDeleted, cancellationToken);

            if (!hasAction)
            {
                errors.Add(new ApiError { Field = "CorrectiveActions", Code = "ACTION_REQUIRED", Message = "Debe registrar al menos una accion correctiva para avanzar el wizard." });
            }
        }

        if (step >= RcaWizardStep.Validation)
        {
            var hasValidatedEvidence = await _dbContext.RcaEvidence
                .AsNoTracking()
                .AnyAsync(x =>
                    x.RcaIncidentId == incidentId &&
                    x.ValidationStatus == "Validated" &&
                    !x.IsDeleted,
                    cancellationToken);

            if (!hasValidatedEvidence)
            {
                errors.Add(new ApiError { Field = "Evidence", Code = "VALIDATED_EVIDENCE_REQUIRED", Message = "Debe existir al menos una evidencia validada para avanzar a validacion." });
            }

            var hasOpenActions = await _dbContext.CorrectiveActions
                .AsNoTracking()
                .AnyAsync(x =>
                    x.RcaIncidentId == incidentId &&
                    x.Status != CorrectiveActionStatus.Completed &&
                    x.Status != CorrectiveActionStatus.Cancelled &&
                    !x.IsDeleted,
                    cancellationToken);

            if (hasOpenActions)
            {
                errors.Add(new ApiError { Field = "CorrectiveActions", Code = "OPEN_ACTIONS_EXIST", Message = "Todas las acciones deben estar completadas o canceladas para validar el wizard." });
            }

            var actions = await _dbContext.CorrectiveActions
                .AsNoTracking()
                .Where(x => x.RcaIncidentId == incidentId && !x.IsDeleted)
                .ToListAsync(cancellationToken);

            errors.AddRange(RcaResolutionPolicy
                .GetResolutionBlockers(actions, HasEscapeAnalysis(actions))
                .Select(x => new ApiError { Field = "CorrectiveActions", Code = "RESOLUTION_ACTIONS_REQUIRED", Message = x }));
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

    private static CorrectiveActionType ParseCorrectiveActionType(string actionType)
    {
        return Enum.TryParse<CorrectiveActionType>(actionType, true, out var parsed)
            ? parsed
            : CorrectiveActionType.Corrective;
    }

    private static RcaResolutionScope ParseResolutionScope(string resolutionScope)
    {
        return Enum.TryParse<RcaResolutionScope>(resolutionScope, true, out var parsed)
            ? parsed
            : RcaResolutionScope.RootCause;
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

    private void AddAuditRecord(
        Guid tenantId,
        Guid incidentId,
        string entityType,
        Guid entityId,
        string action,
        string? userId,
        string summary,
        object? data)
    {
        _dbContext.RcaAuditRecords.Add(new RcaAuditRecord
        {
            TenantId = tenantId,
            RcaIncidentId = incidentId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserId = Normalize(userId),
            OccurredAt = DateTimeOffset.UtcNow,
            Summary = summary,
            DataJson = data is null ? null : JsonSerializer.Serialize(data)
        });
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
            ActionType = action.ActionType.ToString(),
            ResolutionScope = action.ResolutionScope.ToString(),
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

    private static RcaAuditRecordDto ToAuditRecordDto(RcaAuditRecord record)
    {
        return new RcaAuditRecordDto
        {
            Id = record.Id,
            TenantId = record.TenantId,
            RcaIncidentId = record.RcaIncidentId,
            EntityType = record.EntityType,
            EntityId = record.EntityId,
            Action = record.Action,
            UserId = record.UserId,
            OccurredAt = record.OccurredAt,
            Summary = record.Summary,
            DataJson = record.DataJson
        };
    }

    private static RcaFactDto ToFactDto(RcaFact fact)
    {
        return new RcaFactDto
        {
            Id = fact.Id,
            RcaIncidentId = fact.RcaIncidentId,
            CauseId = fact.CauseId,
            EvidenceId = fact.EvidenceId,
            CorrectiveActionId = fact.CorrectiveActionId,
            ExternalIntakeId = fact.ExternalIntakeId,
            FactType = fact.FactType,
            Source = fact.Source,
            SourceDetail = fact.SourceDetail,
            ExternalSourceSystem = fact.ExternalSourceSystem,
            ExternalEventId = fact.ExternalEventId,
            ExternalRecordUri = fact.ExternalRecordUri,
            FactSeverity = fact.FactSeverity,
            ShiftCode = fact.ShiftCode,
            MachineCode = fact.MachineCode,
            LineCode = fact.LineCode,
            WorkOrderCode = fact.WorkOrderCode,
            MaterialCode = fact.MaterialCode,
            BatchOrLot = fact.BatchOrLot,
            AlarmCode = fact.AlarmCode,
            MeasurementName = fact.MeasurementName,
            MeasurementValue = fact.MeasurementValue,
            MeasurementUnit = fact.MeasurementUnit,
            Title = fact.Title,
            Description = fact.Description,
            OccurredAt = fact.OccurredAt,
            CapturedByUserId = fact.CapturedByUserId,
            CreatedAt = fact.CreatedAt
        };
    }

    private async Task<RcaIntegrationSnapshotDto> BuildIntegrationSnapshotAsync(RcaIncident incident, CancellationToken cancellationToken)
    {
        var causes = await _dbContext.IshikawaCauses
            .AsNoTracking()
            .Where(x => x.RcaIncidentId == incident.Id && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var actions = await _dbContext.CorrectiveActions
            .AsNoTracking()
            .Where(x => x.RcaIncidentId == incident.Id && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var evidence = await _dbContext.RcaEvidence
            .AsNoTracking()
            .Where(x => x.RcaIncidentId == incident.Id && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var rootCause = causes
            .Where(x => x.IsRootCause)
            .OrderByDescending(x => x.ImpactScore + x.ProbabilityScore + x.FrequencyScore)
            .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .FirstOrDefault();

        var openActions = actions
            .Where(x => x.Status is not CorrectiveActionStatus.Completed and not CorrectiveActionStatus.Cancelled)
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
            LastUpdatedAt = GetLastUpdatedAt(incident, causes, actions, evidence),
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
            EvidenceCount = evidence.Count,
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

        if (step >= RcaWizardStep.Validation)
        {
            blockers.AddRange(RcaResolutionPolicy.GetResolutionBlockers(actions, HasEscapeAnalysis(actions)));
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

    private async Task AddOutboxEventAsync(RcaDomainEventDto integrationEvent, CancellationToken cancellationToken)
    {
        var alreadyTracked = _dbContext.RcaOutboxEvents.Local.Any(x =>
            x.TenantId == integrationEvent.TenantId &&
            x.EventId == integrationEvent.Id &&
            !x.IsDeleted);

        if (alreadyTracked)
        {
            return;
        }

        var exists = await _dbContext.RcaOutboxEvents
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == integrationEvent.TenantId &&
                    x.EventId == integrationEvent.Id &&
                    !x.IsDeleted,
                cancellationToken);

        if (exists)
        {
            return;
        }

        _dbContext.RcaOutboxEvents.Add(new RcaOutboxEvent
        {
            TenantId = integrationEvent.TenantId,
            EventId = integrationEvent.Id,
            EventType = integrationEvent.Type,
            OccurredAt = integrationEvent.OccurredAt,
            IncidentId = integrationEvent.IncidentId,
            SourceSystem = integrationEvent.SourceSystem,
            ExternalTaskId = integrationEvent.ExternalTaskId,
            ExternalEventId = integrationEvent.ExternalEventId,
            ExternalWorkOrderId = integrationEvent.ExternalWorkOrderId,
            PayloadJson = JsonSerializer.Serialize(integrationEvent, OutboxSerializerOptions),
            Status = RcaOutboxEventStatus.Pending
        });
    }

    private static void AddExternalIntakeEvents(List<RcaDomainEventDto> events, RcaIncident incident, RcaExternalIntakeRequest intake)
    {
        var data = CreateExternalIntakeEventData(intake);

        events.Add(CreateEvent(
            $"rca-external-intake-created:{intake.Id}",
            "RcaExternalIntakeCreated",
            intake.CreatedAt,
            incident,
            data));

        if (intake.OpenedAt.HasValue)
        {
            events.Add(CreateEvent(
                $"rca-external-intake-opened:{intake.Id}",
                "RcaExternalIntakeOpened",
                intake.OpenedAt.Value,
                incident,
                data));
        }

        if (intake.SubmittedAt.HasValue)
        {
            events.Add(CreateEvent(
                $"rca-external-intake-submitted:{intake.Id}",
                "RcaExternalIntakeSubmitted",
                intake.SubmittedAt.Value,
                incident,
                data));
        }

        if (intake.ReviewedAt.HasValue)
        {
            events.Add(CreateEvent(
                $"rca-external-intake-reviewed:{intake.Id}",
                "RcaExternalIntakeReviewed",
                intake.ReviewedAt.Value,
                incident,
                data));
        }

        if (intake.RejectedAt.HasValue)
        {
            events.Add(CreateEvent(
                $"rca-external-intake-rejected:{intake.Id}",
                "RcaExternalIntakeRejected",
                intake.RejectedAt.Value,
                incident,
                data));
        }

        if (intake.Status == RcaExternalIntakeStatus.Revoked)
        {
            events.Add(CreateEvent(
                $"rca-external-intake-revoked:{intake.Id}",
                "RcaExternalIntakeRevoked",
                intake.UpdatedAt ?? intake.CreatedAt,
                incident,
                data));
        }

        if (intake.Status == RcaExternalIntakeStatus.Expired)
        {
            events.Add(CreateEvent(
                $"rca-external-intake-expired:{intake.Id}",
                "RcaExternalIntakeExpired",
                intake.UpdatedAt ?? intake.ExpiresAt,
                incident,
                data));
        }
    }

    private static Dictionary<string, string?> CreateExternalIntakeEventData(RcaExternalIntakeRequest intake)
    {
        return new Dictionary<string, string?>
        {
            ["intakeId"] = intake.Id.ToString(),
            ["actorType"] = intake.ActorType.ToString(),
            ["actorName"] = intake.ActorName,
            ["contactEmail"] = intake.ContactEmail,
            ["status"] = intake.Status.ToString(),
            ["expiresAt"] = intake.ExpiresAt.ToString("O"),
            ["submittedAt"] = intake.SubmittedAt?.ToString("O"),
            ["reviewedAt"] = intake.ReviewedAt?.ToString("O"),
            ["reviewedByUserId"] = intake.ReviewedByUserId,
            ["rejectedAt"] = intake.RejectedAt?.ToString("O"),
            ["rejectedByUserId"] = intake.RejectedByUserId,
            ["rejectionReason"] = intake.RejectionReason,
            ["claimReference"] = intake.ClaimReference,
            ["materialCode"] = intake.MaterialCode,
            ["batchOrLot"] = intake.BatchOrLot,
            ["hasProposedRootCause"] = (!string.IsNullOrWhiteSpace(intake.ProposedRootCause)).ToString(),
            ["hasProposedCorrectiveAction"] = (!string.IsNullOrWhiteSpace(intake.ProposedCorrectiveAction)).ToString(),
            ["hasEvidenceSummary"] = (!string.IsNullOrWhiteSpace(intake.EvidenceSummary)).ToString()
        };
    }

    private static DateTimeOffset GetLastUpdatedAt(RcaIncident incident, IReadOnlyList<IshikawaCause> causes, IReadOnlyList<CorrectiveAction> actions, IReadOnlyList<RcaEvidence> evidence)
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

    private static bool HasEscapeAnalysis(IReadOnlyCollection<CorrectiveAction> actions)
    {
        return actions.Any(x => x.ResolutionScope == RcaResolutionScope.Escape && !x.IsDeleted);
    }

    private static ApiResult<IshikawaCanvasDto> NotFoundCanvas(Guid incidentId)
    {
        return ApiResult<IshikawaCanvasDto>.Fail(
            "No se encontro el incidente RCA.",
            new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
    }
}
