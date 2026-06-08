using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace IshikawaRca.Web.Security;

public class StandaloneRcaAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "StandaloneRca";

    private readonly StandaloneRcaAuthenticationOptions _options;

    public StandaloneRcaAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<StandaloneRcaAuthenticationOptions> standaloneOptions)
        : base(options, logger, encoder)
    {
        _options = standaloneOptions.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var tenantId = ResolveHeader("X-RCA-TenantId") ?? _options.DefaultTenantId;
        if (!Guid.TryParse(tenantId, out var tenantGuid) || tenantGuid == Guid.Empty)
        {
            return Task.FromResult(AuthenticateResult.Fail("RcaSecurity:DefaultTenantId debe ser un GUID valido."));
        }

        var userId = ResolveHeader("X-RCA-UserId") ?? _options.DefaultUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.Fail("RcaSecurity:DefaultUserId es obligatorio."));
        }

        var roles = ResolveRoles();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, userId.Trim()),
            new("rca_tenant_id", tenantGuid.ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private string? ResolveHeader(string name)
    {
        if (!_options.AllowHeaderOverrides ||
            !Request.Headers.TryGetValue(name, out var values))
        {
            return null;
        }

        var value = values.FirstOrDefault();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private IReadOnlyList<string> ResolveRoles()
    {
        var headerRoles = ResolveHeader("X-RCA-Roles");
        var roles = string.IsNullOrWhiteSpace(headerRoles)
            ? _options.DefaultRoles
            : headerRoles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

