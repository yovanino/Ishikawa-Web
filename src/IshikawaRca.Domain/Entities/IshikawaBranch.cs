using IshikawaRca.Domain.Common;

namespace IshikawaRca.Domain.Entities;

public class IshikawaBranch : TenantEntity
{
    public Guid RcaIncidentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int Order { get; set; }

    public string? Color { get; set; }

    public ICollection<IshikawaCause> Causes { get; set; } = new List<IshikawaCause>();
}
