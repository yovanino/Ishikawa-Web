namespace IshikawaRca.Contracts.Rca;

public class CreatedExternalIntakeDto
{
    public RcaExternalIntakeDto Intake { get; set; } = new();

    public string Token { get; set; } = string.Empty;
}
