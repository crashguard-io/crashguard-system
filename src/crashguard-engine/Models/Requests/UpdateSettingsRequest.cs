namespace Crashguard.Engine.Models.Requests;

public class UpdateSettingsRequest
{
    public string? AdminPortalUrl { get; set; }
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public string? SmtpFromAddress { get; set; }
    public string? SmtpFromName { get; set; }
    public bool SmtpUseTls { get; set; } = true;
    public int? ResolvedRetentionDays { get; set; }
    public int? TriggeredRetentionDays { get; set; }
}
