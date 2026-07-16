using System.Net;
using System.Net.Http.Json;
using Gms.Api.Contracts;
using Xunit;

namespace Gms.Api.Tests;

/// <summary>
/// Workflow Engine integration tests against a REAL SQL Server. Covers seeded definitions,
/// the Change integration (Strategy A), the runtime engine (auto-processing, conditions,
/// approval/reject, cancel/pause/resume), RBAC, definition lifecycle (validate/publish/
/// activate/clone/archive) and the unified audit WORKFLOW mapping.
/// </summary>
[Collection("gms")]
public sealed class WorkflowTests
{
    private readonly GmsWebApplicationFactory _factory;
    public WorkflowTests(GmsWebApplicationFactory factory) => _factory = factory;

    /* ── seeded definitions ───────────────────────────────── */

    [Fact] // 1 — the three default change workflows are seeded, Active, with an active version
    public async Task Seeded_Workflows_AreActive()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var list = await admin.GetFromJsonAsync<PagedResult<WorkflowDefinitionListDto>>("/api/workflows?pageSize=100");
        foreach (var code in new[] { "CHANGE_STANDARD_DEFAULT", "CHANGE_NORMAL_DEFAULT", "CHANGE_EMERGENCY_DEFAULT" })
        {
            var def = list!.Items.SingleOrDefault(d => d.Code == code);
            Assert.NotNull(def);
            Assert.Equal("Active", def!.Status);
            Assert.NotNull(def.ActiveVersionId);
            Assert.True(def.IsSystem);
        }
    }

    [Fact] // 2 — the Normal workflow's published v1 has the expected 6-step / 7-transition graph
    public async Task NormalWorkflow_HasExpectedGraph()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var def = await GetDefinitionByCodeAsync(admin, "CHANGE_NORMAL_DEFAULT");
        var v1 = def.Versions.Single(v => v.VersionNumber == 1);
        Assert.Equal("Published", v1.Status);
        Assert.Equal("START", v1.StartStepKey);
        Assert.Equal(6, v1.Steps.Count);
        Assert.Equal(7, v1.Transitions.Count);
        Assert.Contains(v1.Steps, s => s.StepKey == "RISK" && s.StepType == "Condition");
        Assert.Contains(v1.Steps, s => s.StepKey == "RM" && s.AssignedRole == "ReleaseManager");
    }

    /* ── Change integration (Strategy A) ──────────────────── */

    [Fact] // 3 — submitting a change starts a workflow, moves change to UnderReview, activates Architect task
    public async Task ChangeSubmit_StartsWorkflow_ChangeUnderReview()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var change = await CreateChangeAsync(requester, "Normal", "ConfigurationChange", "Medium");
        var submitted = await SubmitAsync(requester, change.Id);
        Assert.Equal("UnderReview", submitted.Status);

        var instance = await GetInstanceForChangeAsync(change.Id);
        Assert.Equal("Waiting", instance.Status);
        var active = instance.Steps.Single(s => s.Status == "Active");
        Assert.Equal("Mimari Onayı", active.Name);
        Assert.Equal("Approval", active.StepType);
    }

    [Fact] // 4 — Standard change: single Architect approval completes the workflow and approves the change
    public async Task StandardWorkflow_Approval_CompletesAndApprovesChange()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var change = await CreateChangeAsync(requester, "Standard", "ConfigurationChange", "Low");
        await SubmitAsync(requester, change.Id);
        var instance = await GetInstanceForChangeAsync(change.Id);

        var architect = await _factory.CreateAuthedClientAsync(Seed.Architect);
        var done = await CompleteTaskAsync(architect, instance.Id, "mimari onay");
        Assert.Equal("Completed", done.Status);

        var reloaded = await GetChangeAsync(requester, change.Id);
        Assert.Equal("Approved", reloaded.Status);
    }

    [Fact] // 5 — Normal + Medium risk: after QA the RISK condition routes to END (no Release Manager)
    public async Task NormalWorkflow_MediumRisk_SkipsReleaseManager()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var change = await CreateChangeAsync(requester, "Normal", "ConfigurationChange", "Medium");
        Assert.Equal("Medium", change.RiskLevel);
        await SubmitAsync(requester, change.Id);
        var instance = await GetInstanceForChangeAsync(change.Id);

        var architect = await _factory.CreateAuthedClientAsync(Seed.Architect);
        await CompleteTaskAsync(architect, instance.Id, "mimari onay");

        var qa = await _factory.CreateAuthedClientAsync(Seed.QA);
        var afterQa = await CompleteTaskAsync(qa, instance.Id, "kalite onay");

        Assert.Equal("Completed", afterQa.Status);
        Assert.DoesNotContain(afterQa.Steps, s => s.StepKey == "RM"); // RM never created
        Assert.Contains(afterQa.Steps, s => s.StepKey == "RISK" && s.Status == "Completed");
        Assert.Equal("Approved", (await GetChangeAsync(requester, change.Id)).Status);
    }

    [Fact] // 6 — Normal + Critical risk: the RISK condition routes to a Release Manager approval
    public async Task NormalWorkflow_CriticalRisk_RoutesToReleaseManager()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var change = await CreateChangeAsync(requester, "Normal", "DatabaseSchemaChange", "High",
            withCriticalAsset: true);
        Assert.Equal("Critical", change.RiskLevel);
        await SubmitAsync(requester, change.Id);
        var instance = await GetInstanceForChangeAsync(change.Id);

        var architect = await _factory.CreateAuthedClientAsync(Seed.Architect);
        await CompleteTaskAsync(architect, instance.Id, "mimari onay");
        var qa = await _factory.CreateAuthedClientAsync(Seed.QA);
        var afterQa = await CompleteTaskAsync(qa, instance.Id, "kalite onay");

        // Condition routed to RM → instance still waiting on the Release Manager step.
        Assert.Equal("Waiting", afterQa.Status);
        Assert.Equal("Yayın Yöneticisi Onayı", afterQa.Steps.Single(s => s.Status == "Active").Name);

        var rm = await _factory.CreateAuthedClientAsync(Seed.ReleaseManager);
        var afterRm = await CompleteTaskAsync(rm, instance.Id, "yayın onay");
        Assert.Equal("Completed", afterRm.Status);
        Assert.Equal("Approved", (await GetChangeAsync(requester, change.Id)).Status);
    }

    [Fact] // 7 — Emergency change: Architect → Release Manager → Admin approvals in sequence
    public async Task EmergencyWorkflow_ThreeApprovals_Chain()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var change = await CreateChangeAsync(requester, "Emergency", "ConfigurationChange", "Critical");
        await SubmitAsync(requester, change.Id);
        var instance = await GetInstanceForChangeAsync(change.Id);

        var architect = await _factory.CreateAuthedClientAsync(Seed.Architect);
        var s1 = await CompleteTaskAsync(architect, instance.Id, "mimari");
        Assert.Equal("Yayın Yöneticisi Onayı", s1.Steps.Single(s => s.Status == "Active").Name);

        var rm = await _factory.CreateAuthedClientAsync(Seed.ReleaseManager);
        var s2 = await CompleteTaskAsync(rm, instance.Id, "yayın");
        Assert.Equal("Admin Onayı", s2.Steps.Single(s => s.Status == "Active").Name);

        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var s3 = await CompleteTaskAsync(admin, instance.Id, "admin");
        Assert.Equal("Completed", s3.Status);
        Assert.Equal("Approved", (await GetChangeAsync(requester, change.Id)).Status);
    }

    /* ── rejection ────────────────────────────────────────── */

    [Fact] // 8 — rejecting an approval ends the instance (Rejected) and sends the change back to Submitted
    public async Task RejectTask_EndsInstance_SendsChangeBack()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var change = await CreateChangeAsync(requester, "Standard", "ConfigurationChange", "Low");
        await SubmitAsync(requester, change.Id);
        var instance = await GetInstanceForChangeAsync(change.Id);

        var architect = await _factory.CreateAuthedClientAsync(Seed.Architect);
        var resp = await architect.PostAsJsonAsync($"/api/workflow-instances/{instance.Id}/tasks/reject",
            new WorkflowTaskActionDto { Comment = "yetersiz" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var rejected = await resp.Content.ReadFromJsonAsync<WorkflowInstanceDetailDto>();
        Assert.Equal("Rejected", rejected!.Status);

        Assert.Equal("Submitted", (await GetChangeAsync(requester, change.Id)).Status);
    }

    [Fact] // 9 — rejection without a comment is rejected (400)
    public async Task RejectTask_WithoutComment_Returns400()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var change = await CreateChangeAsync(requester, "Standard", "ConfigurationChange", "Low");
        await SubmitAsync(requester, change.Id);
        var instance = await GetInstanceForChangeAsync(change.Id);

        var architect = await _factory.CreateAuthedClientAsync(Seed.Architect);
        var resp = await architect.PostAsJsonAsync($"/api/workflow-instances/{instance.Id}/tasks/reject",
            new WorkflowTaskActionDto { Comment = "" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    /* ── RBAC on tasks ────────────────────────────────────── */

    [Fact] // 10 — a user without workflow.task.complete cannot complete a task (403 by policy)
    public async Task Requester_CompletingTask_Returns403()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var change = await CreateChangeAsync(requester, "Standard", "ConfigurationChange", "Low");
        await SubmitAsync(requester, change.Id);
        var instance = await GetInstanceForChangeAsync(change.Id);

        var resp = await requester.PostAsJsonAsync($"/api/workflow-instances/{instance.Id}/tasks/complete",
            new WorkflowTaskActionDto { Comment = "x" });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact] // 11 — Admin holds workflow.admin.override and can action a task assigned to another role
    public async Task Admin_OverrideCompletesArchitectTask()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var change = await CreateChangeAsync(requester, "Standard", "ConfigurationChange", "Low");
        await SubmitAsync(requester, change.Id);
        var instance = await GetInstanceForChangeAsync(change.Id);

        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var done = await CompleteTaskAsync(admin, instance.Id, "override onay");
        Assert.Equal("Completed", done.Status);
    }

    [Fact] // 12 — the assignee sees the task under /tasks/mine
    public async Task MyTasks_ShowsAssignedTask()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var change = await CreateChangeAsync(requester, "Standard", "ConfigurationChange", "Low");
        await SubmitAsync(requester, change.Id);
        var instance = await GetInstanceForChangeAsync(change.Id);

        var architect = await _factory.CreateAuthedClientAsync(Seed.Architect);
        var tasks = await architect.GetFromJsonAsync<List<WorkflowTaskDto>>("/api/workflow-instances/tasks/mine");
        Assert.Contains(tasks!, t => t.InstanceId == instance.Id && t.StepName == "Mimari Onayı");
    }

    /* ── lifecycle: cancel / pause / resume ───────────────── */

    [Fact] // 13 — cancelling a running instance sends the change back to Submitted
    public async Task Cancel_Instance_SendsChangeBack()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var change = await CreateChangeAsync(requester, "Standard", "ConfigurationChange", "Low");
        await SubmitAsync(requester, change.Id);
        var instance = await GetInstanceForChangeAsync(change.Id);

        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var resp = await admin.PostAsJsonAsync($"/api/workflow-instances/{instance.Id}/cancel",
            new WorkflowCancelDto { Reason = "artık gerekmiyor" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("Cancelled", (await resp.Content.ReadFromJsonAsync<WorkflowInstanceDetailDto>())!.Status);
        Assert.Equal("Submitted", (await GetChangeAsync(requester, change.Id)).Status);
    }

    [Fact] // 14 — pause then resume keeps the active step and returns to Waiting
    public async Task Pause_Then_Resume_Instance()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var change = await CreateChangeAsync(requester, "Standard", "ConfigurationChange", "Low");
        await SubmitAsync(requester, change.Id);
        var instance = await GetInstanceForChangeAsync(change.Id);

        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var paused = await admin.PostAsync($"/api/workflow-instances/{instance.Id}/pause", null);
        Assert.Equal(HttpStatusCode.OK, paused.StatusCode);
        Assert.Equal("Running", (await paused.Content.ReadFromJsonAsync<WorkflowInstanceDetailDto>())!.Status);

        var resumed = await admin.PostAsync($"/api/workflow-instances/{instance.Id}/resume", null);
        Assert.Equal(HttpStatusCode.OK, resumed.StatusCode);
        Assert.Equal("Waiting", (await resumed.Content.ReadFromJsonAsync<WorkflowInstanceDetailDto>())!.Status);
    }

    [Fact] // 15 — cancelling the change also cancels its running workflow instance
    public async Task CancelChange_CancelsWorkflow()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var change = await CreateChangeAsync(requester, "Standard", "ConfigurationChange", "Low");
        await SubmitAsync(requester, change.Id);
        var instance = await GetInstanceForChangeAsync(change.Id);

        (await requester.PostAsync($"/api/change-requests/{change.Id}/cancel", null)).EnsureSuccessStatusCode();

        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var reloaded = await admin.GetFromJsonAsync<WorkflowInstanceDetailDto>($"/api/workflow-instances/{instance.Id}");
        Assert.Equal("Cancelled", reloaded!.Status);
    }

    /* ── definition lifecycle ─────────────────────────────── */

    [Fact] // 16 — an invalid draft graph fails validation (no End / unassigned manual / dead-end)
    public async Task Validate_InvalidGraph_ReturnsErrors()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var created = await CreateDefinitionAsync(admin, InvalidGraph());
        var versionId = created.Versions.Single().Id;

        var validateResp = await admin.PostAsync($"/api/workflows/versions/{versionId}/validate", null);
        Assert.Equal(HttpStatusCode.OK, validateResp.StatusCode);
        var result = await validateResp.Content.ReadFromJsonAsync<WorkflowValidationResultDto>();
        Assert.False(result!.IsValid);
        Assert.NotEmpty(result.Errors);

        var publish = await admin.PostAsync($"/api/workflows/versions/{versionId}/publish", null);
        Assert.Equal(HttpStatusCode.BadRequest, publish.StatusCode);
    }

    [Fact] // 17 — a valid draft can be published, and a published version is immutable
    public async Task Publish_ValidGraph_ThenImmutable()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var created = await CreateDefinitionAsync(admin, ValidGraph());
        var versionId = created.Versions.Single().Id;

        var publish = await admin.PostAsync($"/api/workflows/versions/{versionId}/publish", null);
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);
        Assert.Equal("Published", (await publish.Content.ReadFromJsonAsync<WorkflowVersionDto>())!.Status);

        // Editing a published version is rejected.
        var edit = await admin.PutAsJsonAsync($"/api/workflows/versions/{versionId}",
            new UpdateWorkflowVersionDto { Steps = ValidGraph().Steps, Transitions = ValidGraph().Transitions });
        Assert.Equal(HttpStatusCode.BadRequest, edit.StatusCode);
    }

    [Fact] // 18 — publish → activate makes the version the definition's active version
    public async Task Activate_PublishedVersion_SetsActive()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var created = await CreateDefinitionAsync(admin, ValidGraph());
        var versionId = created.Versions.Single().Id;
        (await admin.PostAsync($"/api/workflows/versions/{versionId}/publish", null)).EnsureSuccessStatusCode();

        var resp = await admin.PostAsync($"/api/workflows/{created.Id}/versions/{versionId}/activate", null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var def = await resp.Content.ReadFromJsonAsync<WorkflowDefinitionDetailDto>();
        Assert.Equal(versionId, def!.ActiveVersionId);
        Assert.Equal("Active", def.Status);
    }

    [Fact] // 19 — cloning the latest version yields a new editable Draft (version 2)
    public async Task Clone_CreatesNewDraftVersion()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var created = await CreateDefinitionAsync(admin, ValidGraph());
        var versionId = created.Versions.Single().Id;
        (await admin.PostAsync($"/api/workflows/versions/{versionId}/publish", null)).EnsureSuccessStatusCode();

        var resp = await admin.PostAsync($"/api/workflows/{created.Id}/clone", null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var clone = await resp.Content.ReadFromJsonAsync<WorkflowVersionDto>();
        Assert.Equal(2, clone!.VersionNumber);
        Assert.Equal("Draft", clone.Status);
    }

    [Fact] // 20 — system (seeded) definitions cannot be archived
    public async Task Archive_SystemDefinition_Returns400()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var def = await GetDefinitionByCodeAsync(admin, "CHANGE_STANDARD_DEFAULT");
        var resp = await admin.PostAsync($"/api/workflows/{def.Id}/archive", null);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact] // 21 — a custom definition can be archived
    public async Task Archive_CustomDefinition_Succeeds()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var created = await CreateDefinitionAsync(admin, ValidGraph());
        var resp = await admin.PostAsync($"/api/workflows/{created.Id}/archive", null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("Archived", (await resp.Content.ReadFromJsonAsync<WorkflowDefinitionDetailDto>())!.Status);
    }

    /* ── RBAC on definitions ──────────────────────────────── */

    [Fact] // 22 — a Requester cannot create a workflow definition (403)
    public async Task Requester_CreatingDefinition_Returns403()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var resp = await requester.PostAsJsonAsync("/api/workflows", ValidGraph());
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact] // 23 — an Auditor may read workflow definitions (workflow.definition.read)
    public async Task Auditor_CanReadDefinitions()
    {
        var auditor = await _factory.CreateAuthedClientAsync(Seed.Auditor);
        var resp = await auditor.GetAsync("/api/workflows?pageSize=10");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    /* ── manual start + unified audit ─────────────────────── */

    [Fact] // 24 — a Submitted change with no active instance can be started manually (workflow.instance.start)
    public async Task StartForChange_ManualStart_Succeeds()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var change = await CreateChangeAsync(requester, "Standard", "ConfigurationChange", "Low");
        await SubmitAsync(requester, change.Id);
        // Cancel the auto-started workflow so the change returns to Submitted with no active instance.
        var instance = await GetInstanceForChangeAsync(change.Id);
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        (await admin.PostAsJsonAsync($"/api/workflow-instances/{instance.Id}/cancel",
            new WorkflowCancelDto { Reason = "yeniden başlat" })).EnsureSuccessStatusCode();

        var resp = await admin.PostAsync($"/api/workflow-instances/changes/{change.Id}/start", null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("Waiting", (await resp.Content.ReadFromJsonAsync<WorkflowInstanceDetailDto>())!.Status);
    }

    [Fact] // 25 — workflow events surface in the unified audit read model under the WORKFLOW module
    public async Task WorkflowEvents_AppearInUnifiedAudit()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var change = await CreateChangeAsync(requester, "Standard", "ConfigurationChange", "Low");
        await SubmitAsync(requester, change.Id);
        var instance = await GetInstanceForChangeAsync(change.Id);

        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var audit = await admin.GetFromJsonAsync<PagedResult<UnifiedAuditRecordDto>>(
            "/api/audit?sourceModule=WORKFLOW&pageSize=200");
        Assert.Contains(audit!.Items, r => r.SourceModule == "WORKFLOW" && r.ObjectNumber == instance.InstanceNo);
        Assert.Contains(audit.Items, r => r.EventType == "WorkflowStarted");
    }

    [Fact] // 26 — the safe condition evaluator ignores non-allowlisted fields at publish time
    public async Task Publish_ConditionOnNonAllowlistedField_Fails()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var graph = ValidGraph();
        graph.Transitions[0].ConditionType = "ObjectField";
        graph.Transitions[0].ConditionField = "secretField"; // not allowlisted
        graph.Transitions[0].Operator = "Equals";
        graph.Transitions[0].ExpectedValue = "x";
        var created = await CreateDefinitionAsync(admin, graph);
        var versionId = created.Versions.Single().Id;

        var publish = await admin.PostAsync($"/api/workflows/versions/{versionId}/publish", null);
        Assert.Equal(HttpStatusCode.BadRequest, publish.StatusCode);
    }

    /* ── helpers ──────────────────────────────────────────── */

    private async Task<ChangeRequestDetailDto> CreateChangeAsync(HttpClient client, string changeClass,
        string changeType, string priority, bool withCriticalAsset = false)
    {
        var dto = new CreateChangeRequestDto
        {
            Title = $"WF Test {Guid.NewGuid():N}", BusinessReason = "otomatik test gerekçesi",
            CustomerId = Seed.CustomerId, ProjectId = Seed.ProjectId, EnvironmentId = Seed.EnvironmentId,
            ChangeClass = changeClass, ChangeType = changeType, Priority = priority,
            PlannedImplementationDate = DateTime.UtcNow.AddDays(7),
            Revision = new CreateChangeRevisionDto { TechnicalSummary = "özet", EstimatedDurationMinutes = 20, RollbackScript = "GERI-ALMA" },
            Documents = new() { new CreateChangeDocumentDto { DocumentType = "TestEvidence", DocumentName = "kanit", Version = "v1", Status = "Active" } }
        };
        if (withCriticalAsset)
            dto.Assets = new() { new CreateChangeAffectedAssetDto { AssetType = "Database", AssetName = "Üretim DB", Criticality = "Critical", Description = "kritik" } };

        var resp = await client.PostAsJsonAsync("/api/change-requests", dto);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ChangeRequestDetailDto>())!;
    }

    private async Task<ChangeRequestDetailDto> SubmitAsync(HttpClient client, Guid changeId)
    {
        var resp = await client.PostAsync($"/api/change-requests/{changeId}/submit", null);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ChangeRequestDetailDto>())!;
    }

    private Task<ChangeRequestDetailDto> GetChangeAsync(HttpClient client, Guid changeId) =>
        client.GetFromJsonAsync<ChangeRequestDetailDto>($"/api/change-requests/{changeId}")!;

    private async Task<WorkflowInstanceDetailDto> GetInstanceForChangeAsync(Guid changeId)
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var list = await admin.GetFromJsonAsync<PagedResult<WorkflowInstanceListDto>>(
            $"/api/workflow-instances?triggerObjectId={changeId}&pageSize=10");
        var id = list!.Items.OrderByDescending(i => i.CreatedAt).First().Id;
        return (await admin.GetFromJsonAsync<WorkflowInstanceDetailDto>($"/api/workflow-instances/{id}"))!;
    }

    private static async Task<WorkflowInstanceDetailDto> CompleteTaskAsync(HttpClient client, Guid instanceId, string comment)
    {
        var resp = await client.PostAsJsonAsync($"/api/workflow-instances/{instanceId}/tasks/complete",
            new WorkflowTaskActionDto { Comment = comment });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<WorkflowInstanceDetailDto>())!;
    }

    private static async Task<WorkflowDefinitionDetailDto> GetDefinitionByCodeAsync(HttpClient client, string code)
    {
        var list = await client.GetFromJsonAsync<PagedResult<WorkflowDefinitionListDto>>("/api/workflows?pageSize=100");
        var id = list!.Items.Single(d => d.Code == code).Id;
        return (await client.GetFromJsonAsync<WorkflowDefinitionDetailDto>($"/api/workflows/{id}"))!;
    }

    private static async Task<WorkflowDefinitionDetailDto> CreateDefinitionAsync(HttpClient client, CreateWorkflowDefinitionDto dto)
    {
        var resp = await client.PostAsJsonAsync("/api/workflows", dto);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<WorkflowDefinitionDetailDto>())!;
    }

    private static CreateWorkflowDefinitionDto ValidGraph() => new()
    {
        Code = $"CUSTOM_{Guid.NewGuid():N}", Name = "Özel Akış", Description = "test",
        Category = "ChangeManagement", TriggerObjectType = "ChangeRequest", TriggerEvent = "ChangeSubmitted",
        Steps = new()
        {
            new CreateWorkflowStepDto { StepKey = "START", Name = "Başlangıç", StepType = "Start", StepOrder = 1 },
            new CreateWorkflowStepDto { StepKey = "REV", Name = "İnceleme", StepType = "Approval", StepOrder = 2, AssignedRole = "Architect", DueInHours = 24 },
            new CreateWorkflowStepDto { StepKey = "END", Name = "Bitiş", StepType = "End", StepOrder = 3 }
        },
        Transitions = new()
        {
            new CreateWorkflowTransitionDto { FromStepKey = "START", ToStepKey = "REV", ConditionType = "Always", Priority = 1 },
            new CreateWorkflowTransitionDto { FromStepKey = "REV", ToStepKey = "END", ConditionType = "Always", Priority = 1 }
        }
    };

    private static CreateWorkflowDefinitionDto InvalidGraph() => new()
    {
        Code = $"BADWF_{Guid.NewGuid():N}", Name = "Hatalı Akış", Category = "ChangeManagement",
        TriggerObjectType = "ChangeRequest", TriggerEvent = "ChangeSubmitted",
        Steps = new()
        {
            new CreateWorkflowStepDto { StepKey = "START", Name = "Başlangıç", StepType = "Start", StepOrder = 1 },
            new CreateWorkflowStepDto { StepKey = "TASK", Name = "Görev", StepType = "ManualTask", StepOrder = 2 } // unassigned + no End + dead-end
        },
        Transitions = new()
        {
            new CreateWorkflowTransitionDto { FromStepKey = "START", ToStepKey = "TASK", ConditionType = "Always", Priority = 1 }
        }
    };
}
