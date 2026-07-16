using System.Net;
using System.Net.Http.Json;
using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gms.Api.Tests;

/// <summary>
/// Background processing + observability tests against a REAL SQL Server. Workers are driven
/// deterministically via the Admin run-once endpoint (no real polling delays); external HTTP is
/// intercepted; email is a controllable fake. Covers claiming/idempotency, retry/backoff/
/// dead-letter, email delivery, SLA reminders, correlation id, health, operational status and RBAC.
/// </summary>
[Collection("gms")]
public sealed class BackgroundProcessingTests
{
    private readonly GmsWebApplicationFactory _factory;
    public BackgroundProcessingTests(GmsWebApplicationFactory factory) => _factory = factory;

    /* ── integration dispatch worker ──────────────────────── */

    [Fact] // 1 & 2 — a pending execution is claimed & processed once; a second run does not double-process
    public async Task IntegrationWorker_ClaimsOnce_NoDoubleProcess()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var integ = await SetupOutgoingAsync(admin, "success");
        await FireWorkflowAsync();
        var execId = (await PendingExecAsync(admin, integ)).Single();

        await RunWorkerAsync(admin, "IntegrationDispatch");
        var after1 = await GetExecAsync(admin, execId);
        Assert.Equal("Succeeded", after1.Status);
        Assert.Single(after1.Attempts);

        await RunWorkerAsync(admin, "IntegrationDispatch");
        var after2 = await GetExecAsync(admin, execId);
        Assert.Single(after2.Attempts); // terminal → not reprocessed
    }

    [Fact] // 3 — a retry-scheduled execution is processed only after NextAttemptAt
    public async Task IntegrationWorker_RespectsNextAttemptAt()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var integ = await SetupOutgoingAsync(admin, "transient");
        await FireWorkflowAsync();
        var execId = (await PendingExecAsync(admin, integ)).Single();

        await RunWorkerAsync(admin, "IntegrationDispatch"); // attempt 1 → Failed (transient)
        Assert.Single((await GetExecAsync(admin, execId)).Attempts);

        await SetExecNextAttemptAsync(execId, DateTime.UtcNow.AddHours(1)); // not due yet
        await RunWorkerAsync(admin, "IntegrationDispatch");
        Assert.Single((await GetExecAsync(admin, execId)).Attempts); // skipped

        await SetExecNextAttemptAsync(execId, DateTime.UtcNow.AddMinutes(-1)); // now due
        await RunWorkerAsync(admin, "IntegrationDispatch");
        var attemptsAfterDue = (await GetExecAsync(admin, execId)).Attempts.Count;
        Assert.Equal(2, attemptsAfterDue); // processed
    }

    [Fact] // 4 — transient failures dead-letter after the retry limit
    public async Task IntegrationWorker_DeadLettersAfterExhaustion()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var integ = await SetupOutgoingAsync(admin, "transient");
        await FireWorkflowAsync();
        var execId = (await PendingExecAsync(admin, integ)).Single();

        for (var i = 0; i < 3; i++) await RunWorkerAsync(admin, "IntegrationDispatch");
        var final = await GetExecAsync(admin, execId);
        Assert.Equal("DeadLetter", final.Status);
        var attemptCount = final.Attempts.Count;
        Assert.Equal(3, attemptCount);
    }

    /* ── notification delivery worker ─────────────────────── */

    [Fact] // 5 — a pending email delivery is sent by the worker (after commit)
    public async Task NotificationWorker_SendsPendingEmail()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var title = $"Mail-{Guid.NewGuid():N}";
        await BroadcastAsync(admin, title);
        await RunWorkerAsync(admin, "NotificationDelivery");

        var statuses = await DeliveryStatusesAsync(title);
        Assert.Contains("Sent", statuses);
        Assert.DoesNotContain("Pending", statuses);
    }

    [Fact] // 6 — a transient email failure is retried (Failed + NextAttemptAt), not sent
    public async Task NotificationWorker_TransientFailure_Retries()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var title = $"[[FAIL]]-{Guid.NewGuid():N}";
        await BroadcastAsync(admin, title);
        await RunWorkerAsync(admin, "NotificationDelivery");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GmsDbContext>();
        var d = await db.NotificationDeliveries.AsNoTracking()
            .Where(x => x.Notification!.Title == title && x.Channel == "Email").FirstAsync();
        Assert.Equal("Failed", d.Status);
        Assert.Equal(1, d.AttemptCount);
        Assert.NotNull(d.NextAttemptAt);
    }

    [Fact] // 7 — a permanent email failure dead-letters immediately
    public async Task NotificationWorker_PermanentFailure_DeadLetters()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var title = $"[[PERM]]-{Guid.NewGuid():N}";
        await BroadcastAsync(admin, title);
        await RunWorkerAsync(admin, "NotificationDelivery");

        var statuses = await DeliveryStatusesAsync(title);
        Assert.All(statuses, s => Assert.Equal("DeadLetter", s));
    }

    [Fact] // 8 — a successful email is not sent twice across runs (duplicate prevention)
    public async Task NotificationWorker_DoesNotDoubleSend()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var title = $"Once-{Guid.NewGuid():N}";
        await BroadcastAsync(admin, title);

        await RunWorkerAsync(admin, "NotificationDelivery");
        _factory.Email.SendCounts.TryGetValue(title, out var afterFirst);
        Assert.True(afterFirst >= 1);

        await RunWorkerAsync(admin, "NotificationDelivery");
        _factory.Email.SendCounts.TryGetValue(title, out var afterSecond);
        Assert.Equal(afterFirst, afterSecond); // no re-send
    }

    /* ── workflow SLA worker ──────────────────────────────── */

    [Fact] // 9 & 11 — a due-soon reminder is generated once (cooldown prevents repeats)
    public async Task SlaWorker_DueSoon_GeneratedOnce()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var changeId = await StartWorkflowAsync();
        await SetActiveStepDueAsync(changeId, DateTime.UtcNow.AddHours(1)); // within DueSoonHours

        var before = await NotificationCountAsync(Seed.Architect, NotificationTemplates.WorkflowTaskDueSoon);
        await RunWorkerAsync(admin, "WorkflowSla");
        var after1 = await NotificationCountAsync(Seed.Architect, NotificationTemplates.WorkflowTaskDueSoon);
        Assert.Equal(before + 1, after1);

        await RunWorkerAsync(admin, "WorkflowSla"); // cooldown → no repeat
        var after2 = await NotificationCountAsync(Seed.Architect, NotificationTemplates.WorkflowTaskDueSoon);
        Assert.Equal(after1, after2);
    }

    [Fact] // 10 — an overdue reminder is generated once
    public async Task SlaWorker_Overdue_GeneratedOnce()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var changeId = await StartWorkflowAsync();
        await SetActiveStepDueAsync(changeId, DateTime.UtcNow.AddHours(-1)); // overdue

        var before = await NotificationCountAsync(Seed.Architect, NotificationTemplates.WorkflowTaskOverdue);
        await RunWorkerAsync(admin, "WorkflowSla");
        var after1 = await NotificationCountAsync(Seed.Architect, NotificationTemplates.WorkflowTaskOverdue);
        Assert.Equal(before + 1, after1);

        await RunWorkerAsync(admin, "WorkflowSla");
        var after2 = await NotificationCountAsync(Seed.Architect, NotificationTemplates.WorkflowTaskOverdue);
        Assert.Equal(after1, after2);
    }

    /* ── correlation id ───────────────────────────────────── */

    [Fact] // 12 & 14 — a correlation id is generated and returned in the response
    public async Task CorrelationId_Generated_InResponse()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health");
        Assert.True(resp.Headers.TryGetValues("X-Correlation-Id", out var values));
        Assert.False(string.IsNullOrWhiteSpace(values!.First()));
    }

    [Fact] // 13 — a valid incoming correlation id is preserved
    public async Task CorrelationId_Incoming_Preserved()
    {
        var client = _factory.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/health");
        req.Headers.Add("X-Correlation-Id", "corr-test-12345");
        var resp = await client.SendAsync(req);
        Assert.Equal("corr-test-12345", resp.Headers.GetValues("X-Correlation-Id").First());
    }

    /* ── health / operational status / RBAC ───────────────── */

    [Fact] // 15 — liveness is up and readiness reports healthy (DB reachable), no internals leaked
    public async Task Health_Live_And_Ready()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);

        var ready = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        var body = await ready.Content.ReadAsStringAsync();
        Assert.Contains("status", body);
        Assert.DoesNotContain("StackTrace", body);
        Assert.DoesNotContain("Exception", body);
    }

    [Fact] // 16 & 19 — operational status returns real backlog + worker heartbeat after a run
    public async Task OperationalStatus_ReflectsBacklogAndHeartbeat()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var integ = await SetupOutgoingAsync(admin, "success");
        await FireWorkflowAsync(); // creates a pending execution → backlog

        var status1 = await admin.GetFromJsonAsync<OperationalStatusDto>("/api/operations/status");
        Assert.True(status1!.PendingIntegrationExecutions >= 1);

        await RunWorkerAsync(admin, "IntegrationDispatch");
        var status2 = await admin.GetFromJsonAsync<OperationalStatusDto>("/api/operations/status");
        Assert.Contains(status2!.Workers, w => w.WorkerName == "IntegrationDispatch" && w.LastSucceededAt != null);
    }

    [Fact] // 17 — a Requester cannot read operational status (403)
    public async Task Requester_OperationsStatus_Forbidden()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        Assert.Equal(HttpStatusCode.Forbidden, (await requester.GetAsync("/api/operations/status")).StatusCode);
    }

    [Fact] // 18 — an Auditor can read operational status (200) but cannot run workers (403)
    public async Task Auditor_OperationsStatus_Ok_ButCannotRun()
    {
        var auditor = await _factory.CreateAuthedClientAsync(Seed.Auditor);
        Assert.Equal(HttpStatusCode.OK, (await auditor.GetAsync("/api/operations/status")).StatusCode);
        var run = await auditor.PostAsync("/api/operations/workers/IntegrationDispatch/run-once", null);
        Assert.Equal(HttpStatusCode.Forbidden, run.StatusCode);
    }

    [Fact] // 20 — operational status never leaks secrets/credentials
    public async Task OperationalStatus_NoSecretLeak()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var status = await admin.GetAsync("/api/operations/status");
        var body = await status.Content.ReadAsStringAsync();
        Assert.DoesNotContain("EncryptedValue", body);
        Assert.DoesNotContain("Password", body);
    }

    /* ── helpers ──────────────────────────────────────────── */

    private static async Task<WorkerRunResultDto> RunWorkerAsync(HttpClient admin, string worker)
    {
        var resp = await admin.PostAsync($"/api/operations/workers/{worker}/run-once", null);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<WorkerRunResultDto>())!;
    }

    private static async Task<Guid> SetupOutgoingAsync(HttpClient admin, string suffix)
    {
        var dto = new CreateIntegrationDto
        {
            Code = $"BG_{Guid.NewGuid():N}", Name = "BG Out", Provider = "OutgoingWebhook",
            Category = "Generic", BaseUrl = $"http://mock.local/{suffix}", AuthenticationType = "None"
        };
        var create = await admin.PostAsJsonAsync("/api/integrations", dto);
        create.EnsureSuccessStatusCode();
        var integ = (await create.Content.ReadFromJsonAsync<IntegrationDetailDto>())!;
        var epResp = await admin.PostAsJsonAsync($"/api/integrations/{integ.Id}/endpoints",
            new CreateEndpointDto { Name = "deliver", Direction = "Outgoing", RelativePath = "", HttpMethod = "POST" });
        var ep = await epResp.Content.ReadFromJsonAsync<IntegrationEndpointDto>();
        (await admin.PostAsync($"/api/integrations/{integ.Id}/activate", null)).EnsureSuccessStatusCode();
        (await admin.PostAsJsonAsync($"/api/integrations/{integ.Id}/subscriptions",
            new CreateSubscriptionDto { EventType = "WorkflowCompleted", TargetEndpointId = ep!.Id })).EnsureSuccessStatusCode();
        return integ.Id;
    }

    private async Task<List<Guid>> PendingExecAsync(HttpClient admin, Guid integrationId)
    {
        var page = await admin.GetFromJsonAsync<PagedResult<IntegrationExecutionListDto>>(
            $"/api/integration-executions?integrationId={integrationId}&status=Pending&pageSize=50");
        return page!.Items.Select(i => i.Id).ToList();
    }

    private static Task<IntegrationExecutionDetailDto> GetExecAsync(HttpClient admin, Guid id) =>
        admin.GetFromJsonAsync<IntegrationExecutionDetailDto>($"/api/integration-executions/{id}")!;

    private async Task SetExecNextAttemptAsync(Guid execId, DateTime when)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GmsDbContext>();
        var exec = await db.IntegrationExecutions.FirstAsync(x => x.Id == execId);
        exec.NextAttemptAt = when;
        exec.LockedUntil = null; exec.LockedBy = null;
        await db.SaveChangesAsync();
    }

    private async Task FireWorkflowAsync()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var changeId = await CreateChangeAsync(requester);
        (await requester.PostAsync($"/api/change-requests/{changeId}/submit", null)).EnsureSuccessStatusCode();
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var inst = (await admin.GetFromJsonAsync<PagedResult<WorkflowInstanceListDto>>(
            $"/api/workflow-instances?triggerObjectId={changeId}"))!.Items.Single().Id;
        var architect = await _factory.CreateAuthedClientAsync(Seed.Architect);
        (await architect.PostAsJsonAsync($"/api/workflow-instances/{inst}/tasks/complete",
            new WorkflowTaskActionDto { Comment = "ok" })).EnsureSuccessStatusCode();
    }

    /// <summary>Starts a workflow and returns the change id (instance Waiting on the Architect step).</summary>
    private async Task<Guid> StartWorkflowAsync()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var changeId = await CreateChangeAsync(requester);
        (await requester.PostAsync($"/api/change-requests/{changeId}/submit", null)).EnsureSuccessStatusCode();
        return changeId;
    }

    private async Task SetActiveStepDueAsync(Guid changeId, DateTime dueAt)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GmsDbContext>();
        var instanceId = await db.WorkflowInstances.Where(i => i.TriggerObjectId == changeId)
            .OrderByDescending(i => i.CreatedAt).Select(i => i.Id).FirstAsync();
        var step = await db.WorkflowStepInstances.FirstAsync(s => s.WorkflowInstanceId == instanceId
            && s.Status == WorkflowStepStatuses.Active);
        step.DueAt = dueAt;
        step.DueSoonNotifiedAt = null;
        step.OverdueNotifiedAt = null;
        await db.SaveChangesAsync();
    }

    private async Task<int> NotificationCountAsync(string userEmail, string type)
    {
        var client = await _factory.CreateAuthedClientAsync(userEmail);
        var list = await client.GetFromJsonAsync<PagedResult<NotificationListDto>>("/api/notifications?pageSize=200");
        return list!.Items.Count(n => n.Type == type);
    }

    private static async Task BroadcastAsync(HttpClient admin, string title)
    {
        var resp = await admin.PostAsJsonAsync("/api/notifications/broadcast",
            new BroadcastNotificationDto { Title = title, Message = "arka plan testi", Severity = "Information" });
        resp.EnsureSuccessStatusCode();
    }

    private async Task<List<string>> DeliveryStatusesAsync(string notificationTitle)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GmsDbContext>();
        return await db.NotificationDeliveries.AsNoTracking()
            .Where(d => d.Notification!.Title == notificationTitle && d.Channel == "Email")
            .Select(d => d.Status).ToListAsync();
    }

    private async Task<Guid> CreateChangeAsync(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync("/api/change-requests", new CreateChangeRequestDto
        {
            Title = "BG Test", BusinessReason = "test", CustomerId = Seed.CustomerId, ProjectId = Seed.ProjectId,
            EnvironmentId = Seed.EnvironmentId, ChangeClass = "Standard", ChangeType = "ConfigurationChange", Priority = "Low",
            Revision = new CreateChangeRevisionDto { TechnicalSummary = "s", EstimatedDurationMinutes = 10, RollbackScript = "RB" }
        });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ChangeRequestDetailDto>())!.Id;
    }
}
