namespace Gms.Api.Common;

/// <summary>The fixed set of seeded system roles.</summary>
public static class SystemRoles
{
    public const string Requester = "Requester";
    public const string Architect = "Architect";
    public const string QA = "QA";
    public const string ReleaseManager = "ReleaseManager";
    public const string Executor = "Executor";
    public const string Validator = "Validator";
    public const string Auditor = "Auditor";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Requester, Architect, QA, ReleaseManager, Executor, Validator, Auditor, Admin
    };
}

/// <summary>Security audit event type vocabulary (authentication lifecycle).</summary>
public static class SecurityEventTypes
{
    public const string LoginSucceeded = "LoginSucceeded";
    public const string LoginFailed = "LoginFailed";
    public const string UserLockedOut = "UserLockedOut";
    public const string TokenRefreshed = "TokenRefreshed";
    public const string TokenRefreshFailed = "TokenRefreshFailed";
    public const string Logout = "Logout";
    public const string LogoutAll = "LogoutAll";
    public const string PasswordChanged = "PasswordChanged";
    public const string AuditExported = "AuditExported";
    public const string ReportExported = "ReportExported";
}

/// <summary>Result vocabulary for security audit events.</summary>
public static class SecurityEventResults
{
    public const string Success = "Success";
    public const string Failure = "Failure";
}
