namespace IshikawaRca.Contracts.Rca;

public class RcaAiContextDto
{
    public RcaIncidentDto Incident { get; set; } = new();

    public IshikawaCanvasDto Canvas { get; set; } = new();

    public IReadOnlyList<CorrectiveActionDto> CorrectiveActions { get; set; } = [];
}
