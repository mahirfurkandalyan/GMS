using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Gms.Api.Services.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gms.Api.Tests;

[Collection("gms")]
public sealed class AuditReportTests
{
    private readonly GmsWebApplicationFactory _factory;
    public AuditReportTests(GmsWebApplicationFactory factory) => _factory = factory;

    [Fact] // 1 — unified audit list spans multiple modules
    public async Task UnifiedAudit_ReturnsRecordsFromMultipleModules()
    {
        // Generate multi-module activity: submit a change (CHANGE + APPROVAL + NOTIFICATION).
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        await CreateAndSubmitChangeAsync(requester);

        var auditor = await _factory.CreateAuthedClientAsync(Seed.Auditor); // login → SECURITY
        var page = await auditor.GetFromJsonAsync<PagedResult<UnifiedAuditRecordDto>>("/api/audit?pageSize=500");
        var modules = page!.Items.Select(r => r.SourceModule).Distinct().ToList();
        Assert.True(modules.Count >= 3, $"beklenen >=3 modül, gelen: {string.Join(",", modules)}");
    }

    [Fact] // 2 — object timeline is chronological and complete
    public async Task ObjectTimeline_ReturnsChronologicalHistory()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var change = await CreateAndSubmitChangeAsync(requester);

        var auditor = await _factory.CreateAuthedClientAsync(Seed.Auditor);
        var timeline = await auditor.GetFromJsonAsync<List<UnifiedAuditRecordDto>>($"/api/audit/object/ChangeRequest/{change.Id}");
        Assert.Contains(timeline!, r => r.EventType == "ChangeCreated");
        Assert.Contains(timeline!, r => r.EventType == "ChangeSubmitted");
        for (var i = 1; i < timeline!.Count; i++)
            Assert.True(timeline[i].CreatedAt >= timeline[i - 1].CreatedAt);
    }

    [Fact] // 3 + 4 — requester has no global audit access
    public async Task Requester_GlobalAudit_Returns403()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        Assert.Equal(HttpStatusCode.Forbidden, (await requester.GetAsync("/api/audit?pageSize=1")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await requester.GetAsync($"/api/audit/user/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact] // 5 — auditor has audit access
    public async Task Auditor_Audit_Returns200()
    {
        var auditor = await _factory.CreateAuthedClientAsync(Seed.Auditor);
        Assert.Equal(HttpStatusCode.OK, (await auditor.GetAsync("/api/audit?pageSize=1")).StatusCode);
    }

    [Fact] // 6 — security audit requires audit.security.read
    public async Task SecurityAudit_RequiresSecurityPermission()
    {
        var rm = await _factory.CreateAuthedClientAsync(Seed.ReleaseManager); // has audit.read, NOT security
        Assert.Equal(HttpStatusCode.Forbidden, (await rm.GetAsync("/api/audit/security")).StatusCode);

        var auditor = await _factory.CreateAuthedClientAsync(Seed.Auditor);
        Assert.Equal(HttpStatusCode.OK, (await auditor.GetAsync("/api/audit/security")).StatusCode);
    }

    [Fact] // 7 — overview metrics match direct DB counts
    public async Task Overview_MetricsMatchDatabase()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var overview = await admin.GetFromJsonAsync<ReportOverviewDto>("/api/reports/overview");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GmsDbContext>();
        Assert.Equal(await db.ChangeRequests.CountAsync(), overview!.TotalChanges);
        Assert.Equal(await db.Documents.CountAsync(d => d.Status != "Deleted"), overview.TotalDocuments);
    }

    [Fact] // 8 — change status distribution is consistent
    public async Task ChangeReport_StatusDistributionConsistent()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var report = await admin.GetFromJsonAsync<ChangeReportDto>("/api/reports/changes");
        var sum = report!.ChangesByStatus.Sum(b => b.Count);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GmsDbContext>();
        Assert.Equal(await db.ChangeRequests.CountAsync(), sum);
    }

    [Fact] // 9 + 10 — execution/validation rates are well-formed (0..100)
    public async Task Execution_And_Validation_RatesAreValid()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var exec = await admin.GetFromJsonAsync<ExecutionReportDto>("/api/reports/executions");
        Assert.InRange(exec!.SuccessRate, 0, 100);
        Assert.InRange(exec.FailureRate, 0, 100);
        Assert.InRange(exec.RollbackRate, 0, 100);

        var val = await admin.GetFromJsonAsync<ValidationReportDto>("/api/reports/validations");
        Assert.InRange(val!.PassRate, 0, 100);
        Assert.InRange(val.FailRate, 0, 100);
    }

    [Fact] // 11 — document integrity failures surface in reporting
    public async Task DocumentReport_IncludesIntegrityFailures()
    {
        // Create + upload + tamper + download to force an integrity failure.
        var client = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var create = await client.PostAsJsonAsync("/api/documents", new CreateDocumentDto { Title = "Integrity", Category = "SQL Script" });
        var doc = await create.Content.ReadFromJsonAsync<DocumentDetailDto>();
        var form = new MultipartFormDataContent { { new ByteArrayContent(Encoding.UTF8.GetBytes("orig")) { Headers = { ContentType = new MediaTypeHeaderValue("text/plain") } }, "file", "x.sql" } };
        await client.PostAsync($"/api/documents/{doc!.Id}/upload", form);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GmsDbContext>();
            var v = await db.DocumentVersions.AsNoTracking().FirstAsync(x => x.DocumentId == doc.Id);
            await File.WriteAllTextAsync(Path.Combine(_factory.StorageRoot, v.StoragePath.Replace('/', Path.DirectorySeparatorChar)), "HACKED");
        }
        await client.GetAsync($"/api/documents/{doc.Id}/download"); // → 500 + IntegrityCheckFailed audit

        var report = await client.GetFromJsonAsync<DocumentReportDto>("/api/reports/documents");
        Assert.True(report!.IntegrityFailures >= 1);
    }

    [Fact] // 12 — filtering by module returns only that module
    public async Task Audit_FilterBySourceModule_Works()
    {
        var auditor = await _factory.CreateAuthedClientAsync(Seed.Auditor);
        var page = await auditor.GetFromJsonAsync<PagedResult<UnifiedAuditRecordDto>>("/api/audit?sourceModule=SECURITY&pageSize=50");
        Assert.All(page!.Items, r => Assert.Equal("SECURITY", r.SourceModule));
    }

    [Fact] // 13 — CSV export requires audit.export
    public async Task AuditExport_RequiresPermission()
    {
        var requester = await _factory.CreateAuthedClientAsync(Seed.Requester);
        Assert.Equal(HttpStatusCode.Forbidden, (await requester.GetAsync("/api/audit/export?format=csv")).StatusCode);

        var auditor = await _factory.CreateAuthedClientAsync(Seed.Auditor);
        var resp = await auditor.GetAsync("/api/audit/export?format=csv");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("text/csv", resp.Content.Headers.ContentType!.MediaType);
    }

    [Fact] // 14 — CSV export neutralises formula injection
    public void CsvExport_PreventsFormulaInjection()
    {
        var svc = new ReportExportService();
        var bytes = svc.ToCsv(new[] { "Metric", "Value" },
            new List<IReadOnlyList<string?>> { new string?[] { "=SUM(A1:A2)", "+1+1" }, new string?[] { "@cmd", "-danger" } });
        var csv = Encoding.UTF8.GetString(bytes);
        Assert.Contains("'=SUM(A1:A2)", csv);
        Assert.Contains("'+1+1", csv);
        Assert.Contains("'@cmd", csv);
        Assert.Contains("'-danger", csv);
    }

    [Fact] // 15 — the unified audit view is read-only (keyless → cannot be tracked/written)
    public void UnifiedAuditView_IsReadOnly()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GmsDbContext>();
        // A keyless view entity cannot even be tracked for insert — write is impossible.
        Assert.ThrowsAny<Exception>(() =>
            db.UnifiedAuditRecords.Add(new UnifiedAuditRecord { RecordId = Guid.NewGuid(), SourceModule = "X", EventType = "X", ObjectType = "X", CreatedAt = DateTime.UtcNow }));
    }

    /* ── helpers ── */

    private async Task<ChangeRequestDetailDto> CreateAndSubmitChangeAsync(HttpClient requester)
    {
        var create = await requester.PostAsJsonAsync("/api/change-requests", new CreateChangeRequestDto
        {
            Title = "Audit Timeline", BusinessReason = "t",
            CustomerId = Seed.CustomerId, ProjectId = Seed.ProjectId, EnvironmentId = Seed.EnvironmentId,
            ChangeClass = "Normal", ChangeType = "ConfigurationChange", Priority = "Medium",
            Revision = new CreateChangeRevisionDto { TechnicalSummary = "s", EstimatedDurationMinutes = 10, RollbackScript = "RB" }
        });
        var change = (await create.Content.ReadFromJsonAsync<ChangeRequestDetailDto>())!;
        await requester.PostAsync($"/api/change-requests/{change.Id}/submit", null);
        return change;
    }
}
