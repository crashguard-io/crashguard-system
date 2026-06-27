using Crashguard.Client;
using Crashguard.Sim;
using Crashguard.Sim.Models;
using Crashguard.Sim.Services;
using RestSharp;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext());

var engineBaseUrl = builder.Configuration["Engine:BaseUrl"] ?? "http://localhost:5050";
builder.Services.AddSingleton(new RestClient(engineBaseUrl));
builder.Services.AddSingleton<EngineClient>();
builder.Services.AddSingleton<CrashguardClient>();
builder.Services.AddSingleton<ChannelResolver>();

var apiPort = builder.Configuration.GetValue<int?>("Api:Port") ?? 5055;
builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(apiPort));

var loadTestCount = ParseIntArg(args, "-load");
var outageBatchCount = ParseIntArg(args, "-outage");

if (loadTestCount is { } count)
{
    var verifyDelayMs = ParseIntArg(args, "-delay-ms") ?? 150;
    builder.Services.AddSingleton(new LoadTestOptions { CanaryCount = count, ApiPort = apiPort, VerifyDelayMs = verifyDelayMs });
    builder.Services.AddHostedService<LoadTestService>();
}
else if (outageBatchCount is { } batches)
{
    builder.Services.AddSingleton(new OutageTestOptions { BatchCount = batches });
    builder.Services.AddHostedService<OutageTestService>();
}
else
{
    builder.Services.AddHostedService<Service>();
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

static int? ParseIntArg(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == name && int.TryParse(args[i + 1], out var n))
        {
            return n;
        }
    }
    return null;
}
