using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Gms.Api.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Controllers;

/// <summary>
/// The single, reusable document API for the whole platform. Thin controller — all
/// storage/hashing/versioning/audit logic lives in <see cref="DocumentService"/>.
/// </summary>
[ApiController]
[Route("api/documents")]
[Tags("Documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly GmsDbContext _db;
    private readonly DocumentService _documents;

    public DocumentsController(GmsDbContext db, DocumentService documents)
    {
        _db = db;
        _documents = documents;
    }

    private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();

    /// <summary>Filtrelenebilir + sayfalanabilir doküman listesi (özet). Silinenler varsayılan gizli.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.DocumentRead)]
    public async Task<ActionResult<PagedResult<DocumentListDto>>> GetAll(
        [FromQuery] string? category, [FromQuery] string? status, [FromQuery] Guid? ownerUserId,
        [FromQuery] string? objectType, [FromQuery] Guid? objectId, [FromQuery] bool includeDeleted,
        [FromQuery] string? search, [FromQuery] PagedQuery paging)
    {
        var query = _db.Documents.AsNoTracking().AsQueryable();

        if (!includeDeleted) query = query.Where(d => d.Status != DocumentStatuses.Deleted);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(d => d.Category == category);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(d => d.Status == status);
        if (ownerUserId.HasValue) query = query.Where(d => d.OwnerUserId == ownerUserId.Value);
        if (!string.IsNullOrWhiteSpace(objectType) && objectId.HasValue)
            query = query.Where(d => d.Links.Any(l => l.ObjectType == objectType && l.ObjectId == objectId.Value));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(d => d.DocumentNo.Contains(s) || d.Title.Contains(s));
        }

        var totalCount = await query.CountAsync();

        var ordered = (paging.SortBy?.ToLowerInvariant()) switch
        {
            "documentno" => paging.Descending ? query.OrderByDescending(d => d.DocumentNo) : query.OrderBy(d => d.DocumentNo),
            "title" => paging.Descending ? query.OrderByDescending(d => d.Title) : query.OrderBy(d => d.Title),
            "category" => paging.Descending ? query.OrderByDescending(d => d.Category) : query.OrderBy(d => d.Category),
            "status" => paging.Descending ? query.OrderByDescending(d => d.Status) : query.OrderBy(d => d.Status),
            _ => paging.Descending ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt)
        };

        var items = await ordered.ThenBy(d => d.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(d => new DocumentListDto
            {
                Id = d.Id, DocumentNo = d.DocumentNo, Title = d.Title, Category = d.Category, Status = d.Status,
                OwnerUserName = d.OwnerUser!.FullName,
                VersionCount = d.Versions.Count,
                CurrentVersionNumber = d.Versions.Where(v => v.Id == d.CurrentVersionId).Select(v => v.VersionNumber).FirstOrDefault(),
                CurrentSizeBytes = d.Versions.Where(v => v.Id == d.CurrentVersionId).Select(v => v.SizeBytes).FirstOrDefault(),
                LinkCount = d.Links.Count,
                CreatedAt = d.CreatedAt, UpdatedAt = d.UpdatedAt
            })
            .ToListAsync();

        return Ok(PagedResult<DocumentListDto>.Create(items, paging.Page, paging.PageSize, totalCount));
    }

    /// <summary>Tam doküman detayı: sürümler ve bağlantılar (fiziksel yol içermez).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.DocumentRead)]
    public async Task<ActionResult<DocumentDetailDto>> GetById(Guid id)
    {
        var doc = await LoadFull(id);
        if (doc is null) return NotFound(new { message = "Doküman bulunamadı." });
        return Ok(MapDetail(doc));
    }

    /// <summary>Yeni doküman oluşturur (durum: Draft, henüz dosya yok).</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.DocumentCreate)]
    public async Task<ActionResult<DocumentDetailDto>> Create([FromBody] CreateDocumentDto dto)
    {
        var id = await _documents.CreateAsync(dto);
        var doc = await LoadFull(id);
        return CreatedAtAction(nameof(GetById), new { id }, MapDetail(doc!));
    }

    /// <summary>Doküman metadata günceller (RowVersion ile eşzamanlılık).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.DocumentUpdate)]
    public async Task<ActionResult<DocumentDetailDto>> Update(Guid id, [FromBody] UpdateDocumentDto dto)
    {
        await _documents.UpdateAsync(id, dto);
        return Ok(MapDetail((await LoadFull(id))!));
    }

    /// <summary>Dokümanı yumuşak siler (sürüm/denetim/indirme geçmişi korunur).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.DocumentDelete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _documents.SoftDeleteAsync(id);
        return NoContent();
    }

    /// <summary>İlk dosyayı yükler; ilk sürümde Draft → Active.</summary>
    [HttpPost("{id:guid}/upload")]
    [Authorize(Policy = Permissions.DocumentUpload)]
    public async Task<ActionResult<DocumentDetailDto>> Upload(Guid id, [FromForm] IFormFile file, [FromForm] string? comment)
    {
        await _documents.AddVersionAsync(id, file, comment, DocumentEventTypes.Uploaded);
        return Ok(MapDetail((await LoadFull(id))!));
    }

    /// <summary>Yeni sürüm yükler (mevcut sürümler korunur, sürüm numarası artar).</summary>
    [HttpPost("{id:guid}/new-version")]
    [Authorize(Policy = Permissions.DocumentVersionCreate)]
    public async Task<ActionResult<DocumentDetailDto>> NewVersion(Guid id, [FromForm] IFormFile file, [FromForm] string? comment)
    {
        await _documents.AddVersionAsync(id, file, comment, DocumentEventTypes.VersionCreated);
        return Ok(MapDetail((await LoadFull(id))!));
    }

    /// <summary>Dokümanı arşivler (Active → Archived).</summary>
    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = Permissions.DocumentArchive)]
    public async Task<ActionResult<DocumentDetailDto>> Archive(Guid id)
    {
        await _documents.ArchiveAsync(id);
        return Ok(MapDetail((await LoadFull(id))!));
    }

    /// <summary>Sürüm indirir (varsayılan: güncel). İndirmeden önce SHA-256 bütünlüğü doğrulanır.</summary>
    [HttpGet("{id:guid}/download")]
    [Authorize(Policy = Permissions.DocumentDownload)]
    public async Task<IActionResult> Download(Guid id, [FromQuery] Guid? versionId)
    {
        var result = await _documents.DownloadAsync(id, versionId, Ip);
        return File(result.Content, result.MimeType, result.FileName);
    }

    /// <summary>Dokümanın tüm sürümlerini döndürür.</summary>
    [HttpGet("{id:guid}/versions")]
    [Authorize(Policy = Permissions.DocumentRead)]
    public async Task<ActionResult<IEnumerable<DocumentVersionDto>>> GetVersions(Guid id)
    {
        if (!await _db.Documents.AnyAsync(d => d.Id == id)) return NotFound(new { message = "Doküman bulunamadı." });
        var versions = await _db.DocumentVersions.AsNoTracking()
            .Where(v => v.DocumentId == id)
            .Include(v => v.UploadedByUser)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => MapVersionExpr(v))
            .ToListAsync();
        return Ok(versions);
    }

    /// <summary>Dokümanın bağlı olduğu iş nesnelerini döndürür.</summary>
    [HttpGet("{id:guid}/links")]
    [Authorize(Policy = Permissions.DocumentRead)]
    public async Task<ActionResult<IEnumerable<DocumentLinkDto>>> GetLinks(Guid id)
    {
        if (!await _db.Documents.AnyAsync(d => d.Id == id)) return NotFound(new { message = "Doküman bulunamadı." });
        var links = await _db.DocumentLinks.AsNoTracking()
            .Where(l => l.DocumentId == id).OrderByDescending(l => l.CreatedAt)
            .Select(l => new DocumentLinkDto { Id = l.Id, ObjectType = l.ObjectType, ObjectId = l.ObjectId, CreatedByUserId = l.CreatedByUserId, CreatedAt = l.CreatedAt })
            .ToListAsync();
        return Ok(links);
    }

    /// <summary>Dokümanı bir iş nesnesine bağlar (Change/Approval/Release/Deployment/Validation).</summary>
    [HttpPost("{id:guid}/attach")]
    [Authorize(Policy = Permissions.DocumentLink)]
    public async Task<IActionResult> Attach(Guid id, [FromBody] AttachDocumentDto dto)
    {
        await _documents.AttachAsync(id, dto);
        return NoContent();
    }

    /// <summary>Doküman bağını kaldırır.</summary>
    [HttpPost("{id:guid}/detach")]
    [Authorize(Policy = Permissions.DocumentUnlink)]
    public async Task<IActionResult> Detach(Guid id, [FromBody] AttachDocumentDto dto)
    {
        await _documents.DetachAsync(id, dto);
        return NoContent();
    }

    /// <summary>Doküman denetim olaylarını döndürür.</summary>
    [HttpGet("{id:guid}/audit")]
    [Authorize(Policy = Permissions.DocumentAuditRead)]
    public async Task<ActionResult<IEnumerable<DocumentAuditEventDto>>> GetAudit(Guid id)
    {
        if (!await _db.Documents.AnyAsync(d => d.Id == id)) return NotFound(new { message = "Doküman bulunamadı." });
        var events = await _db.DocumentAuditEvents.AsNoTracking()
            .Where(e => e.DocumentId == id).OrderByDescending(e => e.CreatedAt)
            .Select(e => new DocumentAuditEventDto { Id = e.Id, EventType = e.EventType, Description = e.Description, ActorUserId = e.ActorUserId, CreatedAt = e.CreatedAt })
            .ToListAsync();
        return Ok(events);
    }

    /// <summary>Dokümanın indirme geçmişini döndürür.</summary>
    [HttpGet("{id:guid}/downloads")]
    [Authorize(Policy = Permissions.DocumentAuditRead)]
    public async Task<ActionResult<IEnumerable<DocumentDownloadDto>>> GetDownloads(Guid id)
    {
        if (!await _db.Documents.AnyAsync(d => d.Id == id)) return NotFound(new { message = "Doküman bulunamadı." });
        var downloads = await _db.DocumentDownloads.AsNoTracking()
            .Where(dl => dl.DocumentId == id).OrderByDescending(dl => dl.DownloadedAt)
            .Select(dl => new DocumentDownloadDto
            {
                Id = dl.Id, VersionId = dl.VersionId, DownloadedByUserId = dl.DownloadedByUserId,
                DownloadedByUserName = _db.Users.Where(u => u.Id == dl.DownloadedByUserId).Select(u => u.FullName).FirstOrDefault() ?? string.Empty,
                DownloadedAt = dl.DownloadedAt, IpAddress = dl.IpAddress
            })
            .ToListAsync();
        return Ok(downloads);
    }

    /* ── helpers ── */

    private Task<Document?> LoadFull(Guid id) =>
        _db.Documents
            .Include(d => d.OwnerUser)
            .Include(d => d.Versions).ThenInclude(v => v.UploadedByUser)
            .Include(d => d.Links)
            .AsSplitQuery() // multiple collection includes → avoid Cartesian explosion
            .FirstOrDefaultAsync(d => d.Id == id);

    private static DocumentDetailDto MapDetail(Document d)
    {
        var current = d.Versions.FirstOrDefault(v => v.Id == d.CurrentVersionId);
        return new DocumentDetailDto
        {
            Id = d.Id, DocumentNo = d.DocumentNo, Title = d.Title, Description = d.Description,
            Category = d.Category, Status = d.Status,
            OwnerUserId = d.OwnerUserId, OwnerUserName = d.OwnerUser?.FullName ?? string.Empty,
            CurrentVersionId = d.CurrentVersionId, CurrentVersionNumber = current?.VersionNumber ?? 0,
            HashAlgorithm = d.HashAlgorithm, CurrentHash = d.CurrentHash,
            CreatedAt = d.CreatedAt, UpdatedAt = d.UpdatedAt,
            RowVersion = d.RowVersion is { Length: > 0 } ? Convert.ToBase64String(d.RowVersion) : string.Empty,
            Versions = d.Versions.OrderByDescending(v => v.VersionNumber).Select(v => new DocumentVersionDto
            {
                Id = v.Id, VersionNumber = v.VersionNumber, OriginalFileName = v.OriginalFileName,
                Extension = v.Extension, MimeType = v.MimeType, SizeBytes = v.SizeBytes, Sha256Hash = v.Sha256Hash,
                UploadedByUserId = v.UploadedByUserId, UploadedByUserName = v.UploadedByUser?.FullName ?? string.Empty,
                UploadedAt = v.UploadedAt, Comment = v.Comment
            }).ToList(),
            Links = d.Links.OrderByDescending(l => l.CreatedAt).Select(l => new DocumentLinkDto
            {
                Id = l.Id, ObjectType = l.ObjectType, ObjectId = l.ObjectId, CreatedByUserId = l.CreatedByUserId, CreatedAt = l.CreatedAt
            }).ToList()
        };
    }

    private static DocumentVersionDto MapVersionExpr(DocumentVersion v) => new()
    {
        Id = v.Id, VersionNumber = v.VersionNumber, OriginalFileName = v.OriginalFileName,
        Extension = v.Extension, MimeType = v.MimeType, SizeBytes = v.SizeBytes, Sha256Hash = v.Sha256Hash,
        UploadedByUserId = v.UploadedByUserId, UploadedByUserName = v.UploadedByUser!.FullName,
        UploadedAt = v.UploadedAt, Comment = v.Comment
    };
}
