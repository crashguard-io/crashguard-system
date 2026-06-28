# crashguard-engine

The CrashGuard Engine — the API that tracks canaries, checkpoints, and fires alerts when deadlines are missed.

## Running it standalone

```
dotnet run --project src/crashguard-engine
```

By default this listens on `http://localhost:5050` (set in
[`appsettings.json`](appsettings.json) and [`Properties/launchSettings.json`](Properties/launchSettings.json)).

Data is stored in SQLite. With no `ConnectionStrings:DefaultConnection` configured, it defaults to a file under
your local app data folder (e.g. `%LOCALAPPDATA%\Crashguard\crashguard.db` on Windows). Set
`ConnectionStrings__DefaultConnection` to point it elsewhere.

## Configuring the listen port

The engine reads its listen URL from Kestrel config, in order of precedence:

1. **`ASPNETCORE_URLS` environment variable** — e.g. `ASPNETCORE_URLS=http://+:5050`. This is what the Docker image
   uses internally and is the easiest way to override the port without touching files.
2. **`Kestrel:Endpoints:Http:Url` in `appsettings.json`** (or `appsettings.Development.json`) — currently set to
   `http://localhost:5050`.
3. **`applicationUrl` in `Properties/launchSettings.json`** — only applies when running via `dotnet run` /
   Visual Studio's "Project" launch profile; this is what's actually used for local `dotnet run` sessions and
   currently overrides `appsettings.json` to also use `5050`.

Whatever port you land on, that's the URL the [crashguard-app](../crashguard-app/README.md) (via
`VITE_API_BASE_URL`) and the [crashguard-client SDK](../crashguard-client/) (via the `RestClient` you construct)
both need to point at.

## CORS

The engine accepts cross-origin requests from any origin, including `origin: null` (needed for the Stream Deck
plugin and other non-browser/file:// callers) — see the `Frontend` CORS policy in [`Program.cs`](Program.cs). This
is intentional: the engine is meant to be an open API on a trusted internal network. If you need to expose it
beyond that, put it behind a reverse proxy (e.g. nginx) that restricts access, or tighten the CORS policy yourself.
