namespace IshikawaRca.Contracts.Rca;

public class IshikawaCanvasDto
{
    public Guid RcaIncidentId { get; set; }

    public string ProblemTitle { get; set; } = string.Empty;

    public IReadOnlyList<IshikawaBranchDto> Branches { get; set; } = [];

    public IReadOnlyList<IshikawaCauseDto> Causes { get; set; } = [];
}
