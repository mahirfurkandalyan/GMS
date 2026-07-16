using System.Net;
using System.Net.Http.Json;
using Gms.Api.Contracts;
using Xunit;

namespace Gms.Api.Tests;

[Collection("gms")]
public sealed class NotificationTests
{
    private readonly GmsWebApplicationFactory _factory;
    public NotificationTests(GmsWebApplicationFactory factory) => _factory = factory;

    private sealed record BroadcastResult(int Delivered);

    [Fact] // Broadcast reaches recipients; recipient can read then archive (unread → read → archived)
    public async Task Broadcast_ThenRead_ThenArchive()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var title = $"Duyuru-{Guid.NewGuid():N}";
        var bc = await admin.PostAsJsonAsync("/api/notifications/broadcast",
            new BroadcastNotificationDto { Title = title, Message = "Herkese duyuru", Severity = "Information" });
        Assert.Equal(HttpStatusCode.OK, bc.StatusCode);
        Assert.True((await bc.Content.ReadFromJsonAsync<BroadcastResult>())!.Delivered > 0);

        var qa = await _factory.CreateAuthedClientAsync(Seed.QA);
        var list = await qa.GetFromJsonAsync<PagedResult<NotificationListDto>>("/api/notifications?pageSize=100");
        var mine = list!.Items.First(n => n.Title == title);
        Assert.Equal("Unread", mine.Status);

        Assert.Equal(HttpStatusCode.NoContent, (await qa.PostAsync($"/api/notifications/{mine.Id}/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await qa.PostAsync($"/api/notifications/{mine.Id}/archive", null)).StatusCode);

        var detail = await qa.GetFromJsonAsync<NotificationDetailDto>($"/api/notifications/{mine.Id}");
        Assert.Equal("Archived", detail!.Status);
        Assert.Contains(detail.Deliveries, d => d.Channel == "InApp");
    }

    [Fact] // Workflow integration: submitting a change assigns the first task to the Architect (WORKFLOW notification)
    public async Task ChangeSubmit_NotifiesArchitectWorkflowTask()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var create = await requester.PostAsJsonAsync("/api/change-requests", new CreateChangeRequestDto
        {
            Title = "Bildirim Testi", BusinessReason = "test",
            CustomerId = Seed.CustomerId, ProjectId = Seed.ProjectId, EnvironmentId = Seed.EnvironmentId,
            ChangeClass = "Normal", ChangeType = "ConfigurationChange", Priority = "Medium",
            Revision = new CreateChangeRevisionDto { TechnicalSummary = "s", EstimatedDurationMinutes = 10, RollbackScript = "RB" }
        });
        var change = await create.Content.ReadFromJsonAsync<ChangeRequestDetailDto>();
        await requester.PostAsync($"/api/change-requests/{change!.Id}/submit", null);

        var architect = await _factory.CreateAuthedClientAsync(Seed.Architect);
        var list = await architect.GetFromJsonAsync<PagedResult<NotificationListDto>>("/api/notifications?pageSize=100");
        Assert.Contains(list!.Items, n => n.Type == "WorkflowTaskAssigned" && n.Message.Contains(change.ChangeNo));
    }

    [Fact] // Preference: disabling both channels for a module suppresses that module's notifications
    public async Task Preference_DisablingModule_SuppressesNotification()
    {
        // Executor is used only here (avoids the lockout/password-change users).
        var executor = await _factory.CreateAuthedClientAsync(Seed.Executor);
        var upd = await executor.PutAsJsonAsync("/api/notifications/preferences", new UpdatePreferencesDto
        {
            Preferences = new() { new NotificationPreferenceDto { Module = "SYSTEM", InAppEnabled = false, EmailEnabled = false } }
        });
        Assert.Equal(HttpStatusCode.NoContent, upd.StatusCode);

        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var title = $"Pref-{Guid.NewGuid():N}";
        await admin.PostAsJsonAsync("/api/notifications/broadcast",
            new BroadcastNotificationDto { Title = title, Message = "gizli", Severity = "Information" });

        // Executor opted out of SYSTEM → must NOT receive it.
        var executorList = await executor.GetFromJsonAsync<PagedResult<NotificationListDto>>("/api/notifications?pageSize=200");
        Assert.DoesNotContain(executorList!.Items, n => n.Title == title);

        // A user who did NOT opt out (auditor) DID receive it.
        var auditor = await _factory.CreateAuthedClientAsync(Seed.Auditor);
        var auditorList = await auditor.GetFromJsonAsync<PagedResult<NotificationListDto>>("/api/notifications?pageSize=200");
        Assert.Contains(auditorList!.Items, n => n.Title == title);

        // restore preference for other tests
        await executor.PutAsJsonAsync("/api/notifications/preferences", new UpdatePreferencesDto
        {
            Preferences = new() { new NotificationPreferenceDto { Module = "SYSTEM", InAppEnabled = true, EmailEnabled = true } }
        });
    }

    [Fact] // A user without notification.broadcast cannot broadcast
    public async Task Broadcast_WithoutPermission_Returns403()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var resp = await requester.PostAsJsonAsync("/api/notifications/broadcast",
            new BroadcastNotificationDto { Title = "x", Message = "y", Severity = "Information" });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact] // Templates are admin-only (notification.template.manage)
    public async Task Templates_AdminSeesAll_RequesterForbidden()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var templates = await admin.GetFromJsonAsync<List<NotificationTemplateDto>>("/api/notifications/templates");
        Assert.True(templates!.Count >= 16);

        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var resp = await requester.GetAsync("/api/notifications/templates");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact] // A user cannot read/act on another user's notification
    public async Task MarkRead_OthersNotification_Returns403()
    {
        // Create a notification owned by admin (broadcast reaches admin too).
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var title = $"Own-{Guid.NewGuid():N}";
        await admin.PostAsJsonAsync("/api/notifications/broadcast",
            new BroadcastNotificationDto { Title = title, Message = "z", Severity = "Information" });
        var adminList = await admin.GetFromJsonAsync<PagedResult<NotificationListDto>>("/api/notifications?pageSize=200");
        var adminNotification = adminList!.Items.First(n => n.Title == title);

        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var resp = await requester.PostAsync($"/api/notifications/{adminNotification.Id}/read", null);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }
}
