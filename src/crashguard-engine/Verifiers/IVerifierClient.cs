using Crashguard.Engine.Models;

namespace Crashguard.Engine.Verifiers;

public interface IVerifierClient
{
    Task<VerifierResponse> VerifyAsync(string verifierUrl, Canary canary, CancellationToken ct);
}
