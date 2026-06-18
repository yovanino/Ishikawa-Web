using IshikawaRca.Contracts.Rca;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IshikawaRca.Web.Models.Rca;

public class RcaIncidentDetailsViewModel
{
    public RcaIncidentDto Incident { get; set; } = new();

    public IshikawaCanvasDto Canvas { get; set; } = new();

    public IReadOnlyList<CorrectiveActionDto> CorrectiveActions { get; set; } = [];

    public IReadOnlyList<RcaExternalIntakeDto> ExternalIntakes { get; set; } = [];

    public IReadOnlyList<RcaEvidenceDto> Evidence { get; set; } = [];

    public IReadOnlyList<RcaFactDto> Facts { get; set; } = [];

    public IReadOnlyList<RcaDomainEventDto> TimelineEvents { get; set; } = [];

    public IReadOnlyList<RcaTimelineItemViewModel> UnifiedTimeline { get; set; } = [];

    public IReadOnlyList<RcaAiSuggestionDto> AiSuggestions { get; set; } = [];

    public RcaWizardProgressDto WizardProgress { get; set; } = new();

    public AddIshikawaCauseViewModel Cause { get; set; } = new();

    public AddCorrectiveActionViewModel Action { get; set; } = new();

    public CreateExternalIntakeViewModel ExternalIntake { get; set; } = new();

    public AddRcaEvidenceViewModel EvidenceForm { get; set; } = new();

    public AddRcaFactViewModel FactForm { get; set; } = new();

    public CloseRcaIncidentViewModel CloseForm { get; set; } = new();

    public EscalateRcaIncidentTo8DViewModel EscalateForm { get; set; } = new();

    public CompleteRcaWizardStepViewModel WizardForm { get; set; } = new();

    public IReadOnlyList<SelectListItem> WizardStepOptions { get; set; } = [];

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
