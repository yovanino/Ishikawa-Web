namespace IshikawaRca.Domain.Enums;

public enum RcaOutboxEventStatus
{
    Pending = 0,
    Publishing = 1,
    Published = 2,
    Failed = 3,
    DeadLetter = 4
}
