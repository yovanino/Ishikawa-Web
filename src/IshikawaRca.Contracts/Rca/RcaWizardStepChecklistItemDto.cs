namespace IshikawaRca.Contracts.Rca;

public class RcaWizardStepChecklistItemDto
{
    public string Step { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool IsCurrent { get; set; }

    public bool IsCompleted { get; set; }

    public bool IsBlocked { get; set; }

    public List<string> Requirements { get; set; } = [];

    public List<string> BlockingReasons { get; set; } = [];

    public Dictionary<string, string> Metrics { get; set; } = [];
}
