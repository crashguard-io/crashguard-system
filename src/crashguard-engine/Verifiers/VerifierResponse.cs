namespace Crashguard.Engine.Verifiers;

/// <summary>
/// The action a verifier tells the engine to take for an overdue canary. Sent back as JSON
/// (e.g. <c>{ "action": "extend" }</c>) from the URL configured on <see cref="Models.CanaryType.VerifierUrl"/>.
/// </summary>
public enum VerifierAction
{
    Trigger,
    Extend,
    Resolve,
}

public class VerifierResponse
{
    public VerifierAction Action { get; set; }
}
