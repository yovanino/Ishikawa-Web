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

        return View(new CreateRcaIncidentViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRcaIncidentViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Severities = GetSeverityOptions();
            return View(model);
        }

        var request = new CreateRcaIncidentRequest
        {
            TenantId = DemoTenantId,
            Title = model.Title,
            ProblemDescription = model.ProblemDescription,
            Severity = model.Severity,
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
            return View(model);
        }

        TempData["StatusMessage"] = "Incidente RCA creado.";

        return RedirectToAction(nameof(Details), new { id = result.Data.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var result = await _rcaIncidentService.GetByIdAsync(id, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return NotFound();
        }

        return View(result.Data);
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
}
