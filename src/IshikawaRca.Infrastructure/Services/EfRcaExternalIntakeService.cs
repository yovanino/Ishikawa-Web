using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Entities;
using IshikawaRca.Domain.Enums;
using IshikawaRca.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IshikawaRca.Infrastructure.Services;

public class EfRcaExternalIntakeService : IRcaExternalIntakeService
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromDays(14);
    private static readonly JsonSerializerOptions OutboxSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RcaDbContext _dbContext;

    public EfRcaExternalIntakeService(RcaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResult<CreatedExternalIntakeDto>> CreateAsync(Guid incidentId, CreateExternalIntakeRequest request, CancellationToken cancellationToken = default)
    {
        var incident = await _dbContext.RcaIncidents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

        if (incident is null)
        {
            return ApiResult<CreatedExternalIntakeDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
        }

        var errors = ValidateCreate(request);
        if (errors.Count > 0)
        {
            return ApiResult<CreatedExternalIntakeDto>.Fail("No se pudo crear el link externo.", errors.ToArray());
        }

        var token = CreateToken();
        var actorType = ParseActorType(request.ActorType);
        var intake = new RcaExternalIntakeRequest
        {
            TenantId = incident.TenantId,
            RcaIncidentId = incident.Id,
            ActorType = actorType,
            ActorName = Normalize(request.ActorName) ?? incident.ClaimOwnerName,
            ContactName = Normalize(request.ContactName),
            ContactEmail = Normalize(request.ContactEmail),
            TokenHash = HashToken(token),
            ExpiresAt = request.ExpiresAt ?? DateTimeOffset.UtcNow.Add(DefaultExpiration),
            Status = RcaExternalIntakeStatus.Sent
        };

        _dbContext.RcaExternalIntakeRequests.Add(intake);
        await AddOutboxEventAsync(
            CreateExternalIntakeEvent(
                $"rca-external-intake-created:{intake.Id}",
                "RcaExternalIntakeCreated",
                intake.CreatedAt,
                incident,
                intake),
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<CreatedExternalIntakeDto>.Ok(
            new CreatedExternalIntakeDto
            {
                Intake = ToDto(intake, incident.Title),
                Token = token
            },
            "Link externo creado.");
    }

    public async Task<ApiResult<IReadOnlyList<RcaExternalIntakeDto>>> ListByIncidentAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var incident = await _dbContext.RcaIncidents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

        if (incident is null)
        {
            return ApiResult<IReadOnlyList<RcaExternalIntakeDto>>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
        }

        var intakes = await _dbContext.RcaExternalIntakeRequests
            .AsNoTracking()
            .Where(x => x.RcaIncidentId == incidentId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToDto(x, incident.Title))
            .ToListAsync(cancellationToken);

        return ApiResult<IReadOnlyList<RcaExternalIntakeDto>>.Ok(intakes);
    }

    public async Task<ApiResult<RcaExternalIntakeDto>> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var result = await FindByTokenAsync(token, track: true, cancellationToken);
        if (!result.Success || result.Intake is null || result.IncidentTitle is null)
        {
            return ApiResult<RcaExternalIntakeDto>.Fail("Link externo invalido.", result.Error);
        }

        if (result.Intake.ExpiresAt < DateTimeOffset.UtcNow && result.Intake.Status is not RcaExternalIntakeStatus.Submitted and not RcaExternalIntakeStatus.Reviewed and not RcaExternalIntakeStatus.Rejected)
        {
            result.Intake.Status = RcaExternalIntakeStatus.Expired;
            var incident = await GetIncidentAsync(result.Intake.RcaIncidentId, cancellationToken);
            if (incident is not null)
            {
                await AddOutboxEventAsync(
                    CreateExternalIntakeEvent(
                        $"rca-external-intake-expired:{result.Intake.Id}",
                        "RcaExternalIntakeExpired",
                        result.Intake.UpdatedAt ?? result.Intake.ExpiresAt,
                        incident,
                        result.Intake),
                    cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return ApiResult<RcaExternalIntakeDto>.Fail(
                "El link externo expiro.",
                new ApiError { Field = nameof(token), Code = "INTAKE_EXPIRED", Message = "Solicita un nuevo link al equipo interno." });
        }

        if (result.Intake.Status == RcaExternalIntakeStatus.Revoked)
        {
            return ApiResult<RcaExternalIntakeDto>.Fail(
                "El link externo fue revocado.",
                new ApiError { Field = nameof(token), Code = "INTAKE_REVOKED", Message = "El link ya no esta habilitado." });
        }

        if (result.Intake.Status == RcaExternalIntakeStatus.Rejected)
        {
            return ApiResult<RcaExternalIntakeDto>.Fail(
                "El link externo fue rechazado internamente.",
                new ApiError { Field = nameof(token), Code = "INTAKE_REJECTED", Message = "La respuesta fue cerrada por el equipo interno." });
        }

        if (result.Intake.Status == RcaExternalIntakeStatus.Sent)
        {
            result.Intake.Status = RcaExternalIntakeStatus.Opened;
            result.Intake.OpenedAt = DateTimeOffset.UtcNow;
            var incident = await GetIncidentAsync(result.Intake.RcaIncidentId, cancellationToken);
            if (incident is not null)
            {
                await AddOutboxEventAsync(
                    CreateExternalIntakeEvent(
                        $"rca-external-intake-opened:{result.Intake.Id}",
                        "RcaExternalIntakeOpened",
                        result.Intake.OpenedAt.Value,
                        incident,
                        result.Intake),
                    cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return ApiResult<RcaExternalIntakeDto>.Ok(ToDto(result.Intake, result.IncidentTitle));
    }

    public async Task<ApiResult<RcaExternalIntakeDto>> SubmitAsync(string token, SubmitExternalIntakeRequest request, CancellationToken cancellationToken = default)
    {
        var result = await FindByTokenAsync(token, track: true, cancellationToken);
        if (!result.Success || result.Intake is null || result.IncidentTitle is null)
        {
            return ApiResult<RcaExternalIntakeDto>.Fail("Link externo invalido.", result.Error);
        }

        if (result.Intake.ExpiresAt < DateTimeOffset.UtcNow)
        {
            result.Intake.Status = RcaExternalIntakeStatus.Expired;
            var incident = await GetIncidentAsync(result.Intake.RcaIncidentId, cancellationToken);
            if (incident is not null)
            {
                await AddOutboxEventAsync(
                    CreateExternalIntakeEvent(
                        $"rca-external-intake-expired:{result.Intake.Id}",
                        "RcaExternalIntakeExpired",
                        result.Intake.UpdatedAt ?? result.Intake.ExpiresAt,
                        incident,
                        result.Intake),
                    cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return ApiResult<RcaExternalIntakeDto>.Fail(
                "El link externo expiro.",
                new ApiError { Field = nameof(token), Code = "INTAKE_EXPIRED", Message = "Solicita un nuevo link al equipo interno." });
        }

        if (result.Intake.Status is RcaExternalIntakeStatus.Revoked or RcaExternalIntakeStatus.Submitted or RcaExternalIntakeStatus.Reviewed or RcaExternalIntakeStatus.Rejected)
        {
            return ApiResult<RcaExternalIntakeDto>.Fail(
                "El link externo no acepta nuevas respuestas.",
                new ApiError { Field = nameof(token), Code = "INTAKE_CLOSED", Message = "La solicitud ya fue cerrada o revocada." });
        }

        var errors = ValidateSubmit(request);
        if (errors.Count > 0)
        {
            return ApiResult<RcaExternalIntakeDto>.Fail("No se pudo enviar la respuesta externa.", errors.ToArray());
        }

        result.Intake.ContactName = Normalize(request.ContactName) ?? result.Intake.ContactName;
        result.Intake.ContactEmail = Normalize(request.ContactEmail) ?? result.Intake.ContactEmail;
        result.Intake.ClaimReference = Normalize(request.ClaimReference);
        result.Intake.MaterialCode = Normalize(request.MaterialCode);
        result.Intake.BatchOrLot = Normalize(request.BatchOrLot);
        result.Intake.Description = request.Description.Trim();
        result.Intake.ContainmentResponse = Normalize(request.ContainmentResponse);
        result.Intake.ProposedRootCause = Normalize(request.ProposedRootCause);
        result.Intake.ProposedCorrectiveAction = Normalize(request.ProposedCorrectiveAction);
        result.Intake.EvidenceSummary = Normalize(request.EvidenceSummary);
        result.Intake.SubmittedAt = DateTimeOffset.UtcNow;
        result.Intake.Status = RcaExternalIntakeStatus.Submitted;
        result.Intake.UpdatedAt = DateTimeOffset.UtcNow;

        var submitIncident = await GetIncidentAsync(result.Intake.RcaIncidentId, cancellationToken);
        if (submitIncident is not null)
        {
            await AddOutboxEventAsync(
                CreateExternalIntakeEvent(
                    $"rca-external-intake-submitted:{result.Intake.Id}",
                    "RcaExternalIntakeSubmitted",
                    result.Intake.SubmittedAt.Value,
                    submitIncident,
                    result.Intake),
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<RcaExternalIntakeDto>.Ok(ToDto(result.Intake, result.IncidentTitle), "Respuesta externa enviada.");
    }

    public async Task<ApiResult<RcaExternalIntakeDto>> ReviewAsync(Guid intakeId, ReviewExternalIntakeRequest request, CancellationToken cancellationToken = default)
    {
        var intake = await _dbContext.RcaExternalIntakeRequests
            .FirstOrDefaultAsync(x => x.Id == intakeId && !x.IsDeleted, cancellationToken);

        if (intake is null)
        {
            return ApiResult<RcaExternalIntakeDto>.Fail(
                "No se encontro la respuesta externa.",
                new ApiError { Field = nameof(intakeId), Code = "INTAKE_NOT_FOUND", Message = "El identificador no corresponde a una solicitud activa." });
        }

        var incident = await _dbContext.RcaIncidents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == intake.RcaIncidentId && !x.IsDeleted, cancellationToken);

        if (incident is null)
        {
            return ApiResult<RcaExternalIntakeDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(intakeId), Code = "RCA_NOT_FOUND", Message = "El RCA asociado ya no esta disponible." });
        }

        if (intake.Status != RcaExternalIntakeStatus.Submitted)
        {
            return ApiResult<RcaExternalIntakeDto>.Fail(
                "La respuesta externa no esta pendiente de revision.",
                new ApiError { Field = nameof(intake.Status), Code = "INTAKE_NOT_SUBMITTED", Message = "Solo se pueden revisar respuestas enviadas." });
        }

        var errors = ValidateReview(request, intake);
        if (errors.Count > 0)
        {
            return ApiResult<RcaExternalIntakeDto>.Fail("No se pudo revisar la respuesta externa.", errors.ToArray());
        }

        IshikawaCause? importedCause = null;
        if (request.ImportCause)
        {
            var branch = await _dbContext.IshikawaBranches
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.BranchId && x.RcaIncidentId == intake.RcaIncidentId && !x.IsDeleted, cancellationToken);

            if (branch is null)
            {
                return ApiResult<RcaExternalIntakeDto>.Fail(
                    "No se pudo importar la causa externa.",
                    new ApiError { Field = nameof(request.BranchId), Code = "BRANCH_NOT_FOUND", Message = "La rama seleccionada no corresponde al RCA." });
            }

            importedCause = new IshikawaCause
            {
                TenantId = intake.TenantId,
                RcaIncidentId = intake.RcaIncidentId,
                BranchId = request.BranchId,
                Title = ToTitle(intake.ProposedRootCause, "Causa propuesta externa"),
                Description = TrimToMax(BuildCauseDescription(intake), 4000),
                ProbabilityScore = 3,
                ImpactScore = 3,
                FrequencyScore = 1,
                IsRootCause = request.MarkCauseAsRoot,
                EvidenceSummary = TrimToMax(BuildEvidenceSummary(intake), 4000)
            };

            _dbContext.IshikawaCauses.Add(importedCause);
        }

        if (request.ImportCorrectiveAction)
        {
            var action = new CorrectiveAction
            {
                TenantId = intake.TenantId,
                RcaIncidentId = intake.RcaIncidentId,
                CauseId = importedCause?.Id,
                Title = ToTitle(intake.ProposedCorrectiveAction, "Accion propuesta externa"),
                Description = TrimToMax(BuildActionDescription(intake), 4000),
                Status = CorrectiveActionStatus.Open
            };

            _dbContext.CorrectiveActions.Add(action);
        }

        intake.Status = RcaExternalIntakeStatus.Reviewed;
        intake.ReviewedAt = DateTimeOffset.UtcNow;
        intake.ReviewedByUserId = Normalize(request.ReviewedByUserId);
        intake.UpdatedAt = DateTimeOffset.UtcNow;
        intake.UpdatedByUserId = intake.ReviewedByUserId;

        AddAuditRecord(
            intake.TenantId,
            intake.RcaIncidentId,
            intake.Id,
            "RcaExternalIntakeReviewed",
            intake.ReviewedByUserId,
            "Respuesta externa revisada internamente.",
            new
            {
                request.ImportCause,
                request.MarkCauseAsRoot,
                request.ImportCorrectiveAction,
                importedCauseId = importedCause?.Id
            });

        await AddOutboxEventAsync(
            CreateExternalIntakeEvent(
                $"rca-external-intake-reviewed:{intake.Id}",
                "RcaExternalIntakeReviewed",
                intake.ReviewedAt.Value,
                incident,
                intake),
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<RcaExternalIntakeDto>.Ok(ToDto(intake, incident.Title), "Respuesta externa revisada.");
    }

    public async Task<ApiResult<RcaExternalIntakeDto>> RejectAsync(Guid intakeId, RejectExternalIntakeRequest request, CancellationToken cancellationToken = default)
    {
        var intake = await _dbContext.RcaExternalIntakeRequests
            .FirstOrDefaultAsync(x => x.Id == intakeId && !x.IsDeleted, cancellationToken);

        if (intake is null)
        {
            return ApiResult<RcaExternalIntakeDto>.Fail(
                "No se encontro la respuesta externa.",
                new ApiError { Field = nameof(intakeId), Code = "INTAKE_NOT_FOUND", Message = "El identificador no corresponde a una solicitud activa." });
        }

        var incident = await _dbContext.RcaIncidents
            .AsNoTracking()
            .Where(x => x.Id == intake.RcaIncidentId && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (incident is null)
        {
            return ApiResult<RcaExternalIntakeDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(intakeId), Code = "RCA_NOT_FOUND", Message = "El RCA asociado ya no esta disponible." });
        }

        if (intake.Status != RcaExternalIntakeStatus.Submitted)
        {
            return ApiResult<RcaExternalIntakeDto>.Fail(
                "La respuesta externa no esta pendiente de revision.",
                new ApiError { Field = nameof(intake.Status), Code = "INTAKE_NOT_SUBMITTED", Message = "Solo se pueden rechazar respuestas enviadas." });
        }

        var errors = ValidateReject(request);
        if (errors.Count > 0)
        {
            return ApiResult<RcaExternalIntakeDto>.Fail("No se pudo rechazar la respuesta externa.", errors.ToArray());
        }

        intake.Status = RcaExternalIntakeStatus.Rejected;
        intake.RejectedAt = DateTimeOffset.UtcNow;
        intake.RejectedByUserId = Normalize(request.RejectedByUserId);
        intake.RejectionReason = TrimToMax(request.RejectionReason.Trim(), 1000);
        intake.UpdatedAt = DateTimeOffset.UtcNow;
        intake.UpdatedByUserId = intake.RejectedByUserId;

        AddAuditRecord(
            intake.TenantId,
            intake.RcaIncidentId,
            intake.Id,
            "RcaExternalIntakeRejected",
            intake.RejectedByUserId,
            "Respuesta externa rechazada internamente.",
            new
            {
                intake.RejectedAt,
                intake.RejectionReason
            });

        await AddOutboxEventAsync(
            CreateExternalIntakeEvent(
                $"rca-external-intake-rejected:{intake.Id}",
                "RcaExternalIntakeRejected",
                intake.RejectedAt.Value,
                incident,
                intake),
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<RcaExternalIntakeDto>.Ok(ToDto(intake, incident.Title), "Respuesta externa rechazada.");
    }

    public async Task<ApiResult<RcaExternalIntakeDto>> RevokeAsync(Guid intakeId, string? revokedByUserId = null, CancellationToken cancellationToken = default)
    {
        var intake = await _dbContext.RcaExternalIntakeRequests
            .FirstOrDefaultAsync(x => x.Id == intakeId && !x.IsDeleted, cancellationToken);

        if (intake is null)
        {
            return ApiResult<RcaExternalIntakeDto>.Fail(
                "No se encontro el link externo.",
                new ApiError { Field = nameof(intakeId), Code = "INTAKE_NOT_FOUND", Message = "El identificador no corresponde a un intake activo." });
        }

        var incident = await _dbContext.RcaIncidents
            .AsNoTracking()
            .Where(x => x.Id == intake.RcaIncidentId)
            .FirstOrDefaultAsync(cancellationToken);

        var incidentTitle = incident?.Title ?? string.Empty;

        if (intake.Status is RcaExternalIntakeStatus.Submitted or RcaExternalIntakeStatus.Reviewed or RcaExternalIntakeStatus.Rejected)
        {
            return ApiResult<RcaExternalIntakeDto>.Fail(
                "El link externo ya tiene una respuesta cerrada.",
                new ApiError { Field = nameof(intake.Status), Code = "INTAKE_CLOSED", Message = "Usa revision o rechazo formal para respuestas enviadas." });
        }

        var previousStatus = intake.Status;
        intake.Status = RcaExternalIntakeStatus.Revoked;
        intake.UpdatedAt = DateTimeOffset.UtcNow;
        intake.UpdatedByUserId = Normalize(revokedByUserId);

        AddAuditRecord(
            intake.TenantId,
            intake.RcaIncidentId,
            intake.Id,
            "RcaExternalIntakeRevoked",
            intake.UpdatedByUserId,
            "Link externo revocado internamente.",
            new
            {
                intake.ExpiresAt,
                previousStatus = previousStatus.ToString()
            });

        if (incident is not null)
        {
            await AddOutboxEventAsync(
                CreateExternalIntakeEvent(
                    $"rca-external-intake-revoked:{intake.Id}",
                    "RcaExternalIntakeRevoked",
                    intake.UpdatedAt ?? intake.CreatedAt,
                    incident,
                    intake),
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<RcaExternalIntakeDto>.Ok(ToDto(intake, incidentTitle), "Link externo revocado.");
    }

    private async Task<RcaIncident?> GetIncidentAsync(Guid incidentId, CancellationToken cancellationToken)
    {
        return await _dbContext.RcaIncidents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);
    }

    private static RcaDomainEventDto CreateExternalIntakeEvent(string id, string type, DateTimeOffset occurredAt, RcaIncident incident, RcaExternalIntakeRequest intake)
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
            Data = CreateExternalIntakeEventData(intake)
        };
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
            ["batchOrLot"] = intake.BatchOrLot
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

    private async Task<(bool Success, RcaExternalIntakeRequest? Intake, string? IncidentTitle, ApiError Error)> FindByTokenAsync(string token, bool track, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return (false, null, null, new ApiError { Field = nameof(token), Code = "TOKEN_REQUIRED", Message = "Token requerido." });
        }

        var hash = HashToken(token);
        var query = _dbContext.RcaExternalIntakeRequests.Where(x => x.TokenHash == hash && !x.IsDeleted);
        if (!track)
        {
            query = query.AsNoTracking();
        }

        var intake = await query.FirstOrDefaultAsync(cancellationToken);
        if (intake is null)
        {
            return (false, null, null, new ApiError { Field = nameof(token), Code = "INTAKE_NOT_FOUND", Message = "El link no corresponde a una solicitud activa." });
        }

        var incidentTitle = await _dbContext.RcaIncidents
            .AsNoTracking()
            .Where(x => x.Id == intake.RcaIncidentId && !x.IsDeleted)
            .Select(x => x.Title)
            .FirstOrDefaultAsync(cancellationToken);

        if (incidentTitle is null)
        {
            return (false, null, null, new ApiError { Field = nameof(token), Code = "RCA_NOT_FOUND", Message = "El RCA asociado ya no esta disponible." });
        }

        return (true, intake, incidentTitle, new ApiError());
    }

    private void AddAuditRecord(
        Guid tenantId,
        Guid incidentId,
        Guid intakeId,
        string action,
        string? userId,
        string summary,
        object? data)
    {
        _dbContext.RcaAuditRecords.Add(new RcaAuditRecord
        {
            TenantId = tenantId,
            RcaIncidentId = incidentId,
            EntityType = nameof(RcaExternalIntakeRequest),
            EntityId = intakeId,
            Action = action,
            UserId = Normalize(userId),
            OccurredAt = DateTimeOffset.UtcNow,
            Summary = summary,
            DataJson = data is null ? null : JsonSerializer.Serialize(data)
        });
    }

    private static List<ApiError> ValidateCreate(CreateExternalIntakeRequest request)
    {
        var errors = new List<ApiError>();

        if (!Enum.TryParse<RcaClaimActorType>(request.ActorType, true, out var actorType) || actorType == RcaClaimActorType.InternalArea)
        {
            errors.Add(new ApiError { Field = nameof(request.ActorType), Code = "INVALID_ACTOR_TYPE", Message = "El link externo solo puede crearse para Customer o Supplier." });
        }

        if (request.ExpiresAt.HasValue && request.ExpiresAt.Value <= DateTimeOffset.UtcNow)
        {
            errors.Add(new ApiError { Field = nameof(request.ExpiresAt), Code = "INVALID_EXPIRATION", Message = "La expiracion debe ser futura." });
        }

        return errors;
    }

    private static List<ApiError> ValidateSubmit(SubmitExternalIntakeRequest request)
    {
        var errors = new List<ApiError>();

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors.Add(new ApiError { Field = nameof(request.Description), Code = "DESCRIPTION_REQUIRED", Message = "La descripcion es obligatoria." });
        }

        return errors;
    }

    private static List<ApiError> ValidateReview(ReviewExternalIntakeRequest request, RcaExternalIntakeRequest intake)
    {
        var errors = new List<ApiError>();

        if (!request.ImportCause && !request.ImportCorrectiveAction)
        {
            errors.Add(new ApiError { Field = nameof(request.ImportCause), Code = "IMPORT_REQUIRED", Message = "Selecciona al menos una importacion o revoca el link." });
        }

        if (request.ImportCause && request.BranchId == Guid.Empty)
        {
            errors.Add(new ApiError { Field = nameof(request.BranchId), Code = "BRANCH_REQUIRED", Message = "La rama Ishikawa es obligatoria para importar causa." });
        }

        if (request.ImportCause && string.IsNullOrWhiteSpace(intake.ProposedRootCause))
        {
            errors.Add(new ApiError { Field = nameof(intake.ProposedRootCause), Code = "CAUSE_REQUIRED", Message = "La respuesta externa no tiene causa propuesta." });
        }

        if (request.ImportCorrectiveAction && string.IsNullOrWhiteSpace(intake.ProposedCorrectiveAction))
        {
            errors.Add(new ApiError { Field = nameof(intake.ProposedCorrectiveAction), Code = "ACTION_REQUIRED", Message = "La respuesta externa no tiene accion propuesta." });
        }

        return errors;
    }

    private static List<ApiError> ValidateReject(RejectExternalIntakeRequest request)
    {
        var errors = new List<ApiError>();

        if (string.IsNullOrWhiteSpace(request.RejectionReason))
        {
            errors.Add(new ApiError { Field = nameof(request.RejectionReason), Code = "REJECTION_REASON_REQUIRED", Message = "El motivo de rechazo es obligatorio." });
        }

        return errors;
    }

    private static RcaClaimActorType ParseActorType(string actorType)
    {
        return Enum.TryParse<RcaClaimActorType>(actorType, true, out var parsed)
            ? parsed
            : RcaClaimActorType.Supplier;
    }

    private static RcaExternalIntakeDto ToDto(RcaExternalIntakeRequest intake, string incidentTitle)
    {
        return new RcaExternalIntakeDto
        {
            Id = intake.Id,
            RcaIncidentId = intake.RcaIncidentId,
            IncidentTitle = incidentTitle,
            ActorType = intake.ActorType.ToString(),
            ActorName = intake.ActorName,
            ContactName = intake.ContactName,
            ContactEmail = intake.ContactEmail,
            Status = intake.Status.ToString(),
            CreatedAt = intake.CreatedAt,
            ExpiresAt = intake.ExpiresAt,
            OpenedAt = intake.OpenedAt,
            SubmittedAt = intake.SubmittedAt,
            ReviewedAt = intake.ReviewedAt,
            RejectedAt = intake.RejectedAt,
            RejectionReason = intake.RejectionReason,
            ClaimReference = intake.ClaimReference,
            MaterialCode = intake.MaterialCode,
            BatchOrLot = intake.BatchOrLot,
            Description = intake.Description,
            ContainmentResponse = intake.ContainmentResponse,
            ProposedRootCause = intake.ProposedRootCause,
            ProposedCorrectiveAction = intake.ProposedCorrectiveAction,
            EvidenceSummary = intake.EvidenceSummary
        };
    }

    private static string CreateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string ToTitle(string? value, string fallback)
    {
        return TrimToMax(Normalize(value) ?? fallback, 220);
    }

    private static string TrimToMax(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string BuildCauseDescription(RcaExternalIntakeRequest intake)
    {
        var parts = new List<string>
        {
            $"Respuesta externa de {intake.ActorType} {(intake.ActorName ?? string.Empty)}.".Trim(),
            $"Descripcion: {intake.Description}"
        };

        AddIfPresent(parts, "Referencia", intake.ClaimReference);
        AddIfPresent(parts, "Material", intake.MaterialCode);
        AddIfPresent(parts, "Lote", intake.BatchOrLot);
        AddIfPresent(parts, "Contencion informada", intake.ContainmentResponse);

        return string.Join(Environment.NewLine, parts);
    }

    private static string BuildEvidenceSummary(RcaExternalIntakeRequest intake)
    {
        var parts = new List<string>();
        AddIfPresent(parts, "Evidencia externa", intake.EvidenceSummary);
        AddIfPresent(parts, "Descripcion externa", intake.Description);
        AddIfPresent(parts, "Referencia", intake.ClaimReference);
        AddIfPresent(parts, "Material", intake.MaterialCode);
        AddIfPresent(parts, "Lote", intake.BatchOrLot);

        return string.Join(Environment.NewLine, parts);
    }

    private static string BuildActionDescription(RcaExternalIntakeRequest intake)
    {
        var parts = new List<string>
        {
            $"Accion propuesta por {intake.ActorType} {(intake.ActorName ?? string.Empty)}.".Trim()
        };

        AddIfPresent(parts, "Detalle propuesto", intake.ProposedCorrectiveAction);
        AddIfPresent(parts, "Contencion informada", intake.ContainmentResponse);
        AddIfPresent(parts, "Evidencia", intake.EvidenceSummary);

        return string.Join(Environment.NewLine, parts);
    }

    private static void AddIfPresent(List<string> parts, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{label}: {value.Trim()}");
        }
    }
}
