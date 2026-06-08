namespace IshikawaRca.Web.Security;

public class StandaloneRcaAuthenticationOptions
{
    public string DefaultTenantId { get; set; } = string.Empty;

    public string DefaultUserId { get; set; } = "standalone-user";

    public string[] DefaultRoles { get; set; } =
    [
        RcaRoleNames.Operator,
        RcaRoleNames.Supervisor,
        RcaRoleNames.Quality,
        RcaRoleNames.Maintenance,
        RcaRoleNames.Administrator
    ];

    public bool AllowHeaderOverrides { get; set; }
}

