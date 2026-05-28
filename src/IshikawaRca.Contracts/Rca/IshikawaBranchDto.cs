namespace IshikawaRca.Contracts.Rca;

public class IshikawaBranchDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int Order { get; set; }

    public string? Color { get; set; }
}
