using IshikawaRca.Contracts.Rca;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IshikawaRca.Web.Models.Rca;

public class RcaIncidentDetailsViewModel
{
    public RcaIncidentDto Incident { get; set; } = new();

    public IshikawaCanvasDto Canvas { get; set; } = new();

    public IReadOnlyList<CorrectiveActionDto> CorrectiveActions { get; set; } = [];

    public AddIshikawaCauseViewModel Cause { get; set; } = new();

    public AddCorrectiveActionViewModel Action { get; set; } = new();

    public IReadOnlyList<SelectListItem> BranchOptions =>
        Canvas.Branches
            .OrderBy(x => x.Order)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

    public IReadOnlyList<SelectListItem> CauseOptions =>
        Canvas.Causes
            .OrderByDescending(x => x.IsRootCause)
            .ThenBy(x => x.Title)
            .Select(x => new SelectListItem(x.Title, x.Id.ToString()))
            .ToList();
}
