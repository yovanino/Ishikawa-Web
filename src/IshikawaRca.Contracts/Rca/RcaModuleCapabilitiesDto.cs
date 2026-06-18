namespace IshikawaRca.Contracts.Rca;

public class RcaModuleCapabilitiesDto
{
    public string ModuleKey { get; set; } = "ishikawa-rca";
    public string DisplayName { get; set; } = "Ishikawa RCA";
    public string ApiVersion { get; set; } = "v1";
    public string BasePath { get; set; } = "/api/v1";
    public string MvcBasePath { get; set; } = "/Rca";
    public bool SupportsSnapshots { get; set; } = true;
    public bool SupportsIntegrationEvents { get; set; } = true;
    public bool SupportsLiveEvents { get; set; } = true;
    public bool SupportsOutbox { get; set; } = true;
    public bool SupportsWebhooks { get; set; } = true;
    public bool SupportsClosureDocuments { get; set; } = true;
    public bool SupportsAiAssistance { get; set; } = true;
    public bool SupportsExternalIntake { get; set; } = true;
    public IReadOnlyList<string> IntegrationEndpoints { get; set; } = [];
}
