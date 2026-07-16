namespace Gms.Api.Common;

/// <summary>Authentication failed (bad credentials, locked account, invalid/expired token). → 401.</summary>
public sealed class AuthFailedException : Exception
{
    public AuthFailedException(string message) : base(message) { }
}

/// <summary>Authenticated but not permitted (defense-in-depth beyond policies). → 403.</summary>
public sealed class AuthForbiddenException : Exception
{
    public AuthForbiddenException(string message) : base(message) { }
}

/// <summary>Input/business validation error in the auth module (e.g. weak password). → 400.</summary>
public sealed class AuthValidationException : Exception
{
    public AuthValidationException(string message) : base(message) { }
}
