namespace IshikawaRca.Domain.Common;

public abstract class TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? CreatedByUserId { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public string? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }
}
