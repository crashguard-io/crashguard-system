using Microsoft.AspNetCore.Mvc;

namespace Crashguard.Sim.Controllers;

// Verifier endpoint for the load test: the engine calls this for each overdue load-test canary,
// and it always reports Resolve after a fixed delay so the batch drains without ever alerting.
[ApiController]
[Route("api/verify")]
public class VerifyController : ControllerBase
{
    private const int DefaultDelayMs = 150;

    [HttpPost]
    public async Task<IActionResult> Verify([FromQuery] int? delayMs, CancellationToken ct)
    {
        await Task.Delay(delayMs ?? DefaultDelayMs, ct);
        return Ok(new { action = "Resolve" });
    }
}
