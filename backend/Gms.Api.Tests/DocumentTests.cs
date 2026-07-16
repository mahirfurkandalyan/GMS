using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gms.Api.Tests;

[Collection("gms")]
public sealed class DocumentTests
{
    private readonly GmsWebApplicationFactory _factory;
    public DocumentTests(GmsWebApplicationFactory factory) => _factory = factory;

    [Fact] // Upload creates version 1 and activates the document
    public async Task Upload_CreatesFirstVersion_AndActivates()
    {
        var client = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var doc = await CreateAndUploadAsync(client, Encoding.UTF8.GetBytes("SELECT 1;"), "script.sql");

        Assert.Equal("Active", doc.Status);
        Assert.Single(doc.Versions);
        Assert.Equal(1, doc.Versions[0].VersionNumber);
        Assert.Equal("script.sql", doc.Versions[0].OriginalFileName);
    }

    [Fact] // New version increments the number and keeps the old one
    public async Task NewVersion_IncrementsVersionNumber()
    {
        var client = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var doc = await CreateAndUploadAsync(client, Encoding.UTF8.GetBytes("v1"), "script.sql");

        var v2 = await UploadAsync(client, $"/api/documents/{doc.Id}/new-version", Encoding.UTF8.GetBytes("v2 content"), "script.sql");
        Assert.Equal(HttpStatusCode.OK, v2.StatusCode);
        var updated = await v2.Content.ReadFromJsonAsync<DocumentDetailDto>();
        Assert.Equal(2, updated!.Versions.Count);
        Assert.Equal(2, updated.CurrentVersionNumber);
    }

    [Fact] // Download latest returns bytes whose hash matches the stored version hash
    public async Task Download_Latest_ContentMatchesStoredHash()
    {
        var client = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var bytes = Encoding.UTF8.GetBytes("-- rollback script\nROLLBACK;");
        var doc = await CreateAndUploadAsync(client, bytes, "rollback.sql");

        var resp = await client.GetAsync($"/api/documents/{doc.Id}/download");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var got = await resp.Content.ReadAsByteArrayAsync();
        Assert.Equal(bytes, got);

        var expectedHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        Assert.Equal(expectedHash, doc.Versions[0].Sha256Hash);
    }

    [Fact] // A specific (old) version stays downloadable
    public async Task Download_SpecificVersion_ReturnsThatVersion()
    {
        var client = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var v1Bytes = Encoding.UTF8.GetBytes("first");
        var doc = await CreateAndUploadAsync(client, v1Bytes, "a.sql");
        var v1Id = doc.Versions[0].Id;
        await UploadAsync(client, $"/api/documents/{doc.Id}/new-version", Encoding.UTF8.GetBytes("second"), "a.sql");

        var resp = await client.GetAsync($"/api/documents/{doc.Id}/download?versionId={v1Id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal(v1Bytes, await resp.Content.ReadAsByteArrayAsync());
    }

    [Fact] // Auditor may read/download but NOT create (403)
    public async Task Auditor_CannotCreateDocument_403()
    {
        var client = await _factory.CreateAuthedClientAsync(Seed.Auditor);
        var resp = await client.PostAsJsonAsync("/api/documents", new CreateDocumentDto { Title = "x", Category = "SQL Script" });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact] // Dangerous extension is rejected
    public async Task Upload_DangerousExtension_Returns400()
    {
        var client = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var create = await client.PostAsJsonAsync("/api/documents", new CreateDocumentDto { Title = "Bad", Category = "Other" });
        var doc = await create.Content.ReadFromJsonAsync<DocumentDetailDto>();

        var resp = await UploadAsync(client, $"/api/documents/{doc!.Id}/upload", Encoding.UTF8.GetBytes("MZ..."), "malware.exe");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact] // Soft delete: status Deleted, and download afterwards → 404
    public async Task SoftDelete_ThenDownload_Returns404()
    {
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var doc = await CreateAndUploadAsync(admin, Encoding.UTF8.GetBytes("data"), "c.sql");

        var del = await admin.DeleteAsync($"/api/documents/{doc.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var download = await admin.GetAsync($"/api/documents/{doc.Id}/download");
        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
    }

    [Fact] // Linking: attach to a Change, then find it by object filter
    public async Task Attach_LinksDocument_AndFiltersByObject()
    {
        var client = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var doc = await CreateAndUploadAsync(client, Encoding.UTF8.GetBytes("evidence"), "e.sql");
        var changeId = Guid.NewGuid();

        var attach = await client.PostAsJsonAsync($"/api/documents/{doc.Id}/attach",
            new AttachDocumentDto { ObjectType = "Change", ObjectId = changeId });
        Assert.Equal(HttpStatusCode.NoContent, attach.StatusCode);

        var links = await client.GetFromJsonAsync<List<DocumentLinkDto>>($"/api/documents/{doc.Id}/links");
        Assert.Contains(links!, l => l.ObjectType == "Change" && l.ObjectId == changeId);

        var filtered = await client.GetFromJsonAsync<PagedResult<DocumentListDto>>(
            $"/api/documents?objectType=Change&objectId={changeId}");
        Assert.Contains(filtered!.Items, d => d.Id == doc.Id);
    }

    [Fact] // Integrity: a tampered stored file fails the SHA-256 check on download → 500
    public async Task Download_TamperedFile_FailsIntegrity_500()
    {
        var client = await _factory.CreateAuthedClientAsync(Seed.Requester);
        var doc = await CreateAndUploadAsync(client, Encoding.UTF8.GetBytes("original"), "d.sql");

        // Tamper the physical file behind the version.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GmsDbContext>();
            var version = await db.DocumentVersions.AsNoTracking().FirstAsync(v => v.DocumentId == doc.Id);
            var physical = Path.Combine(_factory.StorageRoot, version.StoragePath.Replace('/', Path.DirectorySeparatorChar));
            await File.WriteAllTextAsync(physical, "TAMPERED CONTENT");
        }

        var resp = await client.GetAsync($"/api/documents/{doc.Id}/download");
        Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);

        // The failed integrity check must be audited.
        var admin = await _factory.CreateAuthedClientAsync(Seed.Admin);
        var audit = await admin.GetFromJsonAsync<List<DocumentAuditEventDto>>($"/api/documents/{doc.Id}/audit");
        Assert.Contains(audit!, e => e.EventType == "IntegrityCheckFailed");
    }

    /* ── helpers ── */

    private static async Task<DocumentDetailDto> CreateAndUploadAsync(HttpClient client, byte[] bytes, string fileName, string category = "SQL Script")
    {
        var create = await client.PostAsJsonAsync("/api/documents", new CreateDocumentDto { Title = "Test Doc", Category = category });
        create.EnsureSuccessStatusCode();
        var doc = (await create.Content.ReadFromJsonAsync<DocumentDetailDto>())!;

        var up = await UploadAsync(client, $"/api/documents/{doc.Id}/upload", bytes, fileName);
        up.EnsureSuccessStatusCode();
        return (await up.Content.ReadFromJsonAsync<DocumentDetailDto>())!;
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string url, byte[] bytes, string fileName)
    {
        var form = new MultipartFormDataContent();
        var fc = new ByteArrayContent(bytes);
        fc.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(fc, "file", fileName);
        form.Add(new StringContent("test comment"), "comment");
        return await client.PostAsync(url, form);
    }
}
