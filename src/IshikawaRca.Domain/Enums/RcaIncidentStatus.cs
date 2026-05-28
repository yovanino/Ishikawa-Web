namespace IshikawaRca.Domain.Enums;

public enum RcaIncidentStatus
{
    Draft = 0,
    Open = 1,
    InAnalysis = 2,
    WaitingValidation = 3,
    Closed = 4,
    EscalatedTo8D = 5,
    Cancelled = 6
}
