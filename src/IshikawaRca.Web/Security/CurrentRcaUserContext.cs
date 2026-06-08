using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace IshikawaRca.Web.Security;

public class CurrentRcaUserContext : ICurrentRcaUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly StandaloneRcaAuthenticationOptions _options;

    public CurrentRcaUserContext(
        IHttpContextAccessor httpContextAccessor,
        IOptions<StandaloneRcaAuthenticationOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
    }

    public Guid TenantId
    {
        get
        {
            var tenantId = User.FindFirstValue("rca_tenant_id") ?? _options.DefaultTenantId;
            if (Guid.TryParse(tenantId, out var guid) && guid != Guid.Empty)
            {
                return guid;
            }

            throw new InvalidOperationException("No hay tenant RCA valido en el contexto actual.");
        }
    }

    public string UserId => User.Identity?.Name ?? _options.DefaultUserId;

    public bool IsInRole(string role)
    {
        return User.IsInRole(role);
    }

    private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
}

