using IshikawaRca.Domain.Common;

namespace IshikawaRca.Domain.Entities;

public class IshikawaCause : TenantEntity
{
    public Guid RcaIncidentId { get; set; }

    public Guid BranchId { get; set; }

    public Guid? ParentCauseId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal X { get; set; }

    public decimal Y { get; set; }

    public int ProbabilityScore { get; set; }

    public int ImpactScore { get; set; }

    public int FrequencyScore { get; set; }

    public bool IsRootCause { get; set; }

    public string? EvidenceSummary { get; set; }
}
