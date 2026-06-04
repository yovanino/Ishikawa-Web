using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Web.Models.Rca;
using IshikawaRca.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IshikawaRca.Web.Controllers;

public class RcaController : Controller
{
    private static readonly Guid DemoTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly IRcaIncidentService _rcaIncidentService;
    private readonly IRcaExternalIntakeService _externalIntakeService;
    private readonly IEvidenceFileStorage _evidenceFileStorage;

    public RcaController(
        IRcaIncidentService rcaIncidentService,
        IRcaExternalIntakeService externalIntakeService,
        IEvidenceFileStorage evidenceFileStorage)
    {
        _rcaIncidentService = rcaIncidentService;
        _externalIntakeService = externalIntakeService;
        _evidenceFileStorage = evidenceFileStorage;
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
            TenantId = DemoTenantId,
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
            ReportedBy = model.ReportedBy
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
            CompletedByUserId = model.CompletedByUserId,
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
            ValidatedByUserId = model.ValidatedByUserId,
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

        var result = await _rcaIncidentService.ReplaceEvidenceAttachmentAsync(id, model.EvidenceId, request, cancellationToken);
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
    public async Task<IActionResult> DeleteEvidence(Guid id, Guid evidenceId, CancellationToken cancellationToken)
    {
        var existingEvidence = await FindEvidenceAsync(id, evidenceId, cancellationToken);
        if (existingEvidence is null)
        {
            return NotFound();
        }

        var result = await _rcaIncidentService.DeleteEvidenceAsync(id, evidenceId, cancellationToken);
        if (result.Success)
        {
            _evidenceFileStorage.Delete(existingEvidence.AttachmentStorageKey);
        }

        TempData["StatusMessage"] = result.Success
            ? "Evidencia eliminada."
            : result.Message ?? "No se pudo eliminar la evidencia.";

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

    [HttpPost]
    [ValidateAntiForgeryToken]
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
            ClosedByUserId = model.ClosedByUserId,
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
            EscalatedByUserId = model.EscalatedByUserId,
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
    public async Task<IActionResult> RevokeExternalIntake(Guid id, Guid intakeId, CancellationToken cancellationToken)
    {
        var result = await _externalIntakeService.RevokeAsync(intakeId, cancellationToken);
        TempData["StatusMessage"] = result.Success ? "Link externo revocado." : "No se pudo revocar el link externo.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
            ReviewedByUserId = reviewedByUserId
        };

        var result = await _externalIntakeService.ReviewAsync(intakeId, request, cancellationToken);
        TempData["StatusMessage"] = result.Success
            ? "Respuesta externa revisada e importada."
            : result.Message ?? "No se pudo revisar la respuesta externa.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
            RejectedByUserId = rejectedByUserId
        };

        var result = await _externalIntakeService.RejectAsync(intakeId, request, cancellationToken);
        TempData["StatusMessage"] = result.Success
            ? "Respuesta externa rechazada."
            : result.Message ?? "No se pudo rechazar la respuesta externa.";

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

        var actionsResult = await _rcaIncidentService.ListCorrectiveActionsAsync(id, cancellationToken);
        var evidenceResult = await _rcaIncidentService.ListEvidenceAsync(id, cancellationToken);
        var externalIntakesResult = await _externalIntakeService.ListByIncidentAsync(id, cancellationToken);
        var timelineResult = await _rcaIncidentService.ListIntegrationEventsAsync(id, cancellationToken: cancellationToken);
        var wizardProgressResult = await _rcaIncidentService.GetWizardProgressAsync(id, cancellationToken);

        return new RcaIncidentDetailsViewModel
        {
            Incident = incidentResult.Data,
            Canvas = canvasResult.Data,
            CorrectiveActions = actionsResult.Data ?? [],
            Evidence = evidenceResult.Data ?? [],
            ExternalIntakes = externalIntakesResult.Data ?? [],
            TimelineEvents = timelineResult.Data?
                .OrderByDescending(x => x.OccurredAt)
                .Take(20)
                .ToList() ?? [],
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
