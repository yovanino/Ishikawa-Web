namespace IshikawaRca.Web.Security;

public interface ICurrentRcaUserContext
{
    Guid TenantId { get; }

    string UserId { get; }

    bool IsInRole(string role);
}

