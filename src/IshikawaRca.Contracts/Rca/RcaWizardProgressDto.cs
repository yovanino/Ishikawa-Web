namespace IshikawaRca.Contracts.Rca;

public class RcaWizardProgressDto
{
    public Guid IncidentId { get; set; }

    public string CurrentStep { get; set; } = string.Empty;

    public string NextRecommendedStep { get; set; } = string.Empty;

    public int CompletionPercent { get; set; }

    public List<RcaWizardStepChecklistItemDto> Steps { get; set; } = [];
}
