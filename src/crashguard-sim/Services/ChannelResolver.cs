namespace Crashguard.Sim.Services;

// Resolves the sim's well-known Slack channels by severity. The engine is expected to already have
// channels named cg-sim-critical, cg-sim-warning, and cg-sim-information configured — the sim only
// looks them up, it never creates them.
public class ChannelResolver(EngineClient engineClient, ILogger<ChannelResolver> logger)
{
    private static readonly Dictionary<string, string> ChannelNamesBySeverity = new()
    {
        ["critical"] = "cg-sim-critical",
        ["warning"] = "cg-sim-warning",
        ["info"] = "cg-sim-information",
    };

    private Dictionary<string, int>? _channelIdsBySeverity;

    public async Task LoadAsync(CancellationToken ct)
    {
        var channels = await engineClient.GetChannelsAsync(ct);
        var channelIdsByName = channels.ToDictionary(c => c.Name, c => c.Id);

        var resolved = new Dictionary<string, int>();
        foreach (var (severity, channelName) in ChannelNamesBySeverity)
        {
            if (channelIdsByName.TryGetValue(channelName, out var id))
            {
                resolved[severity] = id;
            }
            else
            {
                logger.LogWarning(
                    "No channel named '{ChannelName}' is configured; canaries with severity '{Severity}' will be created without a default channel.",
                    channelName, severity);
            }
        }

        _channelIdsBySeverity = resolved;
    }

    public List<int> GetDefaultChannelIds(string severity)
    {
        if (_channelIdsBySeverity is null)
        {
            throw new InvalidOperationException($"{nameof(LoadAsync)} must be called before resolving channels.");
        }

        return _channelIdsBySeverity.TryGetValue(severity, out var id) ? [id] : [];
    }
}
