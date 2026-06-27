using System.Text.Json;
using Crashguard.Common.DTOs;
using Crashguard.Engine.Data;
using Crashguard.Engine.Models;
using Crashguard.Engine.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crashguard.Engine.Controllers;

[ApiController]
[Route("api/channels")]
public class ChannelsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ChannelDto>>> List(CancellationToken ct)
    {
        var channels = await db.Channels.OrderBy(c => c.Name).ToListAsync(ct);
        return Ok(channels.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ChannelDto>> Get(int id, CancellationToken ct)
    {
        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (channel is null) return NotFound();

        return Ok(ToDto(channel));
    }

    [HttpPost]
    public async Task<ActionResult<ChannelDto>> Create(CreateChannelRequest request, CancellationToken ct)
    {
        var channel = new Channel
        {
            Name = request.Name,
            Type = request.Type,
            Config = request.Config.GetRawText(),
            CreatedAt = DateTime.UtcNow,
        };

        db.Channels.Add(channel);
        await db.SaveChangesAsync(ct);

        return Ok(ToDto(channel));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ChannelDto>> Update(int id, UpdateChannelRequest request, CancellationToken ct)
    {
        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (channel is null) return NotFound();

        channel.Name = request.Name;
        channel.Type = request.Type;
        channel.Config = request.Config.GetRawText();
        await db.SaveChangesAsync(ct);

        return Ok(ToDto(channel));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (channel is null) return NotFound();

        db.Channels.Remove(channel);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    private static ChannelDto ToDto(Channel channel) => new()
    {
        Id = channel.Id,
        Name = channel.Name,
        Type = channel.Type,
        Config = JsonSerializer.Deserialize<JsonElement>(channel.Config),
        CreatedAt = channel.CreatedAt,
    };
}
