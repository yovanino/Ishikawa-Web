namespace IshikawaRca.Contracts.Rca;

public class CompleteRcaWizardStepRequest
{
    public string Step { get; set; } = string.Empty;

    public string? CompletedByUserId { get; set; }

    public string? Notes { get; set; }
}
