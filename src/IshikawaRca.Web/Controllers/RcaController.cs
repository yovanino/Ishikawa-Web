using IshikawaRca.Application.Rca;
using IshikawaRca.Application.Ai;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Domain.Enums;
using IshikawaRca.Web.Models.Rca;
using IshikawaRca.Web.Security;
using IshikawaRca.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IshikawaRca.Web.Controllers;

public class RcaController : Controller
{
    private readonly IRcaIncidentService _rcaIncidentService;
    private readonly IRcaExternalIntakeService _externalIntakeService;
    private readonly IRcaAiAssistantService _aiAssistantService;
    private readonly IEvidenceFileStorage _evidenceFileStorage;
    private readonly IRcaPdfReportService _pdfReportService;
    private readonly IClosureDocumentStorage _closureDocumentStorage;
    private readonly IRcaClosureDocumentService _closureDocumentService;
    private readonly ICurrentRcaUserContext _currentUserContext;

    public RcaController(
        IRcaIncidentService rcaIncidentService,
        IRcaExternalIntakeService externalIntakeService,
        IRcaAiAssistantService aiAssistantService,
        IEvidenceFileStorage evidenceFileStorage,
        IRcaPdfReportService pdfReportService,
        IClosureDocumentStorage closureDocumentStorage,
        IRcaClosureDocumentService closureDocumentService,
        ICurrentRcaUserContext currentUserContext)
    {
        _rcaIncidentService = rcaIncidentService;
        _externalIntakeService = externalIntakeService;
        _aiAssistantService = aiAssistantService;
        _evidenceFileStorage = evidenceFileStorage;
        _pdfReportService = pdfReportService;
        _closureDocumentStorage = closureDocumentStorage;
        _closureDocumentService = closureDocumentService;
        _currentUserContext = currentUserContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.ListAsync(cancellationToken: cancellationToken);

        return View(result.Data ?? []);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Severities = GetSeverityOptions();
        ViewBag.ClaimActorTypes = GetClaimActorTypeOptions();

        return View(new CreateRcaIncidentViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRcaIncidentViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Severities = GetSeverityOptions();
            ViewBag.ClaimActorTypes = GetClaimActorTypeOptions();
            return View(model);
        }

        var request = new CreateRcaIncidentRequest
        {
            TenantId = _currentUserContext.TenantId,
            Title = model.Title,
            ProblemDescription = model.ProblemDescription,
            Severity = model.Severity,
            ClaimActorType = model.ClaimActorType,
            ClaimOwnerName = model.ClaimOwnerName,
            SourceSystem = model.SourceSystem,
            OccurredAt = model.OccurredAt,
            MachineCode = model.MachineCode,
            LineCode = model.LineCode,
            WorkOrderCode = model.WorkOrderCode,
            ReportedBy = string.IsNullOrWhiteSpace(model.ReportedBy) ? _currentUserContext.UserId : model.ReportedBy
        };

        var result = await _rcaIncidentService.CreateAsync(request, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }

            ViewBag.Severities = GetSeverityOptions();
            ViewBag.ClaimActorTypes = GetClaimActorTypeOptions();
            return View(model);
        }

        TempData["StatusMessage"] = "Incidente RCA creado.";

        return RedirectToAction(nameof(Details), new { id = result.Data.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var model = await BuildDetailsViewModelAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCause(Guid id, [Bind(Prefix = "Cause")] AddIshikawaCauseViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await DetailsWithCauseModel(id, model, cancellationToken);
        }

        var request = new AddIshikawaCauseRequest
        {
            BranchId = model.BranchId,
            ParentCauseId = model.ParentCauseId,
            Title = model.Title,
            Description = model.Description,
            ProbabilityScore = model.ProbabilityScore,
            ImpactScore = model.ImpactScore,
            FrequencyScore = model.FrequencyScore,
            IsRootCause = model.IsRootCause,
            EvidenceSummary = model.EvidenceSummary
        };

        var result = await _rcaIncidentService.AddCauseAsync(id, request, cancellationToken);
        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError($"Cause.{error.Field}", error.Message);
            }

            return await DetailsWithCauseModel(id, model, cancellationToken);
        }

        TempData["StatusMessage"] = "Causa agregada al canvas Ishikawa.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAction(Guid id, [Bind(Prefix = "Action")] AddCorrectiveActionViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await DetailsWithActionModel(id, model, cancellationToken);
        }

        var request = new AddCorrectiveActionRequest
        {
            CauseId = model.CauseId,
            Title = model.Title,
            Description = model.Description,
            ActionType = model.ActionType,
            ResolutionScope = model.ResolutionScope,
            AssignedToUserId = model.AssignedToUserId,
            DueDate = model.DueDate
        };

        var result = await _rcaIncidentService.AddCorrectiveActionAsync(id, request, cancellationToken);
        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError($"Action.{error.Field}", error.Message);
            }

            return await DetailsWithActionModel(id, model, cancellationToken);
        }

        TempData["StatusMessage"] = "Accion correctiva agregada.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RcaRoleNames.SensitiveOperations)]
    public async Task<IActionResult> UpdateActionStatus(Guid id, UpdateCorrectiveActionStatusViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["StatusMessage"] = "No se pudo actualizar la accion: revise estado y validacion.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var request = new UpdateCorrectiveActionStatusRequest
        {
            Status = model.Status,
            CompletedByUserId = string.IsNullOrWhiteSpace(model.CompletedByUserId) ? _currentUserContext.UserId : model.CompletedByUserId,
            ValidationNotes = model.ValidationNotes
        };

        var result = await _rcaIncidentService.UpdateCorrectiveActionStatusAsync(id, model.ActionId, request, cancellationToken);
        TempData["StatusMessage"] = result.Success
            ? "Estado de accion actualizado."
            : result.Message ?? "No se pudo actualizar la accion.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(104_857_600)]
    public async Task<IActionResult> AddEvidence(Guid id, [Bind(Prefix = "EvidenceForm")] AddRcaEvidenceViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var details = await BuildDetailsViewModelAsync(id, cancellationToken);
            if (details is null)
            {
                return NotFound();
            }

            details.EvidenceForm = model;
            return View(nameof(Details), details);
        }

        StoredEvidenceFile? attachment = null;
        if (model.Attachment is not null)
        {
            try
            {
                attachment = await _evidenceFileStorage.SaveAsync(id, model.Attachment, cancellationToken);
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException)
            {
                ModelState.AddModelError("EvidenceForm.Attachment", ex.Message);
                var details = await BuildDetailsViewModelAsync(id, cancellationToken);
                if (details is null)
                {
                    return NotFound();
                }

                details.EvidenceForm = model;
                return View(nameof(Details), details);
            }
        }

        var request = new AddRcaEvidenceRequest
        {
            CauseId = model.CauseId,
            Title = model.Title,
            EvidenceType = model.EvidenceType,
            Source = model.Source,
            SourceDetail = model.SourceDetail,
            Tags = model.Tags,
            Summary = model.Summary,
            ReferenceUri = model.ReferenceUri,
            AttachmentFileName = attachment?.FileName,
            AttachmentContentType = attachment?.ContentType,
            AttachmentSizeBytes = attachment?.SizeBytes,
            AttachmentStorageProvider = attachment?.StorageProvider,
            AttachmentStorageKey = attachment?.StorageKey,
            AttachmentSha256 = attachment?.Sha256,
            CapturedAt = model.CapturedAt,
            CapturedByUserId = model.CapturedByUserId,
            ValidationStatus = model.ValidationStatus,
            ValidatedAt = model.ValidatedAt,
            ValidatedByUserId = model.ValidatedByUserId,
            ValidationNotes = model.ValidationNotes
        };

        var result = await _rcaIncidentService.AddEvidenceAsync(id, request, cancellationToken);
        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError($"EvidenceForm.{error.Field}", error.Message);
            }

            var details = await BuildDetailsViewModelAsync(id, cancellationToken);
            if (details is null)
            {
                return NotFound();
            }

            details.EvidenceForm = model;
            return View(nameof(Details), details);
        }

        TempData["StatusMessage"] = "Evidencia agregada.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    public async Task<IActionResult> UpdateEvidence(Guid id, [Bind(Prefix = "EvidenceEdit")] UpdateRcaEvidenceViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["StatusMessage"] = "No se pudo actualizar la evidencia: revisa los campos obligatorios.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var request = new UpdateRcaEvidenceRequest
        {
            CauseId = model.CauseId,
            Title = model.Title,
            EvidenceType = model.EvidenceType,
            Source = model.Source,
            SourceDetail = model.SourceDetail,
            Tags = model.Tags,
            Summary = model.Summary,
            ReferenceUri = model.ReferenceUri,
            CapturedAt = model.CapturedAt,
            CapturedByUserId = model.CapturedByUserId,
            ValidationStatus = model.ValidationStatus,
            ValidatedAt = model.ValidatedAt,
            ValidatedByUserId = string.IsNullOrWhiteSpace(model.ValidatedByUserId) ? _currentUserContext.UserId : model.ValidatedByUserId,
            ValidationNotes = model.ValidationNotes
        };

        var result = await _rcaIncidentService.UpdateEvidenceAsync(id, model.EvidenceId, request, cancellationToken);
        TempData["StatusMessage"] = result.Success
            ? "Evidencia actualizada."
            : result.Message ?? "No se pudo actualizar la evidencia.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    [RequestSizeLimit(104_857_600)]
    public async Task<IActionResult> ReplaceEvidenceAttachment(Guid id, [Bind(Prefix = "EvidenceAttachment")] ReplaceRcaEvidenceAttachmentViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.Attachment is null)
        {
            TempData["StatusMessage"] = "Selecciona un archivo valido para reemplazar el adjunto.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var existingEvidence = await FindEvidenceAsync(id, model.EvidenceId, cancellationToken);
        if (existingEvidence is null)
        {
            return NotFound();
        }

        StoredEvidenceFile attachment;
        try
        {
            attachment = await _evidenceFileStorage.SaveAsync(id, model.Attachment, cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            TempData["StatusMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
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

        var result = await _rcaIncidentService.ReplaceEvidenceAttachmentAsync(id, model.EvidenceId, request, _currentUserContext.UserId, cancellationToken);
        if (result.Success)
        {
            _evidenceFileStorage.Delete(existingEvidence.AttachmentStorageKey);
        }
        else
        {
            _evidenceFileStorage.Delete(attachment.StorageKey);
        }

        TempData["StatusMessage"] = result.Success
            ? "Adjunto reemplazado."
            : result.Message ?? "No se pudo reemplazar el adjunto.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    public async Task<IActionResult> DeleteEvidence(Guid id, Guid evidenceId, CancellationToken cancellationToken)
    {
        var existingEvidence = await FindEvidenceAsync(id, evidenceId, cancellationToken);
        if (existingEvidence is null)
        {
            return NotFound();
        }

        var result = await _rcaIncidentService.DeleteEvidenceAsync(id, evidenceId, _currentUserContext.UserId, cancellationToken);
        if (result.Success)
        {
            _evidenceFileStorage.Delete(existingEvidence.AttachmentStorageKey);
        }

        TempData["StatusMessage"] = result.Success
            ? "Evidencia eliminada."
            : result.Message ?? "No se pudo eliminar la evidencia.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFact(Guid id, [Bind(Prefix = "FactForm")] AddRcaFactViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await DetailsWithFactModel(id, model, cancellationToken);
        }

        var request = new AddRcaFactRequest
        {
            CauseId = model.CauseId,
            EvidenceId = model.EvidenceId,
            CorrectiveActionId = model.CorrectiveActionId,
            ExternalIntakeId = model.ExternalIntakeId,
            FactType = model.FactType,
            Source = model.Source,
            SourceDetail = model.SourceDetail,
            ExternalSourceSystem = model.ExternalSourceSystem,
            ExternalEventId = model.ExternalEventId,
            ExternalRecordUri = model.ExternalRecordUri,
            FactSeverity = model.FactSeverity,
            ShiftCode = model.ShiftCode,
            MachineCode = model.MachineCode,
            LineCode = model.LineCode,
            WorkOrderCode = model.WorkOrderCode,
            MaterialCode = model.MaterialCode,
            BatchOrLot = model.BatchOrLot,
            AlarmCode = model.AlarmCode,
            MeasurementName = model.MeasurementName,
            MeasurementValue = model.MeasurementValue,
            MeasurementUnit = model.MeasurementUnit,
            Title = model.Title,
            Description = model.Description,
            OccurredAt = model.OccurredAt,
            CapturedByUserId = model.CapturedByUserId
        };

        var result = await _rcaIncidentService.AddFactAsync(id, request, cancellationToken);
        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError($"FactForm.{error.Field}", error.Message);
            }

            return await DetailsWithFactModel(id, model, cancellationToken);
        }

        TempData["StatusMessage"] = "Hecho agregado a la linea RCA.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadEvidence(Guid id, Guid evidenceId, CancellationToken cancellationToken)
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

    [HttpGet]
    public async Task<IActionResult> PreviewEvidence(Guid id, Guid evidenceId, CancellationToken cancellationToken)
    {
        var evidenceResult = await _rcaIncidentService.ListEvidenceAsync(id, cancellationToken);
        if (!evidenceResult.Success)
        {
            return NotFound();
        }

        var evidence = evidenceResult.Data?.FirstOrDefault(x => x.Id == evidenceId);
        if (evidence is null ||
            string.IsNullOrWhiteSpace(evidence.AttachmentStorageKey) ||
            !IsPreviewableContentType(evidence.AttachmentContentType))
        {
            return NotFound();
        }

        try
        {
            var file = _evidenceFileStorage.Resolve(
                evidence.AttachmentStorageKey,
                evidence.AttachmentFileName,
                evidence.AttachmentContentType);

            Response.Headers.ContentDisposition = "inline";

            return PhysicalFile(file.PhysicalPath, file.ContentType);
        }
        catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(Guid id, CancellationToken cancellationToken)
    {
        var model = await BuildDetailsViewModelAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        var evidenceUrls = model.Evidence
            .Where(x => !string.IsNullOrWhiteSpace(x.AttachmentStorageKey))
            .ToDictionary(
                x => x.Id,
                x => Url.Action(
                    nameof(DownloadEvidence),
                    "Rca",
                    new { id = model.Incident.Id, evidenceId = x.Id },
                    Request.Scheme) ?? string.Empty);

        var pdf = _pdfReportService.Build(model, evidenceUrls);
        var fileName = $"rca-{model.Incident.Id:N}.pdf";

        StoredClosureDocumentFile storedDocument;
        try
        {
            storedDocument = await _closureDocumentStorage.SaveAsync(model.Incident.Id, fileName, pdf, cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            return BadRequest(ex.Message);
        }

        var registration = await _closureDocumentService.RegisterGeneratedAsync(
            model.Incident.Id,
            new RegisterRcaClosureDocumentRequest
            {
                FileName = storedDocument.FileName,
                ContentType = storedDocument.ContentType,
                SizeBytes = storedDocument.SizeBytes,
                StorageProvider = storedDocument.StorageProvider,
                StorageKey = storedDocument.StorageKey,
                Sha256 = storedDocument.Sha256,
                GeneratedByUserId = _currentUserContext.UserId
            },
            cancellationToken);
        if (!registration.Success)
        {
            _closureDocumentStorage.Delete(storedDocument.StorageKey);
            return BadRequest(registration);
        }

        return File(pdf, "application/pdf", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    public async Task<IActionResult> Close(Guid id, [Bind(Prefix = "CloseForm")] CloseRcaIncidentViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var details = await BuildDetailsViewModelAsync(id, cancellationToken);
            if (details is null)
            {
                return NotFound();
            }

            details.CloseForm = model;
            return View(nameof(Details), details);
        }

        var request = new CloseRcaIncidentRequest
        {
            ClosedByUserId = string.IsNullOrWhiteSpace(model.ClosedByUserId) ? _currentUserContext.UserId : model.ClosedByUserId,
            ClosureSummary = model.ClosureSummary
        };

        var result = await _rcaIncidentService.CloseAsync(id, request, cancellationToken);
        TempData["StatusMessage"] = result.Success
            ? "Incidente RCA cerrado."
            : result.Message ?? "No se pudo cerrar el incidente RCA.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    public async Task<IActionResult> EscalateTo8D(Guid id, [Bind(Prefix = "EscalateForm")] EscalateRcaIncidentTo8DViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var details = await BuildDetailsViewModelAsync(id, cancellationToken);
            if (details is null)
            {
                return NotFound();
            }

            details.EscalateForm = model;
            return View(nameof(Details), details);
        }

        var request = new EscalateRcaIncidentTo8DRequest
        {
            EscalatedByUserId = string.IsNullOrWhiteSpace(model.EscalatedByUserId) ? _currentUserContext.UserId : model.EscalatedByUserId,
            EscalationReason = model.EscalationReason
        };

        var result = await _rcaIncidentService.EscalateTo8DAsync(id, request, cancellationToken);
        TempData["StatusMessage"] = result.Success
            ? "Incidente RCA escalado a 8D."
            : result.Message ?? "No se pudo escalar el incidente RCA a 8D.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteWizardStep(Guid id, [Bind(Prefix = "WizardForm")] CompleteRcaWizardStepViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var details = await BuildDetailsViewModelAsync(id, cancellationToken);
            if (details is null)
            {
                return NotFound();
            }

            details.WizardForm = model;
            return View(nameof(Details), details);
        }

        var request = new CompleteRcaWizardStepRequest
        {
            Step = model.Step,
            CompletedByUserId = model.CompletedByUserId,
            Notes = model.Notes
        };

        var result = await _rcaIncidentService.CompleteWizardStepAsync(id, request, cancellationToken);
        TempData["StatusMessage"] = result.Success
            ? "Etapa del wizard RCA completada."
            : result.Message ?? "No se pudo completar la etapa del wizard RCA.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    public async Task<IActionResult> CreateExternalIntake(Guid id, [Bind(Prefix = "ExternalIntake")] CreateExternalIntakeViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var details = await BuildDetailsViewModelAsync(id, cancellationToken);
            if (details is null)
            {
                return NotFound();
            }

            details.ExternalIntake = model;
            return View(nameof(Details), details);
        }

        var request = new CreateExternalIntakeRequest
        {
            ActorType = model.ActorType,
            ActorName = model.ActorName,
            ContactName = model.ContactName,
            ContactEmail = model.ContactEmail,
            ExpiresAt = model.ExpiresAt
        };

        var result = await _externalIntakeService.CreateAsync(id, request, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError($"ExternalIntake.{error.Field}", error.Message);
            }

            var details = await BuildDetailsViewModelAsync(id, cancellationToken);
            if (details is null)
            {
                return NotFound();
            }

            details.ExternalIntake = model;
            return View(nameof(Details), details);
        }

        var link = Url.Action("Index", "ExternalIntake", new { token = result.Data.Token }, Request.Scheme);
        TempData["StatusMessage"] = "Link externo creado.";
        TempData["ExternalIntakeLink"] = link;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    public async Task<IActionResult> RevokeExternalIntake(Guid id, Guid intakeId, CancellationToken cancellationToken)
    {
        var result = await _externalIntakeService.RevokeAsync(intakeId, _currentUserContext.UserId, cancellationToken);
        TempData["StatusMessage"] = result.Success ? "Link externo revocado." : "No se pudo revocar el link externo.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    public async Task<IActionResult> ReviewExternalIntake(
        Guid id,
        Guid intakeId,
        Guid branchId,
        bool importCause,
        bool markCauseAsRoot,
        bool importCorrectiveAction,
        string? reviewedByUserId,
        CancellationToken cancellationToken)
    {
        var request = new ReviewExternalIntakeRequest
        {
            BranchId = branchId,
            ImportCause = importCause,
            MarkCauseAsRoot = markCauseAsRoot,
            ImportCorrectiveAction = importCorrectiveAction,
            ReviewedByUserId = string.IsNullOrWhiteSpace(reviewedByUserId) ? _currentUserContext.UserId : reviewedByUserId
        };

        var result = await _externalIntakeService.ReviewAsync(intakeId, request, cancellationToken);
        TempData["StatusMessage"] = result.Success
            ? "Respuesta externa revisada e importada."
            : result.Message ?? "No se pudo revisar la respuesta externa.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    public async Task<IActionResult> RejectExternalIntake(
        Guid id,
        Guid intakeId,
        string rejectionReason,
        string? rejectedByUserId,
        CancellationToken cancellationToken)
    {
        var request = new RejectExternalIntakeRequest
        {
            RejectionReason = rejectionReason,
            RejectedByUserId = string.IsNullOrWhiteSpace(rejectedByUserId) ? _currentUserContext.UserId : rejectedByUserId
        };

        var result = await _externalIntakeService.RejectAsync(intakeId, request, cancellationToken);
        TempData["StatusMessage"] = result.Success
            ? "Respuesta externa rechazada."
            : result.Message ?? "No se pudo rechazar la respuesta externa.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    public async Task<IActionResult> AcceptAiSuggestion(Guid id, Guid suggestionId, Guid? targetBranchId, string? reviewNotes, CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.AcceptSuggestionAsync(id, suggestionId, new AcceptRcaAiSuggestionRequest
        {
            TargetBranchId = targetBranchId,
            ReviewedByUserId = _currentUserContext.UserId,
            ReviewNotes = reviewNotes ?? string.Empty
        }, cancellationToken);

        TempData["StatusMessage"] = result.Success
            ? "Sugerencia IA aceptada."
            : $"No se pudo aceptar la sugerencia IA: {string.Join(" ", result.Errors.Select(x => x.Message))}";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RcaRoleNames.QualityGovernance)]
    public async Task<IActionResult> RejectAiSuggestion(Guid id, Guid suggestionId, string? reviewNotes, CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.RejectSuggestionAsync(id, suggestionId, new RejectRcaAiSuggestionRequest
        {
            ReviewedByUserId = _currentUserContext.UserId,
            ReviewNotes = reviewNotes ?? string.Empty
        }, cancellationToken);

        TempData["StatusMessage"] = result.Success
            ? "Sugerencia IA rechazada."
            : $"No se pudo rechazar la sugerencia IA: {string.Join(" ", result.Errors.Select(x => x.Message))}";

        return RedirectToAction(nameof(Details), new { id });
    }

    private static IReadOnlyList<SelectListItem> GetSeverityOptions()
    {
        return
        [
            new SelectListItem("Baja", "Low"),
            new SelectListItem("Media", "Medium"),
            new SelectListItem("Alta", "High"),
            new SelectListItem("Critica", "Critical")
        ];
    }

    private static IReadOnlyList<SelectListItem> GetClaimActorTypeOptions()
    {
        return
        [
            new SelectListItem("Area interna", "InternalArea"),
            new SelectListItem("Cliente", "Customer"),
            new SelectListItem("Proveedor", "Supplier")
        ];
    }

    private async Task<IActionResult> DetailsWithCauseModel(Guid id, AddIshikawaCauseViewModel cause, CancellationToken cancellationToken)
    {
        var details = await BuildDetailsViewModelAsync(id, cancellationToken);
        if (details is null)
        {
            return NotFound();
        }

        details.Cause = cause;

        return View(nameof(Details), details);
    }

    private async Task<IActionResult> DetailsWithActionModel(Guid id, AddCorrectiveActionViewModel action, CancellationToken cancellationToken)
    {
        var details = await BuildDetailsViewModelAsync(id, cancellationToken);
        if (details is null)
        {
            return NotFound();
        }

        details.Action = action;

        return View(nameof(Details), details);
    }

    private async Task<IActionResult> DetailsWithFactModel(Guid id, AddRcaFactViewModel fact, CancellationToken cancellationToken)
    {
        var details = await BuildDetailsViewModelAsync(id, cancellationToken);
        if (details is null)
        {
            return NotFound();
        }

        details.FactForm = fact;

        return View(nameof(Details), details);
    }

    private async Task<RcaIncidentDetailsViewModel?> BuildDetailsViewModelAsync(Guid id, CancellationToken cancellationToken)
    {
        var incidentResult = await _rcaIncidentService.GetByIdAsync(id, cancellationToken);
        if (!incidentResult.Success || incidentResult.Data is null)
        {
            return null;
        }

        var canvasResult = await _rcaIncidentService.GetCanvasAsync(id, cancellationToken);
        if (!canvasResult.Success || canvasResult.Data is null)
        {
            return null;
        }

        var factsResult = await _rcaIncidentService.ListFactsAsync(id, cancellationToken);
        var actionsResult = await _rcaIncidentService.ListCorrectiveActionsAsync(id, cancellationToken);
        var evidenceResult = await _rcaIncidentService.ListEvidenceAsync(id, cancellationToken);
        var externalIntakesResult = await _externalIntakeService.ListByIncidentAsync(id, cancellationToken);
        var timelineResult = await _rcaIncidentService.ListIntegrationEventsAsync(id, cancellationToken: cancellationToken);
        var wizardProgressResult = await _rcaIncidentService.GetWizardProgressAsync(id, cancellationToken);
        var canReviewAiSuggestions = CanReviewAiSuggestions();
        var aiSuggestions = canReviewAiSuggestions
            ? (await _aiAssistantService.ListSuggestionsAsync(id, nameof(RcaAiSuggestionStatus.Pending), cancellationToken)).Data ?? []
            : [];

        var facts = factsResult.Data ?? [];
        var correctiveActions = actionsResult.Data ?? [];
        var evidence = evidenceResult.Data ?? [];
        var externalIntakes = externalIntakesResult.Data ?? [];
        var timelineEvents = timelineResult.Data?
            .OrderByDescending(x => x.OccurredAt)
            .Take(30)
            .ToList() ?? [];

        return new RcaIncidentDetailsViewModel
        {
            Incident = incidentResult.Data,
            Canvas = canvasResult.Data,
            Facts = facts,
            CorrectiveActions = correctiveActions,
            Evidence = evidence,
            ExternalIntakes = externalIntakes,
            TimelineEvents = timelineEvents,
            AiSuggestions = aiSuggestions,
            CanReviewAiSuggestions = canReviewAiSuggestions,
            UnifiedTimeline = BuildUnifiedTimeline(timelineEvents, canvasResult.Data, correctiveActions, evidence, externalIntakes),
            WizardProgress = wizardProgressResult.Data ?? new RcaWizardProgressDto
            {
                IncidentId = id,
                CurrentStep = incidentResult.Data.WizardStep,
                NextRecommendedStep = GetNextWizardStep(incidentResult.Data.WizardStep)
            },
            WizardStepOptions = GetWizardStepOptions(),
            WizardForm = new CompleteRcaWizardStepViewModel
            {
                Step = wizardProgressResult.Data?.NextRecommendedStep ?? GetNextWizardStep(incidentResult.Data.WizardStep)
            }
        };
    }

    private bool CanReviewAiSuggestions()
    {
        return _currentUserContext.IsInRole(RcaRoleNames.Supervisor) ||
            _currentUserContext.IsInRole(RcaRoleNames.Quality) ||
            _currentUserContext.IsInRole(RcaRoleNames.Administrator);
    }

    private static IReadOnlyList<RcaTimelineItemViewModel> BuildUnifiedTimeline(
        IReadOnlyList<RcaDomainEventDto> timelineEvents,
        IshikawaCanvasDto canvas,
        IReadOnlyList<CorrectiveActionDto> correctiveActions,
        IReadOnlyList<RcaEvidenceDto> evidence,
        IReadOnlyList<RcaExternalIntakeDto> externalIntakes)
    {
        var causesById = canvas.Causes.ToDictionary(x => x.Id, x => x.Title);
        var actionsById = correctiveActions.ToDictionary(x => x.Id, x => x.Title);
        var evidenceById = evidence.ToDictionary(x => x.Id, x => x.Title);
        var intakesById = externalIntakes.ToDictionary(
            x => x.Id,
            x => string.IsNullOrWhiteSpace(x.ActorName) ? x.ActorType : $"{x.ActorType}: {x.ActorName}");

        return timelineEvents
            .Select(x => BuildUnifiedTimelineItem(x, causesById, actionsById, evidenceById, intakesById))
            .ToList();
    }

    private static RcaTimelineItemViewModel BuildUnifiedTimelineItem(
        RcaDomainEventDto timelineEvent,
        IReadOnlyDictionary<Guid, string> causesById,
        IReadOnlyDictionary<Guid, string> actionsById,
        IReadOnlyDictionary<Guid, string> evidenceById,
        IReadOnlyDictionary<Guid, string> intakesById)
    {
        var data = timelineEvent.Data;
        var badges = BuildTimelineBadges(timelineEvent.Type, data);
        var references = BuildTimelineReferences(timelineEvent, causesById, actionsById, evidenceById, intakesById);
        var industrialContext = BuildIndustrialContext(timelineEvent.Type, data);

        return new RcaTimelineItemViewModel
        {
            Id = timelineEvent.Id,
            Type = timelineEvent.Type,
            Kind = GetTimelineKind(timelineEvent.Type),
            Label = GetTimelineLabel(timelineEvent.Type),
            Detail = GetTimelineDetail(timelineEvent),
            OccurredAt = timelineEvent.OccurredAt,
            SourceSystem = timelineEvent.SourceSystem,
            Severity = GetData(data, "factSeverity"),
            Badges = badges,
            References = references,
            IndustrialContext = industrialContext
        };
    }

    private static IReadOnlyList<string> BuildTimelineBadges(string eventType, IReadOnlyDictionary<string, string?> data)
    {
        var badges = new List<string>();

        if (eventType.Contains("Fact", StringComparison.OrdinalIgnoreCase))
        {
            AddIfNotEmpty(badges, GetFactTypeLabel(GetData(data, "factType")));
            AddIfNotEmpty(badges, GetFactSourceLabel(GetData(data, "source")));
            AddIfNotEmpty(badges, GetFactSeverityLabel(GetData(data, "factSeverity")));
            return badges;
        }

        if (eventType.Contains("Evidence", StringComparison.OrdinalIgnoreCase))
        {
            AddIfNotEmpty(badges, GetEvidenceTypeLabel(GetData(data, "evidenceType")));
            AddIfNotEmpty(badges, GetFactSourceLabel(GetData(data, "source")));
            AddIfNotEmpty(badges, GetEvidenceValidationLabel(GetData(data, "validationStatus")));
            return badges;
        }

        if (eventType.Contains("Action", StringComparison.OrdinalIgnoreCase))
        {
            AddIfNotEmpty(badges, GetActionStatusLabel(GetData(data, "status")));
            AddIfNotEmpty(badges, FormatDueDate(GetData(data, "dueDate")));
            return badges;
        }

        if (eventType.Contains("ExternalIntake", StringComparison.OrdinalIgnoreCase))
        {
            AddIfNotEmpty(badges, GetExternalActorLabel(GetData(data, "actorType")));
            AddIfNotEmpty(badges, GetExternalStatusLabel(GetData(data, "status")));
            return badges;
        }

        if (eventType.Contains("Wizard", StringComparison.OrdinalIgnoreCase))
        {
            AddIfNotEmpty(badges, GetWizardStepLabel(GetData(data, "step")));
        }

        return badges;
    }

    private static IReadOnlyList<string> BuildTimelineReferences(
        RcaDomainEventDto timelineEvent,
        IReadOnlyDictionary<Guid, string> causesById,
        IReadOnlyDictionary<Guid, string> actionsById,
        IReadOnlyDictionary<Guid, string> evidenceById,
        IReadOnlyDictionary<Guid, string> intakesById)
    {
        var references = new List<string>();
        var data = timelineEvent.Data;

        AddReference(references, "Causa", GetData(data, "causeId"), causesById);
        AddReference(references, "Evidencia", GetData(data, "evidenceId"), evidenceById);
        AddReference(references, "Accion", GetData(data, "actionId"), actionsById);
        AddReference(references, "Accion", GetData(data, "correctiveActionId"), actionsById);
        AddReference(references, "Intake", GetData(data, "intakeId"), intakesById);
        AddReference(references, "Intake", GetData(data, "externalIntakeId"), intakesById);
        AddIfNotEmpty(references, Prefix("Fuente", GetData(data, "sourceDetail")));
        AddIfNotEmpty(references, Prefix("Sistema externo", GetData(data, "externalSourceSystem")));
        AddIfNotEmpty(references, Prefix("Evento externo", GetData(data, "externalEventId")));
        AddIfNotEmpty(references, Prefix("Registro externo", GetData(data, "externalRecordUri")));
        AddIfNotEmpty(references, Prefix("Referencia", GetData(data, "referenceUri")));
        AddIfNotEmpty(references, Prefix("Adjunto", GetData(data, "attachmentFileName")));
        AddIfNotEmpty(references, Prefix("Usuario", GetData(data, "completedByUserId") ?? GetData(data, "reviewedByUserId") ?? GetData(data, "rejectedByUserId") ?? GetData(data, "capturedByUserId")));

        return references.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IReadOnlyList<string> BuildIndustrialContext(string eventType, IReadOnlyDictionary<string, string?> data)
    {
        var context = new List<string>();

        AddIfNotEmpty(context, Prefix("Turno", GetData(data, "shiftCode")));
        AddIfNotEmpty(context, Prefix("Maquina", GetData(data, "machineCode")));
        AddIfNotEmpty(context, Prefix("Linea", GetData(data, "lineCode")));
        AddIfNotEmpty(context, Prefix("OT", GetData(data, "workOrderCode")));
        AddIfNotEmpty(context, Prefix("Material", GetData(data, "materialCode")));
        AddIfNotEmpty(context, Prefix("Lote", GetData(data, "batchOrLot")));
        AddIfNotEmpty(context, Prefix("Alarma", GetData(data, "alarmCode")));

        var measurementName = GetData(data, "measurementName");
        if (!string.IsNullOrWhiteSpace(measurementName))
        {
            var measurementValue = GetData(data, "measurementValue");
            var measurementUnit = GetData(data, "measurementUnit");
            AddIfNotEmpty(context, Prefix("Medicion", JoinParts(" ", measurementName, measurementValue, measurementUnit)));
        }

        if (eventType.Contains("ExternalIntake", StringComparison.OrdinalIgnoreCase))
        {
            AddIfNotEmpty(context, Prefix("Reclamo", GetData(data, "claimReference")));
        }

        return context;
    }

    private static void AddReference(
        List<string> references,
        string label,
        string? id,
        IReadOnlyDictionary<Guid, string> lookup)
    {
        if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out var guid) || !lookup.TryGetValue(guid, out var value))
        {
            return;
        }

        AddIfNotEmpty(references, Prefix(label, value));
    }

    private static string GetTimelineKind(string eventType)
    {
        if (eventType.Contains("Fact", StringComparison.OrdinalIgnoreCase))
        {
            return "fact";
        }

        if (eventType.Contains("ExternalIntake", StringComparison.OrdinalIgnoreCase))
        {
            return "external";
        }

        if (eventType.Contains("Wizard", StringComparison.OrdinalIgnoreCase))
        {
            return "wizard";
        }

        if (eventType.Contains("Evidence", StringComparison.OrdinalIgnoreCase))
        {
            return "evidence";
        }

        if (eventType.Contains("Action", StringComparison.OrdinalIgnoreCase))
        {
            return "action";
        }

        if (eventType.Contains("RootCause", StringComparison.OrdinalIgnoreCase) ||
            eventType.Contains("Cause", StringComparison.OrdinalIgnoreCase))
        {
            return "cause";
        }

        return "incident";
    }

    private static string GetTimelineLabel(string eventType)
    {
        return eventType switch
        {
            "RcaIncidentCreated" => "Incidente creado",
            "RcaCauseCreated" => "Causa agregada",
            "RcaRootCauseSelected" => "Causa raiz seleccionada",
            "RcaCorrectiveActionCreated" => "Accion correctiva creada",
            "RcaCorrectiveActionCompleted" => "Accion correctiva completada",
            "RcaEscalatedTo8D" => "RCA escalado a 8D",
            "RcaWizardStepCompleted" => "Etapa de wizard completada",
            "RcaEvidenceAttached" => "Evidencia agregada",
            "RcaFactRecorded" => "Hecho registrado",
            "RcaExternalIntakeCreated" => "Link externo creado",
            "RcaExternalIntakeOpened" => "Link externo abierto",
            "RcaExternalIntakeSubmitted" => "Respuesta externa enviada",
            "RcaExternalIntakeReviewed" => "Respuesta externa revisada",
            "RcaExternalIntakeRejected" => "Respuesta externa rechazada",
            "RcaExternalIntakeRevoked" => "Link externo revocado",
            "RcaExternalIntakeExpired" => "Link externo expirado",
            "RcaClosed" => "Incidente RCA cerrado",
            _ => eventType
        };
    }

    private static string GetTimelineDetail(RcaDomainEventDto timelineEvent)
    {
        var data = timelineEvent.Data;

        if (data.TryGetValue("title", out var title) && !string.IsNullOrWhiteSpace(title))
        {
            return title;
        }

        if (data.TryGetValue("actorName", out var actorName) && !string.IsNullOrWhiteSpace(actorName))
        {
            return actorName;
        }

        if (data.TryGetValue("actorType", out var actorType) && !string.IsNullOrWhiteSpace(actorType))
        {
            return GetExternalActorLabel(actorType);
        }

        if (data.TryGetValue("notes", out var notes) && !string.IsNullOrWhiteSpace(notes))
        {
            return notes;
        }

        return timelineEvent.SourceSystem;
    }

    private static string? GetData(IReadOnlyDictionary<string, string?> data, string key)
    {
        return data.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
    }

    private static string? Prefix(string label, string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : $"{label}: {value}";
    }

    private static void AddIfNotEmpty(List<string> values, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values.Add(value);
        }
    }

    private static string JoinParts(string separator, params string?[] values)
    {
        return string.Join(separator, values.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string? FormatDueDate(string? dueDate)
    {
        return DateTimeOffset.TryParse(dueDate, out var parsed)
            ? $"Vence {parsed.LocalDateTime:dd/MM/yyyy}"
            : null;
    }

    private static string GetFactTypeLabel(string? factType)
    {
        return factType switch
        {
            "Alarm" => "Alarma",
            "Measurement" => "Medicion",
            "Stop" => "Parada",
            "Inspection" => "Inspeccion",
            "ShiftChange" => "Cambio de turno",
            "Material" => "Material / lote",
            "WorkOrder" => "Orden de trabajo",
            "CustomerClaim" => "Reclamo cliente",
            "SupplierClaim" => "Reclamo proveedor",
            "Containment" => "Contencion",
            _ => "Observacion"
        };
    }

    private static string GetFactSeverityLabel(string? severity)
    {
        return severity switch
        {
            "Low" => "Bajo",
            "Medium" => "Medio",
            "High" => "Alto",
            "Critical" => "Critico",
            _ => "Informativo"
        };
    }

    private static string GetFactSourceLabel(string? source)
    {
        return source switch
        {
            "Operator" => "Operador",
            "Quality" => "Calidad",
            "Maintenance" => "Mantenimiento",
            "Customer" => "Cliente",
            "Supplier" => "Proveedor",
            null => string.Empty,
            _ => source
        };
    }

    private static string GetEvidenceTypeLabel(string? evidenceType)
    {
        return evidenceType switch
        {
            "Document" => "Documento",
            "Photo" => "Foto",
            "Sensor" => "Sensor",
            "Customer" => "Cliente",
            "Supplier" => "Proveedor",
            _ => "Observacion"
        };
    }

    private static string GetEvidenceValidationLabel(string? status)
    {
        return status switch
        {
            "Validated" => "Validada",
            "Rejected" => "Rechazada",
            "Expired" => "Vencida",
            _ => "Pendiente"
        };
    }

    private static string GetActionStatusLabel(string? status)
    {
        return status switch
        {
            "InProgress" => "En progreso",
            "WaitingValidation" => "Esperando validacion",
            "Completed" => "Completada",
            "Cancelled" => "Cancelada",
            "Open" => "Abierta",
            null => string.Empty,
            _ => status
        };
    }

    private static string GetExternalActorLabel(string? actorType)
    {
        return actorType switch
        {
            "Supplier" => "Proveedor",
            "Customer" => "Cliente",
            "InternalArea" => "Area interna",
            null => string.Empty,
            _ => actorType
        };
    }

    private static string GetExternalStatusLabel(string? status)
    {
        return status switch
        {
            "Sent" => "Enviado",
            "Opened" => "Abierto",
            "Submitted" => "Enviado por externo",
            "Reviewed" => "Revisado",
            "Rejected" => "Rechazado",
            "Revoked" => "Revocado",
            "Expired" => "Vencido",
            null => string.Empty,
            _ => status
        };
    }

    private static string GetWizardStepLabel(string? step)
    {
        return step switch
        {
            "Problem" => "Problema",
            "Causes" => "Causas",
            "Evidence" => "Evidencias",
            "Actions" => "Acciones",
            "Validation" => "Validacion",
            "Closed" => "Cierre",
            null => string.Empty,
            _ => step
        };
    }

    private async Task<RcaEvidenceDto?> FindEvidenceAsync(Guid incidentId, Guid evidenceId, CancellationToken cancellationToken)
    {
        var evidenceResult = await _rcaIncidentService.ListEvidenceAsync(incidentId, cancellationToken);
        if (!evidenceResult.Success)
        {
            return null;
        }

        return evidenceResult.Data?.FirstOrDefault(x => x.Id == evidenceId);
    }

    private static IReadOnlyList<SelectListItem> GetWizardStepOptions()
    {
        return
        [
            new SelectListItem("Problema", "Problem"),
            new SelectListItem("Causas", "Causes"),
            new SelectListItem("Evidencias", "Evidence"),
            new SelectListItem("Acciones", "Actions"),
            new SelectListItem("Validacion", "Validation"),
            new SelectListItem("Cierre", "Closed")
        ];
    }

    private static string GetNextWizardStep(string currentStep)
    {
        return currentStep switch
        {
            "Problem" => "Causes",
            "Causes" => "Evidence",
            "Evidence" => "Actions",
            "Actions" => "Validation",
            "Validation" => "Closed",
            "Closed" => "Closed",
            _ => "Problem"
        };
    }

    private static bool IsImageContentType(string? contentType)
    {
        return !string.IsNullOrWhiteSpace(contentType) &&
               contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPreviewableContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
               contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ||
               contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
               contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
               contentType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
               contentType.Equals("application/xml", StringComparison.OrdinalIgnoreCase) ||
               contentType.Equals("text/csv", StringComparison.OrdinalIgnoreCase);
    }
}
