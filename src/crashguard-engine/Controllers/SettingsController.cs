using Crashguard.Common.DTOs;
using Crashguard.Engine.Data;
using Crashguard.Engine.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crashguard.Engine.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SettingsDto>> Get(CancellationToken ct)
    {
        var settings = await db.Settings.SingleAsync(ct);
        return Ok(ToDto(settings));
    }

    [HttpPut]
    public async Task<ActionResult<SettingsDto>> Update(UpdateSettingsRequest request, CancellationToken ct)
    {
        var settings = await db.Settings.SingleAsync(ct);
        settings.AdminPortalUrl = string.IsNullOrWhiteSpace(request.AdminPortalUrl)
            ? null
            : request.AdminPortalUrl.TrimEnd('/');
        settings.SmtpHost = string.IsNullOrWhiteSpace(request.SmtpHost) ? null : request.SmtpHost;
        settings.SmtpPort = request.SmtpPort;
        settings.SmtpUsername = string.IsNullOrWhiteSpace(request.SmtpUsername) ? null : request.SmtpUsername;
        settings.SmtpPassword = string.IsNullOrWhiteSpace(request.SmtpPassword) ? null : request.SmtpPassword;
        settings.SmtpFromAddress = string.IsNullOrWhiteSpace(request.SmtpFromAddress) ? null : request.SmtpFromAddress;
        settings.SmtpFromName = string.IsNullOrWhiteSpace(request.SmtpFromName) ? null : request.SmtpFromName;
        settings.SmtpUseTls = request.SmtpUseTls;
        settings.ResolvedRetentionDays = request.ResolvedRetentionDays;
        settings.TriggeredRetentionDays = request.TriggeredRetentionDays;

        await db.SaveChangesAsync(ct);
        return Ok(ToDto(settings));
    }

    private static SettingsDto ToDto(Models.Settings settings) => new()
    {
        AdminPortalUrl = settings.AdminPortalUrl,
        SmtpHost = settings.SmtpHost,
        SmtpPort = settings.SmtpPort,
        SmtpUsername = settings.SmtpUsername,
        SmtpPassword = settings.SmtpPassword,
        SmtpFromAddress = settings.SmtpFromAddress,
        SmtpFromName = settings.SmtpFromName,
        SmtpUseTls = settings.SmtpUseTls,
        ResolvedRetentionDays = settings.ResolvedRetentionDays,
        TriggeredRetentionDays = settings.TriggeredRetentionDays,
    };
}
