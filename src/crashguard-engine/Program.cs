using Crashguard.Engine;
using Crashguard.Engine.Connectors;
using Crashguard.Engine.Data;
using Crashguard.Engine.Services;
using Crashguard.Engine.Verifiers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (connectionString is null)
{
    var dataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Crashguard");
    Directory.CreateDirectory(dataDir);
    connectionString = $"Data Source={Path.Combine(dataDir, "crashguard.db")}";
}
else
{
    var dbPath = new SqliteConnectionStringBuilder(connectionString).DataSource;
    var dbDir = Path.GetDirectoryName(Path.GetFullPath(dbPath));
    if (dbDir is not null) Directory.CreateDirectory(dbDir);
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString)
           .UseSnakeCaseNamingConvention());

builder.Services.AddHostedService<Service>();

// Connectors: each implements IConnector and is registered here. Adding a new connector type
// (PagerDuty, Email, ...) means adding a new class plus one line here — nothing else changes.
builder.Services.AddSingleton<RestClient>();
builder.Services.AddSingleton<SlackConnector>();
builder.Services.AddSingleton<IConnector>(sp => sp.GetRequiredService<SlackConnector>());
builder.Services.AddSingleton<WebhookConnector>();
builder.Services.AddSingleton<IConnector>(sp => sp.GetRequiredService<WebhookConnector>());
builder.Services.AddSingleton<EmailConnector>();
builder.Services.AddSingleton<IConnector>(sp => sp.GetRequiredService<EmailConnector>());
builder.Services.AddSingleton<ConnectorRegistry>();
builder.Services.AddSingleton<IVerifierClient, VerifierClient>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    options.JsonSerializerOptions.Converters.Add(new UtcNullableDateTimeConverter());
});
builder.Services.AddOpenApi();

const string FrontendCorsPolicy = "Frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.SetIsOriginAllowed(origin =>
                  origin == "null" || (Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Host == "localhost"))
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // WAL is persisted in the DB file header, so setting it once here is enough for
    // every future connection (including the admin app and Stream Deck plugins) to
    // read a consistent snapshot while a write transaction is in progress, instead
    // of blocking on the writer as the default rollback journal does.
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseAuthorization();
app.MapControllers();

app.Run();
