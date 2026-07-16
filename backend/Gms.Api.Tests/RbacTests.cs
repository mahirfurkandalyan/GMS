using System.Net;
using System.Net.Http.Json;
using Gms.Api.Contracts;
using Xunit;

namespace Gms.Api.Tests;

[Collection("gms")]
public sealed class RbacTests
{
    private readonly GmsWebApplicationFactory _factory;
    public RbacTests(GmsWebApplicationFactory factory) => _factory = factory;

    [Fact] // 7 — Requester lacks release.create → 403
    public async Task Requester_CreatingRelease_Returns403()
    {
        var client = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var resp = await client.PostAsJsonAsync("/api/releases", new CreateReleasePlanDto
        {
            Name = "x", Version = "v1", CustomerId = Seed.CustomerId, ProjectId = Seed.ProjectId,
            EnvironmentId = Seed.EnvironmentId, ReleaseType = "Minor",
            ReleaseManagerUserId = Guid.NewGuid(), ChangeIds = new() { Guid.NewGuid() }
        });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact] // 8 — Architect completes the Architect workflow task → success; QA step becomes active
    public async Task Architect_CompletingArchitectTask_Succeeds()
    {
        var instance = await CreateStartedWorkflowAsync();
        var architect = await _factory.CreateAuthedClientAsync(Seed.Architect);

        var resp = await architect.PostAsJsonAsync($"/api/workflow-instances/{instance.Id}/tasks/complete",
            new WorkflowTaskActionDto { Comment = "mimari ok" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var updated = await resp.Content.ReadFromJsonAsync<WorkflowInstanceDetailDto>();
        Assert.Equal("Waiting", updated!.Status);
        Assert.Equal("Kalite (QA) Onayı", updated.Steps.First(s => s.Status == "Active").Name);
    }

    [Fact] // 9 — QA cannot complete the Architect-assigned workflow task → 403
    public async Task Qa_CompletingArchitectTask_Returns403()
    {
        var instance = await CreateStartedWorkflowAsync();
        var qa = await _factory.CreateAuthedClientAsync(Seed.QA);

        var resp = await qa.PostAsJsonAsync($"/api/workflow-instances/{instance.Id}/tasks/complete",
            new WorkflowTaskActionDto { Comment = "qa deneme" });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact] // 10 — ReleaseManager has release.schedule; Requester does not
    public async Task ReleaseSchedule_PermissionIsEnforced()
    {
        var someRelease = Guid.NewGuid();

        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var forbidden = await requester.PostAsync($"/api/releases/{someRelease}/schedule", null);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode); // blocked by policy

        var rm = await _factory.CreateAuthedClientAsync(Seed.ReleaseManager);
        var passesPolicy = await rm.PostAsync($"/api/releases/{someRelease}/schedule", null);
        // Has the permission → policy passes; fails only because the release doesn't exist.
        Assert.Equal(HttpStatusCode.NotFound, passesPolicy.StatusCode);
    }

    [Fact] // 11 — actor recorded in audit comes from the JWT, not the request body
    public async Task Change_Audit_Actor_ComesFromJwt()
    {
        var login = await _factory.LoginAsync(Seed.Requester);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", login.AccessToken);

        var change = await CreateChangeAsync(client);
        var audit = await client.GetFromJsonAsync<List<ChangeAuditEventDto>>($"/api/change-requests/{change.Id}/audit");
        var created = audit!.First(e => e.EventType == "ChangeCreated");

        Assert.Equal(login.User.Id, created.ActorUserId);
    }

    /* ── helpers ── */

    private async Task<ChangeRequestDetailDto> CreateChangeAsync(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync("/api/change-requests", new CreateChangeRequestDto
        {
            Title = "RBAC Test Değişikliği",
            BusinessReason = "otomatik test",
            CustomerId = Seed.CustomerId, ProjectId = Seed.ProjectId, EnvironmentId = Seed.EnvironmentId,
            ChangeClass = "Normal", ChangeType = "ConfigurationChange", Priority = "Medium",
            Revision = new CreateChangeRevisionDto { TechnicalSummary = "özet", EstimatedDurationMinutes = 30, RollbackScript = "RB" }
        });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ChangeRequestDetailDto>())!;
    }

    private async Task<WorkflowInstanceDetailDto> CreateStartedWorkflowAsync()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var change = await CreateChangeAsync(requester);
        (await requester.PostAsync($"/api/change-requests/{change.Id}/submit", null)).EnsureSuccessStatusCode();

        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var list = await admin.GetFromJsonAsync<PagedResult<WorkflowInstanceListDto>>(
            $"/api/workflow-instances?triggerObjectId={change.Id}");
        var instanceId = list!.Items.Single().Id;
        return (await admin.GetFromJsonAsync<WorkflowInstanceDetailDto>($"/api/workflow-instances/{instanceId}"))!;
    }
}
