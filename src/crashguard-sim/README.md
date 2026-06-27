# crashguard-sim

A traffic generator for CrashGuard. It spawns canaries against a running
[crashguard-engine](../crashguard-engine/README.md), checkpoints them, resolves most of them, and lets the rest
expire — useful for exercising alerting, dedup, and load without wiring up real workflows.

## Running it

```
dotnet run --project src/crashguard-sim
```

This requires a running engine to talk to — see [crashguard-engine's README](../crashguard-engine/README.md) for
how to start one and configure its listen port.

## Configuring the engine destination

The sim talks to the engine through the [crashguard-client](../crashguard-client/) SDK, pointed at whatever
`Engine:BaseUrl` resolves to. Set it in [`appsettings.json`](appsettings.json) (currently `http://localhost:5050`)
or override it with an environment variable:

```
Engine__BaseUrl=http://localhost:5050
```

This needs to match the engine's actual listen URL — if you changed the engine's port (via `ASPNETCORE_URLS`,
`appsettings.json`, or `launchSettings.json`), update this to match.

## The sim's own port(s)

The sim is itself a small web app — it exposes a `/api/verify` endpoint that the engine calls back into during
the load test (see [`VerifyController`](Controllers/VerifyController.cs)). Its listen port is configured
two ways at once, and both end up active:

- **`Api:Port`** in [`appsettings.json`](appsettings.json) (default `5055`) — explicitly bound in `Program.cs` via
  `ConfigureKestrel`, and is the port the load test tells the engine to call back on.
- **`Kestrel:Endpoints:Http:Url`** in `appsettings.json` / **`applicationUrl`** in `launchSettings.json` (default
  `http://localhost:5060`) — the standard ASP.NET Core listen URL.

If you change one, make sure you mean to — `Api:Port` controls what the load test advertises to the engine as its
verifier callback, while the Kestrel URL is just where the sim's own web host binds.

## launchSettings.json profiles

Run a specific profile with `dotnet run --project src/crashguard-sim --launch-profile <name>`:

| Profile | What it does |
|---|---|
| `http` | Default — runs the steady-state simulator ([`Service`](Services/Service.cs)): spawns canaries at a slow trickle plus periodic "waves" of bursty canaries, checkpoints them, and resolves ~60% of them while letting the rest expire. Good for a realistic, low-volume background load. |
| `https` | Same as `http`, but also binds an HTTPS endpoint (`https://localhost:7164`). |
| `load-test` | Runs [`LoadTestService`](Services/LoadTestService.cs) instead (`-load 1000 -delay-ms 150`): fires 1000 canaries at the engine and uses `/api/verify` to resolve them all after a 150ms delay, to stress-test throughput without triggering alerts. |
| `load-test-slow-verifier` | Same load test shape, but `-load 100 -delay-ms 5000` — fewer canaries with a slow (5s) verifier response, to exercise the engine's handling of slow/backed-up verification calls. |
| `outage-test` | Runs [`OutageTestService`](Services/OutageTestService.cs) (`-outage 5`): simulates an outage by spawning 5 batches of canaries that are never resolved, so they all expire and trigger alerts — useful for testing alert dedup and channel delivery under a failure burst. |

The `-load`, `-delay-ms`, and `-outage` flags are read directly from `commandLineArgs` in each profile — to run a
different size or delay, either edit the profile in [`Properties/launchSettings.json`](Properties/launchSettings.json)
or pass your own args directly: `dotnet run --project src/crashguard-sim -- -load 50 -delay-ms 1000`.
