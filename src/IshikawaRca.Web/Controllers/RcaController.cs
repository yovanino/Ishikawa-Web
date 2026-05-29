using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Web.Models.Rca;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IshikawaRca.Web.Controllers;

public class RcaController : Controller
{
    private static readonly Guid DemoTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly IRcaIncidentService _rcaIncidentService;

    public RcaController(IRcaIncidentService rcaIncidentService)
    {
        _rcaIncidentService = rcaIncidentService;
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
        ViewBag.ClaimScopes = GetClaimScopeOptions();

        return View(new CreateRcaIncidentViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRcaIncidentViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Severities = GetSeverityOptions();
            ViewBag.ClaimScopes = GetClaimScopeOptions();
            return View(model);
        }

        var request = new CreateRcaIncidentRequest
        {
            TenantId = DemoTenantId,
            Title = model.Title,
            ProblemDescription = model.ProblemDescription,
            Severity = model.Severity,
            ClaimScope = model.ClaimScope,
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
            ViewBag.ClaimScopes = GetClaimScopeOptions();
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

    private static IReadOnlyList<SelectListItem> GetClaimScopeOptions()
    {
        return
        [
            new SelectListItem("Interno - area", "Internal"),
            new SelectListItem("Externo - cliente", "External")
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

        return new RcaIncidentDetailsViewModel
        {
            Incident = incidentResult.Data,
            Canvas = canvasResult.Data,
            CorrectiveActions = actionsResult.Data ?? []
        };
    }
}
