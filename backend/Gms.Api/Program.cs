using System.Text;
using System.Threading.RateLimiting;
using Gms.Api.Common;
using Gms.Api.Common.Authorization;
using Gms.Api.Common.Observability;
using Gms.Api.Data;
using Gms.Api.Services.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// ── CORS (Bearer tokens → no credentials needed; explicit origins only) ──
const string AngularCorsPolicy = "AngularDevCors";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:18420" };
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
});

// ── EF Core (SQL Server) ──
builder.Services.AddDbContext<GmsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── JWT options (fail fast if the signing key is missing/weak) ──
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.SigningKey) || Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32)
    throw new InvalidOperationException("Jwt:SigningKey ayarlı değil veya çok kısa (min 32 bayt). Ortam değişkeni/secret ile sağlayın.");

// ── Authentication (JWT bearer) ──
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep raw claim names (sub, role, perm)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = GmsClaimTypes.Role,
            NameClaimType = GmsClaimTypes.UserId
        };
    });

// ── Authorization (one policy per permission code) ──
builder.Services.AddAuthorization(options => options.AddPermissionPolicies());
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

// ── Identity / current-user seam (JWT-backed) ──
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, JwtCurrentUser>();

// ── Auth services ──
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// ── Rate limiting (auth endpoints) ──
// Strict by default (10/min/IP); relax in Development via config for local testing.
var authPermitLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitLimit", 10);
var webhookPermitLimit = builder.Configuration.GetValue("RateLimiting:IntegrationWebhookPermitLimit", 20);
var integrationSensitiveLimit = builder.Configuration.GetValue("RateLimiting:IntegrationSensitivePermitLimit", 30);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPermitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Incoming webhooks: partition per integration code so one integration cannot exhaust others.
    options.AddPolicy("integration-webhook", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Request.RouteValues.TryGetValue("integrationCode", out var codeVal) && codeVal is string code
                ? $"wh:{code}"
                : $"wh-ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = webhookPermitLimit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
            }));

    // Sensitive integration actions (connection test / dispatch / retry): partition per caller.
    options.AddPolicy("integration-sensitive", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.FindFirst(GmsClaimTypes.UserId)?.Value
                ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = integrationSensitiveLimit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
            }));
});

// ── Ortak altyapı servisleri ──
builder.Services.AddScoped<Gms.Api.Services.SequentialNumberGenerator>();

// ── Domain servisleri ──
builder.Services.AddScoped<Gms.Api.Services.ChangeRiskService>();
builder.Services.AddScoped<Gms.Api.Services.ChangeReadinessService>();
builder.Services.AddScoped<Gms.Api.Services.ApprovalChainService>();
builder.Services.AddScoped<Gms.Api.Services.ApprovalService>();
builder.Services.AddScoped<Gms.Api.Services.ReleaseRiskService>();
builder.Services.AddScoped<Gms.Api.Services.ReleasePlanningService>();
builder.Services.AddScoped<Gms.Api.Services.ExecutionService>();
builder.Services.AddScoped<Gms.Api.Services.ValidationService>();

// Document Management (paylaşımlı doküman altyapısı)
builder.Services.AddSingleton<Gms.Api.Services.Documents.IFileStorage, Gms.Api.Services.Documents.LocalFileStorage>();
builder.Services.AddScoped<Gms.Api.Services.Documents.DocumentService>();

// Notification Engine (merkezi bildirim altyapısı)
builder.Services.AddSingleton<Gms.Api.Services.Notifications.IEmailProvider, Gms.Api.Services.Notifications.DummyEmailProvider>();
builder.Services.AddScoped<Gms.Api.Services.Notifications.NotificationService>();

// Unified Audit & Reporting (salt-okunur sorgu/rapor katmanı)
builder.Services.AddScoped<Gms.Api.Services.Reporting.AuditReadService>();
builder.Services.AddScoped<Gms.Api.Services.Reporting.IReportingService, Gms.Api.Services.Reporting.ReportingService>();
builder.Services.AddSingleton<Gms.Api.Services.Reporting.IReportExportService, Gms.Api.Services.Reporting.ReportExportService>();

// Workflow Engine (sürümlü, yeniden kullanılabilir yönetişim akışları)
builder.Services.AddScoped<Gms.Api.Services.Workflow.WorkflowDefinitionService>();
builder.Services.AddScoped<Gms.Api.Services.Workflow.WorkflowRuntimeService>();

// Integration Hub (merkezi dış entegrasyon altyapısı)
builder.Services.Configure<Gms.Api.Services.Integrations.IntegrationOptions>(
    builder.Configuration.GetSection(Gms.Api.Services.Integrations.IntegrationOptions.SectionName));
builder.Services.AddDataProtection();
builder.Services.AddSingleton<Gms.Api.Services.Integrations.ISecretProtector, Gms.Api.Services.Integrations.DataProtectionSecretProtector>();
builder.Services.AddSingleton<Gms.Api.Services.Integrations.IIntegrationDelayStrategy>(sp =>
{
    var baseSeconds = builder.Configuration.GetValue($"{Gms.Api.Services.Background.BackgroundProcessingOptions.SectionName}:RetryBaseDelaySeconds", 30);
    return new Gms.Api.Services.Integrations.ExponentialDelayStrategy(TimeSpan.FromSeconds(Math.Clamp(baseSeconds, 1, 3600)));
});
// Typed HTTP client (factory-based; no automatic redirect following).
builder.Services.AddHttpClient(Gms.Api.Services.Integrations.IntegrationHttpClient.ClientName)
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, MaxAutomaticRedirections = 1 });
builder.Services.AddScoped<Gms.Api.Services.Integrations.IntegrationHttpClient>();
// Provider adapters (resolved by IntegrationDefinition.Provider — no scattered switch statements).
builder.Services.AddScoped<Gms.Api.Services.Integrations.Providers.IIntegrationProvider, Gms.Api.Services.Integrations.Providers.GenericRestIntegrationProvider>();
builder.Services.AddScoped<Gms.Api.Services.Integrations.Providers.IIntegrationProvider, Gms.Api.Services.Integrations.Providers.IncomingWebhookIntegrationProvider>();
builder.Services.AddScoped<Gms.Api.Services.Integrations.Providers.IIntegrationProvider, Gms.Api.Services.Integrations.Providers.OutgoingWebhookIntegrationProvider>();
builder.Services.AddScoped<Gms.Api.Services.Integrations.Providers.IIntegrationProvider, Gms.Api.Services.Integrations.Providers.DummySmtpIntegrationProvider>();
builder.Services.AddScoped<Gms.Api.Services.Integrations.Providers.IIntegrationProvider, Gms.Api.Services.Integrations.Providers.AzureDevOpsSandboxProvider>();
builder.Services.AddScoped<Gms.Api.Services.Integrations.Providers.IIntegrationProvider, Gms.Api.Services.Integrations.Providers.JiraSandboxProvider>();
builder.Services.AddScoped<Gms.Api.Services.Integrations.Providers.IIntegrationProviderResolver, Gms.Api.Services.Integrations.Providers.IntegrationProviderResolver>();
builder.Services.AddScoped<Gms.Api.Services.Integrations.IIntegrationEventPublisher, Gms.Api.Services.Integrations.IntegrationEventPublisher>();
builder.Services.AddScoped<Gms.Api.Services.Integrations.IIntegrationService, Gms.Api.Services.Integrations.IntegrationService>();
builder.Services.AddScoped<Gms.Api.Services.Integrations.IIntegrationDispatcher, Gms.Api.Services.Integrations.IntegrationDispatcher>();

// ── Background processing (BackgroundService workers over DB-backed records) ──
builder.Services.Configure<Gms.Api.Services.Background.BackgroundProcessingOptions>(
    builder.Configuration.GetSection(Gms.Api.Services.Background.BackgroundProcessingOptions.SectionName));
builder.Services.Configure<Gms.Api.Services.Background.HealthOptions>(
    builder.Configuration.GetSection(Gms.Api.Services.Background.HealthOptions.SectionName));
builder.Services.AddScoped<Gms.Api.Services.Background.NotificationDeliveryDispatcher>();
builder.Services.AddScoped<Gms.Api.Services.Background.IOperationalStatusService, Gms.Api.Services.Background.OperationalStatusService>();
// Workers are singletons (also exposed for controlled run-once via the registry) + hosted.
builder.Services.AddSingleton<Gms.Api.Services.Background.IntegrationDispatchWorker>();
builder.Services.AddSingleton<Gms.Api.Services.Background.NotificationDeliveryWorker>();
builder.Services.AddSingleton<Gms.Api.Services.Background.WorkflowSlaWorker>();
builder.Services.AddSingleton<Gms.Api.Services.Background.OperationalCleanupWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<Gms.Api.Services.Background.IntegrationDispatchWorker>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<Gms.Api.Services.Background.NotificationDeliveryWorker>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<Gms.Api.Services.Background.WorkflowSlaWorker>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<Gms.Api.Services.Background.OperationalCleanupWorker>());
builder.Services.AddSingleton(sp => new Gms.Api.Services.Background.WorkerRegistry(new Gms.Api.Services.Background.BackgroundWorkerBase[]
{
    sp.GetRequiredService<Gms.Api.Services.Background.IntegrationDispatchWorker>(),
    sp.GetRequiredService<Gms.Api.Services.Background.NotificationDeliveryWorker>(),
    sp.GetRequiredService<Gms.Api.Services.Background.WorkflowSlaWorker>(),
    sp.GetRequiredService<Gms.Api.Services.Background.OperationalCleanupWorker>()
}));

// ── Observability (OpenTelemetry tracing + metrics over BCL ActivitySource/Meter) ──
builder.Services.Configure<Gms.Api.Services.Background.ObservabilityOptions>(
    builder.Configuration.GetSection(Gms.Api.Services.Background.ObservabilityOptions.SectionName));
var obs = builder.Configuration.GetSection(Gms.Api.Services.Background.ObservabilityOptions.SectionName)
    .Get<Gms.Api.Services.Background.ObservabilityOptions>() ?? new Gms.Api.Services.Background.ObservabilityOptions();
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(obs.ServiceName))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation()
            .AddSource(Gms.Api.Common.Observability.GmsTelemetry.SourceName);
        if (obs.EnableConsoleExporter) t.AddConsoleExporter();
    })
    .WithMetrics(m =>
    {
        m.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation()
            .AddMeter(Gms.Api.Common.Observability.GmsTelemetry.MeterName);
        if (obs.EnableConsoleExporter) m.AddConsoleExporter();
    });

// ── Health checks (SQL / Data Protection / storage / worker freshness / backlog) ──
builder.Services.AddHealthChecks()
    .AddCheck<Gms.Api.Common.Health.SqlServerHealthCheck>("sqlserver", tags: new[] { "ready" })
    .AddCheck<Gms.Api.Common.Health.DataProtectionHealthCheck>("dataprotection", tags: new[] { "ready" })
    .AddCheck<Gms.Api.Common.Health.StorageHealthCheck>("storage", tags: new[] { "ready" })
    .AddCheck<Gms.Api.Common.Health.WorkerFreshnessHealthCheck>("workers", tags: new[] { "ready" })
    .AddCheck<Gms.Api.Common.Health.BacklogHealthCheck>("backlog", tags: new[] { "ready" });

builder.Services.AddControllers();

// ── Swagger (+ Bearer JWT) ──
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GMS API",
        Version = "v1",
        Description = "Kurumsal Yönetişim Yönetim Sistemi — JWT/RBAC korumalı API"
    });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Bearer {access token}",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

var app = builder.Build();

// ── HTTP pipeline ──
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "GMS API v1"));
}

app.UseHttpsRedirection();
app.UseCors(AngularCorsPolicy);
app.UseRateLimiter();

// Domain/auth exception → HTTP mapping (400/401/403/409)
app.UseMiddleware<Gms.Api.Common.DomainExceptionMiddleware>();

app.UseAuthentication();
// Correlation id: after authentication (so UserId enrichment works) and before authorization; runs
// for every request (authentication never short-circuits) so the response always carries the id.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "GMS API",
    timestamp = DateTime.UtcNow
}))
.WithName("HealthCheck")
.WithTags("System")
.AllowAnonymous();

// Liveness: process is up (no dependency checks). Readiness: DB/storage/config/backlog/workers.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = c => c.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
}).AllowAnonymous();

app.MapControllers();

// ── Development-only: set seeded user password hashes at startup ──
if (app.Environment.IsDevelopment())
{
    await DevDataSeeder.SeedAsync(app.Services, app.Configuration,
        app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DevDataSeeder"));
}

app.Run();

// Minimal readiness response — status + per-check status/description only (no stack traces/secrets).
static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description
        })
    });
}

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
