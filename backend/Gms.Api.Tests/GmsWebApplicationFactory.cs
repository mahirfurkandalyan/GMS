using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Collections.Concurrent;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Services.Auth;
using Gms.Api.Services.Integrations;
using Gms.Api.Services.Notifications;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gms.Api.Tests;

/// <summary>
/// Deterministic email provider for delivery-worker tests. Behaviour is keyed on the subject:
/// contains "[[PERM]]" → permanent failure; "[[FAIL]]" → transient failure; otherwise success.
/// Records successful send counts per subject so tests can assert no duplicate sends.
/// </summary>
public sealed class TestEmailProvider : IEmailProvider
{
    public ConcurrentDictionary<string, int> SendCounts { get; } = new();

    public Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (message.Subject.Contains("[[PERM]]")) throw new EmailPermanentException("test kalıcı hata");
        if (message.Subject.Contains("[[FAIL]]")) return Task.FromResult(false);
        SendCounts.AddOrUpdate(message.Subject, 1, (_, c) => c + 1);
        return Task.FromResult(true);
    }
}

/// <summary>
/// Deterministic HTTP handler for integration outgoing calls in tests (no real internet). Routes
/// by URL substring: "/transient" → 503, "/permanent" → 400, "/timeout" → 408, otherwise 200.
/// </summary>
public sealed class TestIntegrationHttpHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.ToString() ?? string.Empty;
        HttpStatusCode code = url switch
        {
            _ when url.Contains("/transient") => HttpStatusCode.ServiceUnavailable,
            _ when url.Contains("/permanent") => HttpStatusCode.BadRequest,
            _ when url.Contains("/timeout") => (HttpStatusCode)408,
            _ when url.Contains("/fail") => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.OK
        };
        var body = code == HttpStatusCode.OK ? "{\"ok\":true}" : "{\"error\":\"simulated\"}";
        return Task.FromResult(new HttpResponseMessage(code) { Content = new StringContent(body) });
    }
}

/// <summary>
/// Integration-test host backed by a REAL, isolated SQL Server database (GmsDb_Test on
/// the same Docker instance). The DB is dropped + migrated + password-seeded once per
/// factory so every run starts from a deterministic state. No EF InMemory — relational
/// behaviour (unique indexes, rowversion, cascades) is exercised for real.
///
/// The DB is swapped via ConfigureServices (post-registration) rather than config, so it
/// applies reliably; the JWT signing key comes from appsettings.Development.json for both
/// signing and validation (consistent).
/// </summary>
public sealed class GmsWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string DevPassword = "Gms.Dev.2026!";
    private const string TestConnectionString =
        "Server=localhost,1433;Database=GmsDb_Test;User Id=sa;Password=Your_strong_Passw0rd;TrustServerCertificate=True;Encrypt=False";

    /// <summary>Isolated document storage root for tests (lets tests tamper files for integrity checks).</summary>
    public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), "gms-test-documents");

    /// <summary>Controllable email provider (subject markers drive success/transient/permanent).</summary>
    public TestEmailProvider Email { get; } = new();

    public GmsWebApplicationFactory()
    {
        var options = new DbContextOptionsBuilder<GmsDbContext>().UseSqlServer(TestConnectionString).Options;
        using var db = new GmsDbContext(options);
        db.Database.EnsureDeleted();
        db.Database.Migrate();

        var passwords = new PasswordService();
        foreach (var user in db.Users.Where(u => u.PasswordHash == string.Empty).ToList())
            user.PasswordHash = passwords.Hash(user, DevPassword);
        db.SaveChanges();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // Host settings are read at startup (unlike late config), so the small webhook window
        // applies to the rate-limiter registration in Program.cs.
        builder.UseSetting("RateLimiting:IntegrationWebhookPermitLimit", "5");
        // Disable the automatic worker loop in tests — workers are driven deterministically via
        // RunOnceAsync / the run-once endpoint. Large batches so one run drains all pending rows.
        builder.UseSetting("BackgroundProcessing:Enabled", "false");
        builder.UseSetting("BackgroundProcessing:IntegrationDispatch:BatchSize", "500");
        builder.UseSetting("BackgroundProcessing:NotificationDelivery:BatchSize", "500");
        builder.UseSetting("BackgroundProcessing:WorkflowSla:BatchSize", "500");
        // Late-read config: the LocalFileStorage singleton is constructed at request time,
        // so this override is honoured (unlike eager startup reads).
        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:DocumentsRoot"] = StorageRoot,
                // Small webhook window so the rate-limit test triggers deterministically (per integration code).
                ["RateLimiting:IntegrationWebhookPermitLimit"] = "5"
            }));
        builder.ConfigureServices(services =>
        {
            // Replace the app's DbContext registration with the isolated test database.
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<GmsDbContext>) ||
                d.ServiceType == typeof(GmsDbContext)).ToList();
            foreach (var d in toRemove) services.Remove(d);

            services.AddDbContext<GmsDbContext>(o => o.UseSqlServer(TestConnectionString));

            // Integration Hub: deterministic outgoing HTTP + no retry delays (hermetic tests).
            services.AddHttpClient(IntegrationHttpClient.ClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new TestIntegrationHttpHandler());
            services.RemoveAll<IIntegrationDelayStrategy>();
            services.AddSingleton<IIntegrationDelayStrategy, ImmediateDelayStrategy>();

            // Controllable email provider (replaces the dummy) so delivery-worker tests can force
            // transient/permanent failures and assert no duplicate sends.
            services.RemoveAll<IEmailProvider>();
            services.AddSingleton<IEmailProvider>(Email);
        });
    }

    public async Task<AuthResponse> LoginAsync(string email, string? password = null)
    {
        var client = CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = password ?? DevPassword });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public async Task<HttpClient> CreateAuthedClientAsync(string email)
    {
        var login = await LoginAsync(email);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }
}
