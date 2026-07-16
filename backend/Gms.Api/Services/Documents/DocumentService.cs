using System.Security.Cryptography;
using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Services.Documents;

public sealed record DocumentDownloadResult(byte[] Content, string FileName, string MimeType, Guid VersionId);

/// <summary>
/// Owns the whole document lifecycle: metadata, versioning, file storage (via
/// <see cref="IFileStorage"/>), SHA-256 hashing/integrity, linking, soft delete and the
/// full audit trail. This is a self-contained unit of work (file IO + DB together), so it
/// saves its own changes — keeping controllers thin and avoiding duplicated storage/hash/
/// audit logic anywhere else in the platform. The actor always comes from ICurrentUser.
/// </summary>
public sealed class DocumentService
{
    private readonly GmsDbContext _db;
    private readonly IFileStorage _storage;
    private readonly SequentialNumberGenerator _numbers;
    private readonly ICurrentUser _currentUser;
    private readonly Notifications.NotificationService _notifications;

    public DocumentService(GmsDbContext db, IFileStorage storage, SequentialNumberGenerator numbers,
        ICurrentUser currentUser, Notifications.NotificationService notifications)
    {
        _db = db;
        _storage = storage;
        _numbers = numbers;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    /// <summary>Creates a document (Draft, no file yet).</summary>
    public async Task<Guid> CreateAsync(CreateDocumentDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new DocumentValidationException("Doküman başlığı zorunludur.");
        if (!DocumentCategories.All.Contains(dto.Category))
            throw new DocumentValidationException("Geçersiz doküman kategorisi.");

        var actor = _currentUser.RequireUserId();
        var now = DateTime.UtcNow;
        var documentNo = await _numbers.NextAsync($"DOC-{now.Year}-", _db.Documents.Select(d => d.DocumentNo), ct);

        var doc = new Document
        {
            Id = Guid.NewGuid(),
            DocumentNo = documentNo,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            Category = dto.Category,
            Status = DocumentStatuses.Draft,
            OwnerUserId = actor,
            HashAlgorithm = DocumentFileRules.HashAlgorithm,
            CreatedAt = now
        };
        doc.AuditEvents.Add(AuditFactory.Document(DocumentEventTypes.DocumentCreated,
            $"Doküman oluşturuldu ({documentNo}).", actor, now));

        _db.Documents.Add(doc);
        await _db.SaveChangesAsync(ct);
        return doc.Id;
    }

    /// <summary>
    /// Adds a new immutable version from an uploaded file: validates, hashes (SHA-256),
    /// stores via IFileStorage, increments the version number, updates CurrentVersionId/Hash,
    /// and (for the first version) transitions Draft → Active. Never overwrites a file.
    /// </summary>
    public async Task AddVersionAsync(Guid documentId, IFormFile? file, string? comment, string eventType, CancellationToken ct = default)
    {
        var doc = await LoadForWriteAsync(documentId, includeVersions: true, ct);
        if (doc.Status == DocumentStatuses.Deleted)
            throw new DocumentValidationException("Silinmiş dokümana sürüm eklenemez.");

        var (originalName, extension) = ValidateFile(file);
        var actor = _currentUser.RequireUserId();
        var now = DateTime.UtcNow;

        // Read once → hash → store (from the same buffered copy). Never trust the client name.
        await using var buffer = new MemoryStream();
        await file!.CopyToAsync(buffer, ct);
        if (buffer.Length == 0) throw new DocumentValidationException("Boş dosya yüklenemez.");
        buffer.Position = 0;
        var sha256 = Convert.ToHexString(SHA256.HashData(buffer)).ToLowerInvariant();
        buffer.Position = 0;

        var versionNumber = doc.Versions.Count == 0 ? 1 : doc.Versions.Max(v => v.VersionNumber) + 1;
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var relativeDir = $"{now.Year:0000}/{now.Month:00}/{doc.DocumentNo}/v{versionNumber}";
        var stored = await _storage.UploadAsync(buffer, relativeDir, storedFileName, ct);

        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = doc.Id,
            VersionNumber = versionNumber,
            StoragePath = stored.StoragePath,
            OriginalFileName = originalName,
            StoredFileName = storedFileName,
            Extension = extension,
            MimeType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            SizeBytes = stored.SizeBytes,
            Sha256Hash = sha256,
            UploadedByUserId = actor,
            UploadedAt = now,
            Comment = comment?.Trim() ?? string.Empty
        };
        doc.Versions.Add(version);

        doc.CurrentVersionId = version.Id;
        doc.CurrentHash = sha256;
        doc.UpdatedAt = now;

        // First version activates the document.
        if (versionNumber == 1 && doc.Status == DocumentStatuses.Draft)
        {
            doc.TransitionTo(DocumentStatuses.Active);
            doc.AuditEvents.Add(AuditFactory.Document(DocumentEventTypes.Activated, "Doküman aktifleştirildi.", actor, now));
        }

        doc.AuditEvents.Add(AuditFactory.Document(eventType,
            $"Sürüm {versionNumber} yüklendi: {originalName} ({stored.SizeBytes} bayt).", actor, now));
        // First-file upload additionally records VersionCreated; new-version already is one.
        if (eventType != DocumentEventTypes.VersionCreated)
            doc.AuditEvents.Add(AuditFactory.Document(DocumentEventTypes.VersionCreated,
                $"Sürüm {versionNumber} oluşturuldu.", actor, now));

        // Notify the document owner (central engine). Skip when the owner is the uploader.
        if (doc.OwnerUserId != actor)
        {
            var template = versionNumber == 1
                ? Common.NotificationTemplates.DocumentUploaded
                : Common.NotificationTemplates.DocumentVersionCreated;
            await _notifications.NotifyUserAsync(doc.OwnerUserId, template, Common.NotificationSeverities.Information,
                new Dictionary<string, string> { ["DocumentNo"] = doc.DocumentNo, ["Version"] = $"v{versionNumber}" }, actor);
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Downloads a version (latest by default): verifies SHA-256 integrity, records the
    /// download + audit, and returns the file bytes. On integrity mismatch it audits
    /// IntegrityCheckFailed and throws (→ 500) — the file is never served.
    /// </summary>
    public async Task<DocumentDownloadResult> DownloadAsync(Guid documentId, Guid? versionId, string? ip, CancellationToken ct = default)
    {
        var doc = await _db.Documents
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == documentId, ct)
            ?? throw new KeyNotFoundException("Doküman bulunamadı.");
        if (doc.Status == DocumentStatuses.Deleted)
            throw new KeyNotFoundException("Doküman bulunamadı.");

        var version = versionId is null
            ? doc.Versions.FirstOrDefault(v => v.Id == doc.CurrentVersionId)
            : doc.Versions.FirstOrDefault(v => v.Id == versionId.Value);
        if (version is null)
            throw new KeyNotFoundException("İstenen sürüm bulunamadı.");

        var actor = _currentUser.RequireUserId();
        var now = DateTime.UtcNow;

        var content = await _storage.DownloadAsync(version.StoragePath, ct);
        var actualHash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

        if (!string.Equals(actualHash, version.Sha256Hash, StringComparison.OrdinalIgnoreCase))
        {
            doc.AuditEvents.Add(AuditFactory.Document(DocumentEventTypes.IntegrityCheckFailed,
                $"Sürüm {version.VersionNumber} bütünlük kontrolü BAŞARISIZ (hash uyuşmuyor).", actor, now));
            await _db.SaveChangesAsync(ct);
            throw new DocumentIntegrityException("Dosya bütünlük doğrulaması başarısız oldu; indirme reddedildi.");
        }

        doc.Downloads.Add(new DocumentDownload
        {
            Id = Guid.NewGuid(), DocumentId = doc.Id, VersionId = version.Id,
            DownloadedByUserId = actor, DownloadedAt = now, IpAddress = ip
        });
        doc.AuditEvents.Add(AuditFactory.Document(DocumentEventTypes.Downloaded,
            $"Sürüm {version.VersionNumber} indirildi.", actor, now));
        await _db.SaveChangesAsync(ct);

        return new DocumentDownloadResult(content, version.OriginalFileName, version.MimeType, version.Id);
    }

    public async Task UpdateAsync(Guid documentId, UpdateDocumentDto dto, CancellationToken ct = default)
    {
        var doc = await LoadForWriteAsync(documentId, includeVersions: false, ct);
        if (doc.Status == DocumentStatuses.Deleted)
            throw new DocumentValidationException("Silinmiş doküman güncellenemez.");

        if (!string.IsNullOrWhiteSpace(dto.RowVersion))
            _db.Entry(doc).Property(d => d.RowVersion).OriginalValue = Convert.FromBase64String(dto.RowVersion);

        if (dto.Category is not null && !DocumentCategories.All.Contains(dto.Category))
            throw new DocumentValidationException("Geçersiz doküman kategorisi.");

        var actor = _currentUser.RequireUserId();
        if (!string.IsNullOrWhiteSpace(dto.Title)) doc.Title = dto.Title.Trim();
        if (dto.Description is not null) doc.Description = dto.Description.Trim();
        if (dto.Category is not null) doc.Category = dto.Category;
        doc.UpdatedAt = DateTime.UtcNow;
        doc.AuditEvents.Add(AuditFactory.Document(DocumentEventTypes.DocumentUpdated, "Doküman güncellendi.", actor, doc.UpdatedAt.Value));

        await _db.SaveChangesAsync(ct);
    }

    public async Task ArchiveAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await LoadForWriteAsync(documentId, includeVersions: false, ct);
        var actor = _currentUser.RequireUserId();
        var now = DateTime.UtcNow;
        doc.TransitionTo(DocumentStatuses.Archived);
        doc.UpdatedAt = now;
        doc.AuditEvents.Add(AuditFactory.Document(DocumentEventTypes.Archived, "Doküman arşivlendi.", actor, now));
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Soft delete: status → Deleted, DeletedAt set. Versions, files, audit and
    /// download history are kept intact.</summary>
    public async Task SoftDeleteAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await LoadForWriteAsync(documentId, includeVersions: false, ct);
        var actor = _currentUser.RequireUserId();
        var now = DateTime.UtcNow;
        doc.TransitionTo(DocumentStatuses.Deleted);
        doc.DeletedAt = now;
        doc.UpdatedAt = now;
        doc.AuditEvents.Add(AuditFactory.Document(DocumentEventTypes.Deleted, "Doküman silindi (soft delete).", actor, now));
        await _db.SaveChangesAsync(ct);
    }

    public async Task AttachAsync(Guid documentId, AttachDocumentDto dto, CancellationToken ct = default)
    {
        if (!DocumentObjectTypes.All.Contains(dto.ObjectType))
            throw new DocumentValidationException("Geçersiz nesne türü (ObjectType).");
        if (dto.ObjectId == Guid.Empty)
            throw new DocumentValidationException("ObjectId zorunludur.");

        var doc = await LoadForWriteAsync(documentId, includeVersions: false, ct);
        if (doc.Status == DocumentStatuses.Deleted)
            throw new DocumentValidationException("Silinmiş dokümana bağlantı eklenemez.");

        var exists = await _db.DocumentLinks.AnyAsync(l =>
            l.DocumentId == documentId && l.ObjectType == dto.ObjectType && l.ObjectId == dto.ObjectId, ct);
        if (exists) return; // idempotent

        var actor = _currentUser.RequireUserId();
        var now = DateTime.UtcNow;
        doc.Links.Add(new DocumentLink
        {
            Id = Guid.NewGuid(), DocumentId = doc.Id, ObjectType = dto.ObjectType,
            ObjectId = dto.ObjectId, CreatedByUserId = actor, CreatedAt = now
        });
        doc.AuditEvents.Add(AuditFactory.Document(DocumentEventTypes.Attached,
            $"{dto.ObjectType} nesnesine bağlandı ({dto.ObjectId}).", actor, now));
        await _db.SaveChangesAsync(ct);
    }

    public async Task DetachAsync(Guid documentId, AttachDocumentDto dto, CancellationToken ct = default)
    {
        var doc = await LoadForWriteAsync(documentId, includeVersions: false, ct);
        var link = await _db.DocumentLinks.FirstOrDefaultAsync(l =>
            l.DocumentId == documentId && l.ObjectType == dto.ObjectType && l.ObjectId == dto.ObjectId, ct);
        if (link is null) return; // idempotent

        _db.DocumentLinks.Remove(link);
        var actor = _currentUser.RequireUserId();
        var now = DateTime.UtcNow;
        doc.AuditEvents.Add(AuditFactory.Document(DocumentEventTypes.Detached,
            $"{dto.ObjectType} bağlantısı kaldırıldı ({dto.ObjectId}).", actor, now));
        await _db.SaveChangesAsync(ct);
    }

    /* ── helpers ── */

    private async Task<Document> LoadForWriteAsync(Guid id, bool includeVersions, CancellationToken ct)
    {
        var query = _db.Documents.Include(d => d.AuditEvents).AsQueryable();
        if (includeVersions) query = query.Include(d => d.Versions);
        var doc = await query.FirstOrDefaultAsync(d => d.Id == id, ct);
        return doc ?? throw new KeyNotFoundException("Doküman bulunamadı.");
    }

    private static (string OriginalName, string Extension) ValidateFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            throw new DocumentValidationException("Boş veya eksik dosya.");
        if (file.Length > DocumentFileRules.MaxSizeBytes)
            throw new DocumentValidationException($"Dosya boyutu üst sınırı aşıyor (maks {DocumentFileRules.MaxSizeBytes / (1024 * 1024)} MB).");

        // Never trust the client path — keep only the file name.
        var originalName = Path.GetFileName(file.FileName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(originalName))
            throw new DocumentValidationException("Geçersiz dosya adı.");
        if (originalName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new DocumentValidationException("Dosya adı geçersiz karakter içeriyor.");

        var extension = Path.GetExtension(originalName).ToLowerInvariant();
        if (DocumentFileRules.DangerousExtensions.Contains(extension))
            throw new DocumentValidationException($"Güvenlik nedeniyle '{extension}' uzantılı dosyalar reddedilir.");

        return (originalName, extension);
    }
}
