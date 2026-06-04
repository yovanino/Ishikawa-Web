using IshikawaRca.Domain.Entities;
using IshikawaRca.Domain.Enums;

namespace IshikawaRca.Domain.Services;

public static class RcaResolutionPolicy
{
    public const string RootCauseRecurrenceBlocker = "Falta una accion preventiva de recurrencia para la causa raiz.";

    public const string EscapeActionSetBlocker = "La FUGA requiere accion correctiva, preventiva y preventiva de recurrencia.";

    public static IReadOnlyList<string> GetResolutionBlockers(
        IReadOnlyCollection<CorrectiveAction> actions,
        bool hasEscapeAnalysis)
    {
        var blockers = new List<string>();

        if (!HasAction(actions, RcaResolutionScope.RootCause, CorrectiveActionType.RecurrencePreventive))
        {
            blockers.Add(RootCauseRecurrenceBlocker);
        }

        if (hasEscapeAnalysis && !HasCompleteActionSet(actions, RcaResolutionScope.Escape))
        {
            blockers.Add(EscapeActionSetBlocker);
        }

        return blockers;
    }

    public static bool HasCompleteActionSet(
        IReadOnlyCollection<CorrectiveAction> actions,
        RcaResolutionScope scope)
    {
        return HasAction(actions, scope, CorrectiveActionType.Corrective) &&
            HasAction(actions, scope, CorrectiveActionType.Preventive) &&
            HasAction(actions, scope, CorrectiveActionType.RecurrencePreventive);
    }

    private static bool HasAction(
        IReadOnlyCollection<CorrectiveAction> actions,
        RcaResolutionScope scope,
        CorrectiveActionType type)
    {
        return actions.Any(x =>
            x.ResolutionScope == scope &&
            x.ActionType == type &&
            !x.IsDeleted &&
            x.Status is not CorrectiveActionStatus.Cancelled);
    }
}
