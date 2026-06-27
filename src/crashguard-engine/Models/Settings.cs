namespace Crashguard.Engine.Models;

/// <summary>
/// Single-row table holding instance-wide configuration set by the admin. There is always exactly
/// one row, with a fixed Id of 1 — see AppDbContext's seed data.
/// </summary>
public class Settings
{
    public int Id { get; set; }

    /// <summary>
    /// The externally-reachable URL of the admin portal (e.g. "http://my-host:9080"), used to build
    /// links in alert messages. The engine cannot infer this on its own: it runs behind a reverse
    /// proxy inside the container, and has no way to know which host port the operator mapped it to.
    /// Left null until the admin sets it; alert messages omit the link in that case.
    /// </summary>
    public string? AdminPortalUrl { get; set; }

    /// <summary>
    /// SMTP relay used by the Email connector to send alerts. Configured once here rather than per
    /// channel, since channels only need to name a recipient address — see <see cref="Connectors.EmailConnector"/>.
    /// All fields are null until the admin configures email; the connector throws if it's used before that.
    /// </summary>
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public string? SmtpFromAddress { get; set; }
    public string? SmtpFromName { get; set; }
    public bool SmtpUseTls { get; set; } = true;

    /// <summary>
    /// How many days to keep a canary after it's Resolved before it's purged. Null means keep
    /// forever (no automatic purge for Resolved canaries).
    /// </summary>
    public int? ResolvedRetentionDays { get; set; }

    /// <summary>
    /// How many days to keep a canary after it's Triggered before it's purged. Null means keep
    /// forever (no automatic purge for Triggered canaries).
    /// </summary>
    public int? TriggeredRetentionDays { get; set; }
}
