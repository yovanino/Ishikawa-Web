namespace IshikawaRca.Contracts.Rca;

public class RcaDashboardSummaryDto
{
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public int TotalIncidents { get; set; }
    public int OpenIncidents { get; set; }
    public int ClosedIncidents { get; set; }
    public int EscalatedTo8DIncidents { get; set; }
    public int OpenCorrectiveActions { get; set; }
    public int OverdueCorrectiveActions { get; set; }
    public int PendingOutboxEvents { get; set; }
    public int FailedOutboxEvents { get; set; }
    public int DeadLetterOutboxEvents { get; set; }
    public IReadOnlyList<string> SourceSystems { get; set; } = [];
}
