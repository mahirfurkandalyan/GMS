namespace Gms.Api.Services.Documents;

/// <summary>Result of storing a file: the opaque storage path and its size.</summary>
public sealed record StoredFile(string StoragePath, long SizeBytes);

/// <summary>Physical-storage metadata (never exposed to callers as a physical path).</summary>
public sealed record FileMetadata(long SizeBytes, DateTime LastModifiedUtc);

/// <summary>
/// Storage abstraction so business/domain code never touches the file system directly.
/// A single implementation is used everywhere; swapping to cloud storage (S3/Blob) later
/// means adding one implementation, with no changes to DocumentService or controllers.
/// The <c>storagePath</c> is opaque (relative) and must never be surfaced in DTOs.
/// </summary>
public interface IFileStorage
{
    Task<StoredFile> UploadAsync(Stream content, string relativeDir, string storedFileName, CancellationToken ct = default);
    Task<byte[]> DownloadAsync(string storagePath, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default);
    Task<bool> ExistsAsync(string storagePath, CancellationToken ct = default);
    Task DeleteAsync(string storagePath, CancellationToken ct = default);
    Task<FileMetadata> GetMetadataAsync(string storagePath, CancellationToken ct = default);
}
