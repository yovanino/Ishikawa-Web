using IshikawaRca.Domain.Entities;
using IshikawaRca.Domain.Enums;
using IshikawaRca.Domain.Services;

var rootOnlyActions = new[]
{
    NewAction(CorrectiveActionType.Corrective, RcaResolutionScope.RootCause),
    NewAction(CorrectiveActionType.Preventive, RcaResolutionScope.RootCause)
};

AssertContains(
    RcaResolutionPolicy.GetResolutionBlockers(rootOnlyActions, hasEscapeAnalysis: false),
    "Falta una accion preventiva de recurrencia para la causa raiz.");

var documentedEscapeWithoutActions = new[]
{
    NewAction(CorrectiveActionType.Corrective, RcaResolutionScope.RootCause),
    NewAction(CorrectiveActionType.Preventive, RcaResolutionScope.RootCause),
    NewAction(CorrectiveActionType.RecurrencePreventive, RcaResolutionScope.RootCause)
};

AssertContains(
    RcaResolutionPolicy.GetResolutionBlockers(documentedEscapeWithoutActions, hasEscapeAnalysis: true),
    "La FUGA requiere accion correctiva, preventiva y preventiva de recurrencia.");

var completeResolution = new[]
{
    NewAction(CorrectiveActionType.Corrective, RcaResolutionScope.RootCause),
    NewAction(CorrectiveActionType.Preventive, RcaResolutionScope.RootCause),
    NewAction(CorrectiveActionType.RecurrencePreventive, RcaResolutionScope.RootCause),
    NewAction(CorrectiveActionType.Corrective, RcaResolutionScope.Escape),
    NewAction(CorrectiveActionType.Preventive, RcaResolutionScope.Escape),
    NewAction(CorrectiveActionType.RecurrencePreventive, RcaResolutionScope.Escape)
};

AssertEmpty(RcaResolutionPolicy.GetResolutionBlockers(completeResolution, hasEscapeAnalysis: true));

static CorrectiveAction NewAction(CorrectiveActionType type, RcaResolutionScope scope)
{
    return new CorrectiveAction
    {
        ActionType = type,
        ResolutionScope = scope
    };
}

static void AssertContains(IReadOnlyList<string> values, string expected)
{
    if (!values.Contains(expected, StringComparer.Ordinal))
    {
        throw new InvalidOperationException($"Expected blocker '{expected}'. Actual: {string.Join(" | ", values)}");
    }
}

static void AssertEmpty(IReadOnlyList<string> values)
{
    if (values.Count != 0)
    {
        throw new InvalidOperationException($"Expected no blockers. Actual: {string.Join(" | ", values)}");
    }
}
