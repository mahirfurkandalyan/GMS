namespace Gms.Api.Common;

/// <summary>Invalid file/input for a document operation (bad extension, too large, empty…). → 400.</summary>
public sealed class DocumentValidationException : Exception
{
    public DocumentValidationException(string message) : base(message) { }
}

/// <summary>Stored file failed its SHA-256 integrity check on download. → 500.</summary>
public sealed class DocumentIntegrityException : Exception
{
    public DocumentIntegrityException(string message) : base(message) { }
}
