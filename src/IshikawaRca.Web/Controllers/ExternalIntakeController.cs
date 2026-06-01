using IshikawaRca.Application.Rca;
using IshikawaRca.Contracts.Rca;
using IshikawaRca.Web.Models.Rca;
using Microsoft.AspNetCore.Mvc;

namespace IshikawaRca.Web.Controllers;

[Route("external-intake")]
public class ExternalIntakeController : Controller
{
    private readonly IRcaExternalIntakeService _externalIntakeService;

    public ExternalIntakeController(IRcaExternalIntakeService externalIntakeService)
    {
        _externalIntakeService = externalIntakeService;
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> Index(string token, CancellationToken cancellationToken)
    {
        var result = await _externalIntakeService.GetByTokenAsync(token, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return View("Unavailable", result.Message ?? "Link externo no disponible.");
        }

        var model = new ExternalIntakePortalViewModel
        {
            Token = token,
            Intake = result.Data,
            ContactName = result.Data.ContactName,
            ContactEmail = result.Data.ContactEmail,
            ClaimReference = result.Data.ClaimReference,
            MaterialCode = result.Data.MaterialCode,
            BatchOrLot = result.Data.BatchOrLot,
            Description = result.Data.Description ?? string.Empty,
            ContainmentResponse = result.Data.ContainmentResponse,
            ProposedRootCause = result.Data.ProposedRootCause,
            ProposedCorrectiveAction = result.Data.ProposedCorrectiveAction,
            EvidenceSummary = result.Data.EvidenceSummary
        };

        return View(model);
    }

    [HttpPost("{token}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(string token, ExternalIntakePortalViewModel model, CancellationToken cancellationToken)
    {
        var intakeResult = await _externalIntakeService.GetByTokenAsync(token, cancellationToken);
        if (!intakeResult.Success || intakeResult.Data is null)
        {
            return View("Unavailable", intakeResult.Message ?? "Link externo no disponible.");
        }

        model.Token = token;
        model.Intake = intakeResult.Data;

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var request = new SubmitExternalIntakeRequest
        {
            ContactName = model.ContactName,
            ContactEmail = model.ContactEmail,
            ClaimReference = model.ClaimReference,
            MaterialCode = model.MaterialCode,
            BatchOrLot = model.BatchOrLot,
            Description = model.Description,
            ContainmentResponse = model.ContainmentResponse,
            ProposedRootCause = model.ProposedRootCause,
            ProposedCorrectiveAction = model.ProposedCorrectiveAction,
            EvidenceSummary = model.EvidenceSummary
        };

        var result = await _externalIntakeService.SubmitAsync(token, request, cancellationToken);
        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }

            return View("Index", model);
        }

        return View("Submitted", result.Data);
    }
}
