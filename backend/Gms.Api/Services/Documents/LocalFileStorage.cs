namespace Gms.Api.Services.Documents;

/// <summary>
/// Local-disk implementation of <see cref="IFileStorage"/>. Files live under
/// storage/documents/ organised as /year/month/documentNo/vN/storedFileName. Callers only
/// ever see the relative storage path; physical paths are resolved internally and guarded
/// against path traversal. Files are never overwritten (each version is a new path).
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IWebHostEnvironment env, IConfiguration config)
    {
        var configured = config["Storage:DocumentsRoot"];
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(env.ContentRootPath, "storage", "documents")
            : configured;
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredFile> UploadAsync(Stream content, string relativeDir, string storedFileName, CancellationToken ct = default)
    {
        var storagePath = CombineRelative(relativeDir, storedFileName);
        var physical = ResolvePhysical(storagePath);
        Directory.CreateDirectory(Path.GetDirectoryName(physical)!);

        // Never overwrite: exclusive create.
        await using (var fs = new FileStream(physical, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            if (content.CanSeek) content.Position = 0;
            await content.CopyToAsync(fs, ct);
        }

        var size = new FileInfo(physical).Length;
        return new StoredFile(storagePath, size);
    }

    public async Task<byte[]> DownloadAsync(string storagePath, CancellationToken ct = default) =>
        await File.ReadAllBytesAsync(ResolvePhysical(storagePath), ct);

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default) =>
        Task.FromResult<Stream>(new FileStream(ResolvePhysical(storagePath), FileMode.Open, FileAccess.Read, FileShare.Read));

    public Task<bool> ExistsAsync(string storagePath, CancellationToken ct = default) =>
        Task.FromResult(File.Exists(ResolvePhysical(storagePath)));

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var physical = ResolvePhysical(storagePath);
        if (File.Exists(physical)) File.Delete(physical);
        return Task.CompletedTask;
    }

    public Task<FileMetadata> GetMetadataAsync(string storagePath, CancellationToken ct = default)
    {
        var info = new FileInfo(ResolvePhysical(storagePath));
        return Task.FromResult(new FileMetadata(info.Exists ? info.Length : 0, info.Exists ? info.LastWriteTimeUtc : default));
    }

    /* ── helpers ── */

    private static string CombineRelative(string relativeDir, string fileName) =>
        $"{relativeDir.Replace('\\', '/').Trim('/')}/{fileName}";

    /// <summary>Resolves a storage-relative path to an absolute path, guarding traversal.</summary>
    private string ResolvePhysical(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath) || storagePath.Contains(".."))
            throw new InvalidOperationException("Geçersiz depolama yolu.");

        var relative = storagePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(_root, relative));

        var rootFull = Path.GetFullPath(_root);
        if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Depolama kök dizini dışına erişim engellendi.");

        return full;
    }
}
