namespace Gms.Api.Common;

/// <summary>Invalid integration input/state/configuration → HTTP 400.</summary>
public sealed class IntegrationValidationException : Exception
{
    public IntegrationValidationException(string message) : base(message) { }
}

/// <summary>Invalid/missing webhook signature or secret → HTTP 401.</summary>
public sealed class IntegrationSignatureException : Exception
{
    public IntegrationSignatureException(string message) : base(message) { }
}

/// <summary>Duplicate incoming delivery / duplicate external link → HTTP 409.</summary>
public sealed class IntegrationDuplicateException : Exception
{
    public IntegrationDuplicateException(string message) : base(message) { }
}
