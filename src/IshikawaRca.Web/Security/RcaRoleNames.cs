namespace IshikawaRca.Web.Security;

public static class RcaRoleNames
{
    public const string Operator = "Operator";
    public const string Supervisor = "Supervisor";
    public const string Quality = "Quality";
    public const string Maintenance = "Maintenance";
    public const string Administrator = "Administrator";

    public const string SensitiveOperations = $"{Supervisor},{Quality},{Maintenance},{Administrator}";
    public const string QualityGovernance = $"{Supervisor},{Quality},{Administrator}";
}

