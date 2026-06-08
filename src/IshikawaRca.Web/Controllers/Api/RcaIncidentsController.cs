using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Common;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Web.Models.Rca;
using IshikawaRca.Web.Security;
using IshikawaRca.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IshikawaRca.Web.Controllers.Api;

[ApiController]
[Route("api/v1/rca/incidents")]
public class RcaIncidentsController : ControllerBase
{
    private readonly IRcaIncidentService _rcaIncidentService;
    private readonly IEvidenceFileStorage _evidenceFileStorage;
    private readonly ICurrentRcaUserContext _currentUserContext;

    public RcaIncidentsController(
        IRcaIncidentService rcaIncidentService,
        IEvidenceFileStorage evidenceFileStorage,
        ICurrentRcaUserContext currentUserContext)
    {
        _rcaIncidentService = rcaIncidentService;
        _evidenceFileStorage = evidenceFileStorage;
        _currentUserContext = currentUserContext;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResult<RcaIncidentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult<RcaIncidentDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResult<RcaIncidentDto>>> Create(CreateRcaIncidentRequest request, CancellationToken cancellationToken)
    {
        if (request.TenantId == Guid.Empty)
        {
            request.TenantId = _currentUserContext.TenantId;
        }

        request.ReportedBy = string.IsNullOrWhiteSpace(request.ReportedBy)
            ? _currentUserContext.UserId
            : request.ReportedBy;

        var result = await _rcaIncidentService.CreateAsync(request, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<RcaIncidentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult<IReadOnlyList<RcaIncidentDto>>>> List(
        [FromQuery] string? sourceSystem,
        [FromQuery] string? externalTaskId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.ListAsync(sourceSystem, externalTaskId, status, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<RcaIncidentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaIncidentDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<RcaIncidentDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.GetByIdAsync(id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}/canvas")]
    [ProducesResponseType(typeof(ApiResult<IshikawaCanvasDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<IshikawaCanvasDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<IshikawaCanvasDto>>> GetCanvas(Guid id, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.GetCanvasAsync(id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/causes")]
    [ProducesResponseType(typeof(ApiResult<IshikawaCauseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult<IshikawaCauseDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResult<IshikawaCauseDto>>> AddCause(Guid id, AddIshikawaCauseRequest request, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.AddCauseAsync(id, request, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetCanvas), new { id }, result);
    }

    [HttpGet("{id:guid}/actions")]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<CorrectiveActionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<CorrectiveActionDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<IReadOnlyList<CorrectiveActionDto>>>> ListCorrectiveActions(Guid id, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.ListCorrectiveActionsAsync(id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/actions")]
    [ProducesResponseType(typeof(ApiResult<CorrectiveActionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult<CorrectiveActionDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResult<CorrectiveActionDto>>> AddCorrectiveAction(Guid id, AddCorrectiveActionRequest request, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.AddCorrectiveActionAsync(id, request, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(ListCorrectiveActions), new { id }, result);
    }

    [HttpPost("{id:guid}/actions/{actionId:guid}/status")]
    [Authorize(Roles = RcaRoleNames.SensitiveOperations)]
    [ProducesResponseType(typeof(ApiResult<CorrectiveActionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<CorrectiveActionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<CorrectiveActionDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<CorrectiveActionDto>>> UpdateCorrectiveActionStatus(Guid id, Guid actionId, UpdateCorrectiveActionStatusRequest request, CancellationToken cancellationToken)
    {
        request.CompletedByUserId = string.IsNullOrWhiteSpace(request.CompletedByUserId)
            ? _currentUserContext.UserId
            : request.CompletedByUserId;

        var result = await _rcaIncidentService.UpdateCorrectiveActionStatusAsync(id, actionId, request, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return result.Errors.Any(x => x.Code == "ACTION_NOT_FOUND")
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}/evidence")]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<RcaEvidenceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<RcaEvidenceDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<IReadOnlyList<RcaEvidenceDto>>>> ListEvidence(Guid id, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.ListEvidenceAsync(id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/evidence")]
    [ProducesResponseType(typeof(ApiResult<RcaEvidenceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult<RcaEvidenceDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResult<RcaEvidenceDto>>> AddEvidence(Guid id, AddRcaEvidenceRequest request, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.AddEvidenceAsync(id, request, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(ListEvidence), new { id }, result);
    }

    [HttpGet("{id:guid}/facts")]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<RcaFactDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<RcaFactDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<IReadOnlyList<RcaFactDto>>>> ListFacts(Guid id, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.ListFactsAsync(id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/facts")]
    [ProducesResponseType(typeof(ApiResult<RcaFactDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult<RcaFactDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<RcaFactDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<RcaFactDto>>> AddFact(Guid id, AddRcaFactRequest request, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.AddFactAsync(id, request, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return result.Errors.Any(x => x.Code == "RCA_NOT_FOUND")
                ? NotFound(result)
                : BadRequest(result);
        }

        if (string.Equals(result.Message, "Hecho externo existente.", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(result);
        }

        return CreatedAtAction(nameof(ListFacts), new { id }, result);
    }

    [HttpPost("{id:guid}/evidence-files")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(104_857_600)]
    [ProducesResponseType(typeof(ApiResult<RcaEvidenceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult<RcaEvidenceDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResult<RcaEvidenceDto>>> AddEvidenceFile(
        Guid id,
        [FromForm] AddRcaEvidenceFileViewModel form,
        CancellationToken cancellationToken)
    {
        if (form.Attachment is null)
        {
            return BadRequest(ApiResult<RcaEvidenceDto>.Fail(
                "No se pudo agregar la evidencia.",
                new ApiError { Field = nameof(form.Attachment), Code = "ATTACHMENT_REQUIRED", Message = "El archivo adjunto es obligatorio." }));
        }

        StoredEvidenceFile attachment;
        try
        {
            attachment = await _evidenceFileStorage.SaveAsync(id, form.Attachment, cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            return BadRequest(ApiResult<RcaEvidenceDto>.Fail(
                "No se pudo agregar la evidencia.",
                new ApiError { Field = nameof(form.Attachment), Code = "INVALID_ATTACHMENT", Message = ex.Message }));
        }

        var request = new AddRcaEvidenceRequest
        {
            CauseId = form.CauseId,
            ExternalIntakeId = form.ExternalIntakeId,
            Title = form.Title,
            EvidenceType = form.EvidenceType,
            Source = form.Source,
            SourceDetail = form.SourceDetail,
            Tags = form.Tags,
            Summary = form.Summary,
            ReferenceUri = form.ReferenceUri,
            CapturedAt = form.CapturedAt,
            CapturedByUserId = form.CapturedByUserId,
            ValidationStatus = form.ValidationStatus,
            ValidatedAt = form.ValidatedAt,
            ValidatedByUserId = form.ValidatedByUserId,
            ValidationNotes = form.ValidationNotes,
            AttachmentFileName = attachment.FileName,
            AttachmentContentType = attachment.ContentType,
            AttachmentSizeBytes = attachment.SizeBytes,
            AttachmentStorageProvider = attachment.StorageProvider,
            AttachmentStorageKey = attachment.StorageKey,
            AttachmentSha256 = attachment.Sha256
        };

        var result = await _rcaIncidentService.AddEvidenceAsync(id, request, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(ListEvidence), new { id }, result);
    }

    [HttpPut("{id:guid}/evidence/{evidenceId:guid}")]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    [ProducesResponseType(typeof(ApiResult<RcaEvidenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaEvidenceDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<RcaEvidenceDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<RcaEvidenceDto>>> UpdateEvidence(
        Guid id,
        Guid evidenceId,
        UpdateRcaEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        request.ValidatedByUserId = string.IsNullOrWhiteSpace(request.ValidatedByUserId)
            ? _currentUserContext.UserId
            : request.ValidatedByUserId;

        var result = await _rcaIncidentService.UpdateEvidenceAsync(id, evidenceId, request, cancellationToken);
        if (!result.Success)
        {
            return result.Errors.Any(x => x.Code == "EVIDENCE_NOT_FOUND")
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/evidence/{evidenceId:guid}/attachment")]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(104_857_600)]
    [ProducesResponseType(typeof(ApiResult<RcaEvidenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaEvidenceDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<RcaEvidenceDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<RcaEvidenceDto>>> ReplaceEvidenceAttachment(
        Guid id,
        Guid evidenceId,
        [FromForm] ReplaceRcaEvidenceAttachmentViewModel form,
        CancellationToken cancellationToken)
    {
        if (form.Attachment is null)
        {
            return BadRequest(ApiResult<RcaEvidenceDto>.Fail(
                "No se pudo reemplazar el adjunto.",
                new ApiError { Field = nameof(form.Attachment), Code = "ATTACHMENT_REQUIRED", Message = "El archivo adjunto es obligatorio." }));
        }

        var evidenceResult = await _rcaIncidentService.ListEvidenceAsync(id, cancellationToken);
        if (!evidenceResult.Success)
        {
            return NotFound(evidenceResult);
        }

        var existingEvidence = evidenceResult.Data?.FirstOrDefault(x => x.Id == evidenceId);
        if (existingEvidence is null)
        {
            return NotFound(ApiResult<RcaEvidenceDto>.Fail(
                "No se encontro la evidencia RCA.",
                new ApiError { Field = nameof(evidenceId), Code = "EVIDENCE_NOT_FOUND", Message = "La evidencia no corresponde al incidente RCA." }));
        }

        StoredEvidenceFile attachment;
        try
        {
            attachment = await _evidenceFileStorage.SaveAsync(id, form.Attachment, cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            return BadRequest(ApiResult<RcaEvidenceDto>.Fail(
                "No se pudo reemplazar el adjunto.",
                new ApiError { Field = nameof(form.Attachment), Code = "INVALID_ATTACHMENT", Message = ex.Message }));
        }

        var request = new ReplaceRcaEvidenceAttachmentRequest
        {
            AttachmentFileName = attachment.FileName,
            AttachmentContentType = attachment.ContentType,
            AttachmentSizeBytes = attachment.SizeBytes,
            AttachmentStorageProvider = attachment.StorageProvider,
            AttachmentStorageKey = attachment.StorageKey,
            AttachmentSha256 = attachment.Sha256
        };

        var result = await _rcaIncidentService.ReplaceEvidenceAttachmentAsync(id, evidenceId, request, _currentUserContext.UserId, cancellationToken);
        if (!result.Success)
        {
            _evidenceFileStorage.Delete(attachment.StorageKey);

            return result.Errors.Any(x => x.Code == "EVIDENCE_NOT_FOUND")
                ? NotFound(result)
                : BadRequest(result);
        }

        _evidenceFileStorage.Delete(existingEvidence.AttachmentStorageKey);

        return Ok(result);
    }

    [HttpDelete("{id:guid}/evidence/{evidenceId:guid}")]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    [ProducesResponseType(typeof(ApiResult<RcaEvidenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaEvidenceDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<RcaEvidenceDto>>> DeleteEvidence(Guid id, Guid evidenceId, CancellationToken cancellationToken)
    {
        var evidenceResult = await _rcaIncidentService.ListEvidenceAsync(id, cancellationToken);
        if (!evidenceResult.Success)
        {
            return NotFound(evidenceResult);
        }

        var existingEvidence = evidenceResult.Data?.FirstOrDefault(x => x.Id == evidenceId);
        if (existingEvidence is null)
        {
            return NotFound(ApiResult<RcaEvidenceDto>.Fail(
                "No se encontro la evidencia RCA.",
                new ApiError { Field = nameof(evidenceId), Code = "EVIDENCE_NOT_FOUND", Message = "La evidencia no corresponde al incidente RCA." }));
        }

        var result = await _rcaIncidentService.DeleteEvidenceAsync(id, evidenceId, _currentUserContext.UserId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }

        _evidenceFileStorage.Delete(existingEvidence.AttachmentStorageKey);

        return Ok(result);
    }

    [HttpGet("{id:guid}/evidence/{evidenceId:guid}/attachment")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadEvidenceAttachment(Guid id, Guid evidenceId, CancellationToken cancellationToken)
    {
        var evidenceResult = await _rcaIncidentService.ListEvidenceAsync(id, cancellationToken);
        if (!evidenceResult.Success)
        {
            return NotFound();
        }

        var evidence = evidenceResult.Data?.FirstOrDefault(x => x.Id == evidenceId);
        if (evidence is null || string.IsNullOrWhiteSpace(evidence.AttachmentStorageKey))
        {
            return NotFound();
        }

        try
        {
            var file = _evidenceFileStorage.Resolve(
                evidence.AttachmentStorageKey,
                evidence.AttachmentFileName,
                evidence.AttachmentContentType);

            return PhysicalFile(file.PhysicalPath, file.ContentType, file.FileName);
        }
        catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    [ProducesResponseType(typeof(ApiResult<RcaIncidentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaIncidentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<RcaIncidentDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<RcaIncidentDto>>> Close(Guid id, CloseRcaIncidentRequest request, CancellationToken cancellationToken)
    {
        request.ClosedByUserId = string.IsNullOrWhiteSpace(request.ClosedByUserId)
            ? _currentUserContext.UserId
            : request.ClosedByUserId;

        var result = await _rcaIncidentService.CloseAsync(id, request, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return result.Errors.Any(x => x.Code == "RCA_NOT_FOUND")
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/escalate-8d")]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    [ProducesResponseType(typeof(ApiResult<RcaIncidentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaIncidentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<RcaIncidentDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<RcaIncidentDto>>> EscalateTo8D(Guid id, EscalateRcaIncidentTo8DRequest request, CancellationToken cancellationToken)
    {
        request.EscalatedByUserId = string.IsNullOrWhiteSpace(request.EscalatedByUserId)
            ? _currentUserContext.UserId
            : request.EscalatedByUserId;

        var result = await _rcaIncidentService.EscalateTo8DAsync(id, request, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return result.Errors.Any(x => x.Code == "RCA_NOT_FOUND")
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/wizard/step")]
    [ProducesResponseType(typeof(ApiResult<RcaIncidentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaIncidentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<RcaIncidentDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<RcaIncidentDto>>> CompleteWizardStep(Guid id, CompleteRcaWizardStepRequest request, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.CompleteWizardStepAsync(id, request, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return result.Errors.Any(x => x.Code == "RCA_NOT_FOUND")
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}/wizard/progress")]
    [ProducesResponseType(typeof(ApiResult<RcaWizardProgressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<RcaWizardProgressDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResult<RcaWizardProgressDto>>> GetWizardProgress(Guid id, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.GetWizardProgressAsync(id, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
