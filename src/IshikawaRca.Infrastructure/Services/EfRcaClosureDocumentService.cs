using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Entities;
using IshikawaRca.Domain.Enums;
using IshikawaRca.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IshikawaRca.Infrastructure.Services;

public class EfRcaClosureDocumentService : IRcaClosureDocumentService
{
    private readonly RcaDbContext _dbContext;

    public EfRcaClosureDocumentService(RcaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResult<RcaClosureDocumentDto>> RegisterGeneratedAsync(
        Guid incidentId,
        RegisterRcaClosureDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateRegisterRequest(request);
        if (validationErrors.Count > 0)
        {
            return ApiResult<RcaClosureDocumentDto>.Fail("No se pudo registrar el documento de cierre RCA.", validationErrors.ToArray());
        }

        var incident = await _dbContext.RcaIncidents
            .FirstOrDefaultAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

        if (incident is null)
        {
            return ApiResult<RcaClosureDocumentDto>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
        }

        if (incident.Status != RcaIncidentStatus.Closed)
        {
            return ApiResult<RcaClosureDocumentDto>.Fail(
                "No se pudo registrar el documento de cierre RCA.",
                new ApiError { Field = nameof(incident.Status), Code = "RCA_NOT_CLOSED", Message = "El RCA debe estar cerrado formalmente." });
        }

        var nextVersion = await _dbContext.RcaClosureDocuments
            .Where(x => x.TenantId == incident.TenantId && x.RcaIncidentId == incidentId && !x.IsDeleted)
            .Select(x => (int?)x.Version)
            .MaxAsync(cancellationToken) ?? 0;

        var now = DateTimeOffset.UtcNow;
        var document = new RcaClosureDocument
        {
            TenantId = incident.TenantId,
            RcaIncidentId = incidentId,
            Version = nextVersion + 1,
            FileName = Normalize(request.FileName),
            ContentType = Normalize(request.ContentType),
            SizeBytes = request.SizeBytes,
            StorageProvider = Normalize(request.StorageProvider),
            StorageKey = Normalize(request.StorageKey),
            Sha256 = Normalize(request.Sha256),
            Status = RcaClosureDocumentStatus.Draft,
            GeneratedAt = now,
            GeneratedByUserId = Normalize(request.GeneratedByUserId),
            CreatedAt = now,
            CreatedByUserId = Normalize(request.GeneratedByUserId)
        };

        _dbContext.RcaClosureDocuments.Add(document);
        AddAuditRecord(
            incident.TenantId,
            incidentId,
            document.Id,
            "RcaClosureDocumentGenerated",
            document.GeneratedByUserId,
            $"Documento de cierre RCA v{document.Version} generado.",
            new
            {
                document.Version,
                document.FileName,
                document.StorageProvider,
                document.Sha256
            });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<RcaClosureDocumentDto>.Ok(ToDto(document), "Documento de cierre RCA registrado.");
    }

    public async Task<ApiResult<IReadOnlyList<RcaClosureDocumentDto>>> ListAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var incident = await _dbContext.RcaIncidents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == incidentId && !x.IsDeleted, cancellationToken);

        if (incident is null)
        {
            return ApiResult<IReadOnlyList<RcaClosureDocumentDto>>.Fail(
                "No se encontro el incidente RCA.",
                new ApiError { Field = nameof(incidentId), Code = "RCA_NOT_FOUND", Message = "El identificador no corresponde a un incidente activo." });
        }

        var documents = await _dbContext.RcaClosureDocuments
            .AsNoTracking()
            .Where(x => x.TenantId == incident.TenantId && x.RcaIncidentId == incidentId && !x.IsDeleted)
            .OrderByDescending(x => x.Version)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

        return ApiResult<IReadOnlyList<RcaClosureDocumentDto>>.Ok(documents);
    }

    public Task<ApiResult<RcaClosureDocumentDto>> ApproveAsync(
        Guid incidentId,
        Guid documentId,
        ReviewRcaClosureDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        return ReviewAsync(incidentId, documentId, request, RcaClosureDocumentStatus.Approved, "RcaClosureDocumentApproved", cancellationToken);
    }

    public Task<ApiResult<RcaClosureDocumentDto>> RejectAsync(
        Guid incidentId,
        Guid documentId,
        ReviewRcaClosureDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        return ReviewAsync(incidentId, documentId, request, RcaClosureDocumentStatus.Rejected, "RcaClosureDocumentRejected", cancellationToken);
    }

    private async Task<ApiResult<RcaClosureDocumentDto>> ReviewAsync(
        Guid incidentId,
        Guid documentId,
        ReviewRcaClosureDocumentRequest request,
        RcaClosureDocumentStatus status,
        string auditAction,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateReviewRequest(request);
        if (validationErrors.Count > 0)
        {
            return ApiResult<RcaClosureDocumentDto>.Fail("No se pudo revisar el documento de cierre RCA.", validationErrors.ToArray());
        }

        var document = await _dbContext.RcaClosureDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId && x.RcaIncidentId == incidentId && !x.IsDeleted, cancellationToken);

        if (document is null)
        {
            return ApiResult<RcaClosureDocumentDto>.Fail(
                "No se encontro el documento de cierre RCA.",
                new ApiError { Field = nameof(documentId), Code = "RCA_CLOSURE_DOCUMENT_NOT_FOUND", Message = "El identificador no corresponde a un documento activo." });
        }

        if (document.Status is RcaClosureDocumentStatus.Approved or RcaClosureDocumentStatus.Rejected)
        {
            return ApiResult<RcaClosureDocumentDto>.Fail(
                "No se pudo revisar el documento de cierre RCA.",
                new ApiError { Field = nameof(document.Status), Code = "RCA_CLOSURE_DOCUMENT_ALREADY_REVIEWED", Message = "El documento ya fue revisado." });
        }

        var now = DateTimeOffset.UtcNow;
        document.Status = status;
        document.ReviewedAt = now;
        document.ReviewedByUserId = Normalize(request.ReviewedByUserId);
        document.ReviewNotes = Normalize(request.ReviewNotes);
        document.UpdatedAt = now;
        document.UpdatedByUserId = document.ReviewedByUserId;

        AddAuditRecord(
            document.TenantId,
            incidentId,
            document.Id,
            auditAction,
            document.ReviewedByUserId,
            $"Documento de cierre RCA v{document.Version} revisado como {status}.",
            new
            {
                document.Version,
                status = status.ToString(),
                document.ReviewNotes
            });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult<RcaClosureDocumentDto>.Ok(ToDto(document), "Documento de cierre RCA revisado.");
    }

    private static List<ApiError> ValidateRegisterRequest(RegisterRcaClosureDocumentRequest request)
    {
        var errors = new List<ApiError>();

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            errors.Add(new ApiError { Field = nameof(request.FileName), Code = "DOCUMENT_FILE_NAME_REQUIRED", Message = "El nombre del documento es obligatorio." });
        }

        if (request.SizeBytes <= 0)
        {
            errors.Add(new ApiError { Field = nameof(request.SizeBytes), Code = "DOCUMENT_SIZE_REQUIRED", Message = "El documento debe tener contenido." });
        }

        if (string.IsNullOrWhiteSpace(request.StorageProvider))
        {
            errors.Add(new ApiError { Field = nameof(request.StorageProvider), Code = "DOCUMENT_STORAGE_PROVIDER_REQUIRED", Message = "El proveedor de storage es obligatorio." });
        }

        if (string.IsNullOrWhiteSpace(request.StorageKey))
        {
            errors.Add(new ApiError { Field = nameof(request.StorageKey), Code = "DOCUMENT_STORAGE_KEY_REQUIRED", Message = "La clave de storage es obligatoria." });
        }

        if (string.IsNullOrWhiteSpace(request.Sha256) || request.Sha256.Trim().Length != 64)
        {
            errors.Add(new ApiError { Field = nameof(request.Sha256), Code = "DOCUMENT_SHA256_REQUIRED", Message = "El SHA-256 del documento es obligatorio." });
        }

        if (string.IsNullOrWhiteSpace(request.GeneratedByUserId))
        {
            errors.Add(new ApiError { Field = nameof(request.GeneratedByUserId), Code = "DOCUMENT_GENERATOR_REQUIRED", Message = "El usuario generador es obligatorio." });
        }

        return errors;
    }

    private static List<ApiError> ValidateReviewRequest(ReviewRcaClosureDocumentRequest request)
    {
        var errors = new List<ApiError>();

        if (string.IsNullOrWhiteSpace(request.ReviewedByUserId))
        {
            errors.Add(new ApiError { Field = nameof(request.ReviewedByUserId), Code = "DOCUMENT_REVIEWER_REQUIRED", Message = "El usuario revisor es obligatorio." });
        }

        if (string.IsNullOrWhiteSpace(request.ReviewNotes))
        {
            errors.Add(new ApiError { Field = nameof(request.ReviewNotes), Code = "DOCUMENT_REVIEW_NOTES_REQUIRED", Message = "Las notas de revision son obligatorias." });
        }

        return errors;
    }

    private void AddAuditRecord(Guid tenantId, Guid incidentId, Guid documentId, string action, string? userId, string summary, object data)
    {
        _dbContext.RcaAuditRecords.Add(new RcaAuditRecord
        {
            TenantId = tenantId,
            RcaIncidentId = incidentId,
            EntityType = nameof(RcaClosureDocument),
            EntityId = documentId,
            Action = action,
            UserId = userId,
            OccurredAt = DateTimeOffset.UtcNow,
            Summary = summary,
            DataJson = System.Text.Json.JsonSerializer.Serialize(data)
        });
    }

    private static RcaClosureDocumentDto ToDto(RcaClosureDocument document)
    {
        return new RcaClosureDocumentDto
        {
            Id = document.Id,
            TenantId = document.TenantId,
            RcaIncidentId = document.RcaIncidentId,
            Version = document.Version,
            FileName = document.FileName,
            ContentType = document.ContentType,
            SizeBytes = document.SizeBytes,
            StorageProvider = document.StorageProvider,
            StorageKey = document.StorageKey,
            Sha256 = document.Sha256,
            Status = document.Status.ToString(),
            GeneratedAt = document.GeneratedAt,
            GeneratedByUserId = document.GeneratedByUserId,
            ReviewedAt = document.ReviewedAt,
            ReviewedByUserId = document.ReviewedByUserId,
            ReviewNotes = document.ReviewNotes
        };
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
