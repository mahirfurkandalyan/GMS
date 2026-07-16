using System.Net;
using System.Net.Http.Json;
using System.Text;
using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Services.Integrations.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gms.Api.Tests;

/// <summary>
/// Integration Hub tests against a REAL SQL Server. External HTTP is intercepted by a deterministic
/// test handler (no internet). Covers secret protection, connection tests, incoming webhook
/// security, the outbox/dispatcher/retry model, external links, RBAC, unified audit and reporting.
/// </summary>
[Collection("gms")]
public sealed class IntegrationTests
{
    private readonly GmsWebApplicationFactory _factory;
    public IntegrationTests(GmsWebApplicationFactory factory) => _factory = factory;

    /* ── definitions & credentials ────────────────────────── */

    [Fact] // 1 — Admin creates an integration
    public async Task Admin_CreatesIntegration()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var created = await CreateAsync(admin, GenericRest("http://mock.local/success"));
        Assert.StartsWith("INT-", created.IntegrationNo);
        Assert.Equal("Draft", created.Status);
    }

    [Fact] // 2 — a non-admin cannot manage credentials
    public async Task NonAdmin_CannotManageCredentials()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var integ = await CreateAsync(admin, GenericRest("http://mock.local/success"));

        var auditor = await _factory.CreateAuthedClientAsync(Seed.Auditor);
        var resp = await auditor.PostAsJsonAsync($"/api/integrations/{integ.Id}/credentials",
            new CreateCredentialDto { KeyName = "ApiKey", Value = "x" });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact] // 3 & 4 — secret is encrypted at rest and never returned raw
    public async Task Secret_EncryptedAtRest_AndNeverReturned()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var integ = await CreateAsync(admin, GenericRest("http://mock.local/success", auth: "ApiKey"));
        const string raw = "super-secret-APIKEY-123456";
        var credResp = await admin.PostAsJsonAsync($"/api/integrations/{integ.Id}/credentials",
            new CreateCredentialDto { KeyName = "ApiKey", Value = raw });
        credResp.EnsureSuccessStatusCode();
        var cred = await credResp.Content.ReadFromJsonAsync<IntegrationCredentialDto>();
        Assert.DoesNotContain(raw, cred!.MaskedValue);

        // DTO detail never exposes the raw or encrypted value.
        var detail = await admin.GetFromJsonAsync<IntegrationDetailDto>($"/api/integrations/{integ.Id}");
        var serialized = System.Text.Json.JsonSerializer.Serialize(detail);
        Assert.DoesNotContain(raw, serialized);

        // Encrypted at rest: DB stores a protected value, not the plaintext.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GmsDbContext>();
        var stored = await db.IntegrationCredentials.AsNoTracking().FirstAsync(c => c.IntegrationDefinitionId == integ.Id);
        Assert.NotEqual(raw, stored.EncryptedValue);
        Assert.DoesNotContain(raw, stored.EncryptedValue);
    }

    /* ── connection test / activation ─────────────────────── */

    [Fact] // 5 — connection test success
    public async Task ConnectionTest_Success()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var integ = await CreateAsync(admin, GenericRest("http://mock.local/success"));
        var resp = await admin.PostAsync($"/api/integrations/{integ.Id}/test-connection", null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.True((await resp.Content.ReadFromJsonAsync<ConnectionTestResultDto>())!.Success);
    }

    [Fact] // 6 — connection test failure creates an audit event
    public async Task ConnectionTest_Failure_CreatesAuditEvent()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var integ = await CreateAsync(admin, GenericRest("http://mock.local/fail"));
        var resp = await admin.PostAsync($"/api/integrations/{integ.Id}/test-connection", null);
        Assert.False((await resp.Content.ReadFromJsonAsync<ConnectionTestResultDto>())!.Success);

        var events = await admin.GetFromJsonAsync<List<IntegrationEventDto>>($"/api/integrations/{integ.Id}/audit");
        Assert.Contains(events!, e => e.EventType == "ConnectionTestFailed");
    }

    [Fact] // 7 — activation requires valid configuration
    public async Task Activation_RequiresValidConfiguration()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var integ = await CreateAsync(admin, GenericRest(baseUrl: null)); // GenericRest needs BaseUrl
        var resp = await admin.PostAsync($"/api/integrations/{integ.Id}/activate", null);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    /* ── incoming webhooks ────────────────────────────────── */

    [Fact] // 8 — incoming webhook with valid secret is accepted (202)
    public async Task IncomingWebhook_ValidSecret_Accepted()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var (integ, secret) = await CreateActiveIncomingAsync(admin);
        var resp = await SendWebhookAsync(integ.Code, "{\"hello\":\"world\"}", secretHeader: secret);
        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
    }

    [Fact] // 9 — invalid webhook secret returns 401
    public async Task IncomingWebhook_InvalidSecret_401()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var (integ, _) = await CreateActiveIncomingAsync(admin);
        var resp = await SendWebhookAsync(integ.Code, "{\"a\":1}", secretHeader: "wrong-secret");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact] // 10 — duplicate delivery returns 409
    public async Task IncomingWebhook_DuplicateDelivery_409()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var (integ, secret) = await CreateActiveIncomingAsync(admin);
        var first = await SendWebhookAsync(integ.Code, "{\"a\":1}", secretHeader: secret, deliveryId: "dlv-1");
        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        var second = await SendWebhookAsync(integ.Code, "{\"a\":1}", secretHeader: secret, deliveryId: "dlv-1");
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact] // 11 — HMAC signature path is accepted (secret-safe validation)
    public async Task IncomingWebhook_HmacSignature_Accepted()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var (integ, secret) = await CreateActiveIncomingAsync(admin);
        var body = "{\"event\":\"x\"}";
        var sig = ProviderHelpers.ComputeSignature(secret, body);
        var resp = await SendWebhookAsync(integ.Code, body, signatureHeader: sig);
        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
    }

    [Fact] // 12 — the webhook endpoint is rate limited (429 after the window is exhausted)
    public async Task IncomingWebhook_RateLimited()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var (integ, secret) = await CreateActiveIncomingAsync(admin);
        var sawTooMany = false;
        for (var i = 0; i < 9; i++)
        {
            var resp = await SendWebhookAsync(integ.Code, "{\"n\":" + i + "}", secretHeader: secret);
            if (resp.StatusCode == HttpStatusCode.TooManyRequests) { sawTooMany = true; break; }
        }
        Assert.True(sawTooMany, "Beklenen 429 (rate limit) alınmadı.");
    }

    /* ── outbox / dispatcher / retry ──────────────────────── */

    [Fact] // 13 — a supported GMS event creates a Pending outgoing execution
    public async Task Event_CreatesPendingExecution()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var integ = await CreateActiveOutgoingWithSubscriptionAsync(admin, "success");
        await FireWorkflowCompletedAsync();

        var pending = await GetExecutionsAsync(admin, integ.Id, "Pending");
        Assert.NotEmpty(pending);
    }

    [Fact] // 14 — the dispatcher marks a successful execution Succeeded (attempt recorded)
    public async Task Dispatcher_Success_MarksSucceeded()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var integ = await CreateActiveOutgoingWithSubscriptionAsync(admin, "success");
        await FireWorkflowCompletedAsync();
        var execId = (await GetExecutionsAsync(admin, integ.Id, "Pending")).First().Id;

        var detail = await RetryAsync(admin, execId);
        Assert.Equal("Succeeded", detail.Status);
        Assert.Single(detail.Attempts);
        Assert.Equal("Succeeded", detail.Attempts[0].Status);
    }

    [Fact] // 15 & 16 — transient failure retries, then the retry limit moves it to DeadLetter
    public async Task Dispatcher_TransientFailure_RetriesThenDeadLetter()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var integ = await CreateActiveOutgoingWithSubscriptionAsync(admin, "transient");
        await FireWorkflowCompletedAsync();
        var execId = (await GetExecutionsAsync(admin, integ.Id, "Pending")).First().Id;

        var a1 = await RetryAsync(admin, execId);
        Assert.Equal("Failed", a1.Status); // transient, retryable
        var a2 = await RetryAsync(admin, execId);
        Assert.Equal("Failed", a2.Status);
        var a3 = await RetryAsync(admin, execId);
        Assert.Equal("DeadLetter", a3.Status); // reached max attempts
        Assert.Equal(3, a3.Attempts.Count);
    }

    [Fact] // 17 — a permanent failure dead-letters without over-retrying
    public async Task Dispatcher_PermanentFailure_DeadLetters()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var integ = await CreateActiveOutgoingWithSubscriptionAsync(admin, "permanent");
        await FireWorkflowCompletedAsync();
        var execId = (await GetExecutionsAsync(admin, integ.Id, "Pending")).First().Id;

        var detail = await RetryAsync(admin, execId);
        Assert.Equal("DeadLetter", detail.Status); // non-transient → immediate dead-letter
        Assert.Single(detail.Attempts);
    }

    [Fact] // 18 — dispatch-pending processes queued executions
    public async Task DispatchPending_ProcessesQueue()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var integ = await CreateActiveOutgoingWithSubscriptionAsync(admin, "success");
        await FireWorkflowCompletedAsync();

        var resp = await admin.PostAsync("/api/integration-executions/dispatch-pending?max=100", null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var succeeded = (await GetExecutionsAsync(admin, integ.Id, "Succeeded")).Count;
        Assert.True(succeeded >= 1);
    }

    /* ── external links ───────────────────────────────────── */

    [Fact] // 19 & 20 — external object link creation, and duplicate is rejected
    public async Task ExternalLink_Create_AndDuplicateRejected()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var jira = await CreateAsync(admin, Jira());
        var changeId = await CreateChangeAsync(admin);

        var body = new CreateExternalLinkDto { InternalObjectType = "ChangeRequest", InternalObjectId = changeId, ExternalReference = "EBR-421" };
        var first = await admin.PostAsJsonAsync($"/api/integrations/{jira.Id}/links", body);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var link = await first.Content.ReadFromJsonAsync<ExternalObjectLinkDto>();
        Assert.Equal("EBR-421", link!.ExternalObjectKey);

        var dup = await admin.PostAsJsonAsync($"/api/integrations/{jira.Id}/links", body);
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
    }

    [Fact] // 21 — a change source reference can be linked and queried by object
    public async Task Change_SourceReference_CanBeLinked()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var jira = await CreateAsync(admin, Jira());
        var changeId = await CreateChangeAsync(admin);

        var resp = await admin.PostAsJsonAsync($"/api/change-requests/{changeId}/external-links",
            new LinkChangeExternalDto { IntegrationId = jira.Id, ExternalReference = "PROJ-77" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var links = await admin.GetFromJsonAsync<List<ExternalObjectLinkDto>>($"/api/integrations/links/object/ChangeRequest/{changeId}");
        Assert.Contains(links!, l => l.ExternalObjectKey == "PROJ-77");
    }

    [Fact] // 22 — incoming Jira webhook with workflow-trigger flag OFF does not start a workflow
    public async Task IncomingWebhook_WorkflowTriggerDisabled_NoAutoStart()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var (integ, secret) = await CreateActiveIncomingAsync(admin, provider: IntegrationProviders.Jira, baseUrl: "https://jira.mock");
        var changeId = await CreateChangeAsync(admin);
        await admin.PostAsJsonAsync($"/api/integrations/{integ.Id}/links",
            new CreateExternalLinkDto { InternalObjectType = "ChangeRequest", InternalObjectId = changeId, ExternalReference = "RDY-5" });

        var body = "{\"eventType\":\"jira:issue_ready_for_review\",\"issueKey\":\"RDY-5\",\"deliveryId\":\"jira-1\"}";
        var resp = await SendWebhookAsync(integ.Code, body, secretHeader: secret);
        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);

        // Flag is off by default → no workflow instance was created for this change.
        var instances = await admin.GetFromJsonAsync<PagedResult<WorkflowInstanceListDto>>(
            $"/api/workflow-instances?triggerObjectId={changeId}");
        Assert.Empty(instances!.Items);
    }

    /* ── notifications / audit / reporting / concurrency ──── */

    [Fact] // 23 — notifications are created on dead-letter (not on success)
    public async Task Notifications_OnlyOnFailure()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var integ = await CreateActiveOutgoingWithSubscriptionAsync(admin, "permanent");
        await FireWorkflowCompletedAsync();
        var execId = (await GetExecutionsAsync(admin, integ.Id, "Pending")).First().Id;
        var detail = await RetryAsync(admin, execId);
        Assert.Equal("DeadLetter", detail.Status);

        var notifications = await admin.GetFromJsonAsync<PagedResult<NotificationListDto>>("/api/notifications?pageSize=200");
        Assert.Contains(notifications!.Items, n => n.Type == "IntegrationDeadLettered" && n.Message.Contains(detail.ExecutionNo));
    }

    [Fact] // 24 — integration events appear in the unified audit under the INTEGRATION module
    public async Task IntegrationEvents_AppearInUnifiedAudit()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var integ = await CreateAsync(admin, GenericRest("http://mock.local/success"));
        var audit = await admin.GetFromJsonAsync<PagedResult<UnifiedAuditRecordDto>>("/api/audit?sourceModule=INTEGRATION&pageSize=200");
        Assert.Contains(audit!.Items, r => r.SourceModule == "INTEGRATION" && r.ObjectNumber == integ.IntegrationNo && r.EventType == "IntegrationCreated");
    }

    [Fact] // 25 — the integration report returns real metrics
    public async Task IntegrationReport_ReturnsMetrics()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        await CreateAsync(admin, GenericRest("http://mock.local/success"));
        var report = await admin.GetFromJsonAsync<IntegrationReportDto>("/api/reports/integrations");
        Assert.NotNull(report);
        Assert.Contains(report!.IntegrationsByProvider, b => b.Key == "GenericRest");
        Assert.True(report.SuccessRate >= 0 && report.SuccessRate <= 100);
    }

    [Fact] // 26 — a stale RowVersion update returns 409
    public async Task StaleRowVersion_Returns409()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var integ = await CreateAsync(admin, GenericRest("http://mock.local/success"));
        var stale = integ.RowVersion;
        (await admin.PutAsJsonAsync($"/api/integrations/{integ.Id}", new UpdateIntegrationDto { Name = "İlk güncelleme" })).EnsureSuccessStatusCode();

        var resp = await admin.PutAsJsonAsync($"/api/integrations/{integ.Id}", new UpdateIntegrationDto { Name = "Bayat", RowVersion = stale });
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact] // 27 — request/response summaries do not leak the webhook secret
    public async Task Summaries_DoNotContainSecrets()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var integ = await CreateActiveOutgoingWithSubscriptionAsync(admin, "success", withWebhookSecret: "TOP-SECRET-SIGKEY");
        await FireWorkflowCompletedAsync();
        var execId = (await GetExecutionsAsync(admin, integ.Id, "Pending")).First().Id;
        var detail = await RetryAsync(admin, execId);
        var serialized = System.Text.Json.JsonSerializer.Serialize(detail);
        Assert.DoesNotContain("TOP-SECRET-SIGKEY", serialized);
    }

    /* ── helpers ──────────────────────────────────────────── */

    private static CreateIntegrationDto GenericRest(string? baseUrl, string auth = "None") => new()
    {
        Code = $"GR_{Guid.NewGuid():N}", Name = "Generic REST", Provider = "GenericRest",
        Category = "Generic", BaseUrl = baseUrl, AuthenticationType = auth
    };

    private static CreateIntegrationDto Jira() => new()
    {
        Code = $"JIRA_{Guid.NewGuid():N}", Name = "Jira Sandbox", Provider = "Jira",
        Category = "WorkManagement", BaseUrl = "https://jira.mock", AuthenticationType = "None"
    };

    private static async Task<IntegrationDetailDto> CreateAsync(HttpClient admin, CreateIntegrationDto dto)
    {
        var resp = await admin.PostAsJsonAsync("/api/integrations", dto);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<IntegrationDetailDto>())!;
    }

    private async Task<(IntegrationDetailDto Integ, string Secret)> CreateActiveIncomingAsync(
        HttpClient admin, string provider = "IncomingWebhook", string? baseUrl = null)
    {
        const string secret = "wh-secret-9f2a";
        var dto = new CreateIntegrationDto
        {
            Code = $"IN_{Guid.NewGuid():N}", Name = "Incoming", Provider = provider,
            Category = provider == "Jira" ? "WorkManagement" : "Generic", BaseUrl = baseUrl, AuthenticationType = "WebhookSecret"
        };
        var integ = await CreateAsync(admin, dto);
        (await admin.PostAsJsonAsync($"/api/integrations/{integ.Id}/credentials",
            new CreateCredentialDto { KeyName = "WebhookSecret", Value = secret })).EnsureSuccessStatusCode();
        (await admin.PostAsync($"/api/integrations/{integ.Id}/activate", null)).EnsureSuccessStatusCode();
        return (integ, secret);
    }

    private async Task<IntegrationDetailDto> CreateActiveOutgoingWithSubscriptionAsync(
        HttpClient admin, string baseUrlSuffix, string? withWebhookSecret = null)
    {
        var dto = new CreateIntegrationDto
        {
            Code = $"OUT_{Guid.NewGuid():N}", Name = "Outgoing", Provider = "OutgoingWebhook",
            Category = "Generic", BaseUrl = $"http://mock.local/{baseUrlSuffix}",
            AuthenticationType = withWebhookSecret is null ? "None" : "WebhookSecret"
        };
        var integ = await CreateAsync(admin, dto);
        if (withWebhookSecret is not null)
            (await admin.PostAsJsonAsync($"/api/integrations/{integ.Id}/credentials",
                new CreateCredentialDto { KeyName = "WebhookSecret", Value = withWebhookSecret })).EnsureSuccessStatusCode();

        var epResp = await admin.PostAsJsonAsync($"/api/integrations/{integ.Id}/endpoints",
            new CreateEndpointDto { Name = "deliver", Direction = "Outgoing", RelativePath = "", HttpMethod = "POST" });
        var endpoint = await epResp.Content.ReadFromJsonAsync<IntegrationEndpointDto>();
        (await admin.PostAsync($"/api/integrations/{integ.Id}/activate", null)).EnsureSuccessStatusCode();
        (await admin.PostAsJsonAsync($"/api/integrations/{integ.Id}/subscriptions",
            new CreateSubscriptionDto { EventType = "WorkflowCompleted", TargetEndpointId = endpoint!.Id })).EnsureSuccessStatusCode();
        return integ;
    }

    private async Task<List<IntegrationExecutionListDto>> GetExecutionsAsync(HttpClient admin, Guid integrationId, string status)
    {
        var page = await admin.GetFromJsonAsync<PagedResult<IntegrationExecutionListDto>>(
            $"/api/integration-executions?integrationId={integrationId}&status={status}&pageSize=50");
        return page!.Items.ToList();
    }

    private static async Task<IntegrationExecutionDetailDto> RetryAsync(HttpClient admin, Guid execId)
    {
        var resp = await admin.PostAsync($"/api/integration-executions/{execId}/retry", null);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<IntegrationExecutionDetailDto>())!;
    }

    private async Task<HttpResponseMessage> SendWebhookAsync(string code, string body,
        string? secretHeader = null, string? signatureHeader = null, string? deliveryId = null)
    {
        var client = _factory.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/integrations/webhooks/{code}")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        if (secretHeader is not null) req.Headers.Add("X-Webhook-Secret", secretHeader);
        if (signatureHeader is not null) req.Headers.Add("X-Gms-Signature", signatureHeader);
        if (deliveryId is not null) req.Headers.Add("X-Delivery-Id", deliveryId);
        return await client.SendAsync(req);
    }

    private async Task<Guid> CreateChangeAsync(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync("/api/change-requests", new CreateChangeRequestDto
        {
            Title = "Integ Test", BusinessReason = "test", CustomerId = Seed.CustomerId, ProjectId = Seed.ProjectId,
            EnvironmentId = Seed.EnvironmentId, ChangeClass = "Standard", ChangeType = "ConfigurationChange", Priority = "Low",
            Revision = new CreateChangeRevisionDto { TechnicalSummary = "s", EstimatedDurationMinutes = 10, RollbackScript = "RB" }
        });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ChangeRequestDetailDto>())!.Id;
    }

    /// <summary>Drives a Standard change through its workflow to completion (fires WorkflowCompleted).</summary>
    private async Task FireWorkflowCompletedAsync()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var changeId = await CreateChangeAsync(requester);
        (await requester.PostAsync($"/api/change-requests/{changeId}/submit", null)).EnsureSuccessStatusCode();

        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var list = await admin.GetFromJsonAsync<PagedResult<WorkflowInstanceListDto>>(
            $"/api/workflow-instances?triggerObjectId={changeId}");
        var instanceId = list!.Items.Single().Id;

        var architect = await _factory.CreateAuthedClientAsync(Seed.Architect);
        (await architect.PostAsJsonAsync($"/api/workflow-instances/{instanceId}/tasks/complete",
            new WorkflowTaskActionDto { Comment = "ok" })).EnsureSuccessStatusCode();
    }
}
