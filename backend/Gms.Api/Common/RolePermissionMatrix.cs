namespace Gms.Api.Common;

/// <summary>
/// The single, centralized role → permission mapping. Change authorization behaviour
/// here (and re-seed) — never scatter role checks across controllers. Admin implicitly
/// receives every permission in the catalog.
/// </summary>
public static class RolePermissionMatrix
{
    /// <summary>Self-service notification permissions every role receives.</summary>
    private static readonly string[] NotificationSelfPermissions =
    {
        Permissions.NotificationRead, Permissions.NotificationPreference, Permissions.NotificationArchive
    };

    // Workflow participation (can action assigned tasks).
    private static readonly string[] WorkflowParticipant =
    {
        Permissions.WorkflowInstanceRead, Permissions.WorkflowTaskRead, Permissions.WorkflowTaskComplete, Permissions.WorkflowTaskReject
    };
    // Workflow observer (read own tasks/instances only).
    private static readonly string[] WorkflowObserver = { Permissions.WorkflowInstanceRead, Permissions.WorkflowTaskRead };
    // Auditor also reads definitions.
    private static readonly string[] WorkflowAuditor = { Permissions.WorkflowDefinitionRead, Permissions.WorkflowInstanceRead, Permissions.WorkflowTaskRead };

    private static IReadOnlyList<string> WorkflowPermissionsFor(string role) => role switch
    {
        SystemRoles.Architect or SystemRoles.QA or SystemRoles.ReleaseManager => WorkflowParticipant,
        SystemRoles.Requester or SystemRoles.Executor or SystemRoles.Validator => WorkflowObserver,
        SystemRoles.Auditor => WorkflowAuditor,
        _ => Array.Empty<string>()
    };

    // Integration Hub: credential management stays narrow (Admin only, via catalog). Other roles
    // get read + a scoped capability. A future IntegrationManager role may broaden this.
    private static readonly string[] IntegrationAuditor = { Permissions.IntegrationRead, Permissions.IntegrationAuditRead };
    private static readonly string[] IntegrationReleaseManager = { Permissions.IntegrationRead, Permissions.IntegrationLinkManage };
    private static readonly string[] IntegrationReadOnly = { Permissions.IntegrationRead };

    private static IReadOnlyList<string> IntegrationPermissionsFor(string role) => role switch
    {
        SystemRoles.Auditor => IntegrationAuditor,
        SystemRoles.ReleaseManager => IntegrationReleaseManager,
        SystemRoles.Architect => IntegrationReadOnly,
        _ => Array.Empty<string>()
    };

    // Operations (background processing status). Manage (run-once diagnostics) stays Admin-only.
    private static readonly string[] OperationsReadOnly = { Permissions.OperationsRead };

    private static IReadOnlyList<string> OperationsPermissionsFor(string role) => role switch
    {
        SystemRoles.Auditor or SystemRoles.ReleaseManager => OperationsReadOnly,
        _ => Array.Empty<string>()
    };

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Map = Build();

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> Build()
    {
        var baseMap = new Dictionary<string, IReadOnlyList<string>>
        {
            [SystemRoles.Requester] = new[]
            {
                Permissions.ChangeRead, Permissions.ChangeCreate, Permissions.ChangeUpdate,
                Permissions.ChangeSubmit, Permissions.ChangeCancel, Permissions.ChangeRevisionCreate,
                Permissions.ReleaseRead, Permissions.ApprovalRead, Permissions.ValidationRead, Permissions.ExecutionRead,
                Permissions.DocumentRead, Permissions.DocumentCreate, Permissions.DocumentUpload, Permissions.DocumentDownload,
                Permissions.DocumentVersionCreate, Permissions.DocumentUpdate, Permissions.DocumentLink, Permissions.DocumentUnlink
            },

            [SystemRoles.Architect] = new[]
            {
                // all Requester read permissions
                Permissions.ChangeRead, Permissions.ReleaseRead, Permissions.ApprovalRead,
                Permissions.ValidationRead, Permissions.ExecutionRead,
                Permissions.ApprovalApproveArchitect, Permissions.ApprovalReject, Permissions.ApprovalRequestRevision,
                Permissions.DocumentRead, Permissions.DocumentCreate, Permissions.DocumentUpload, Permissions.DocumentDownload,
                Permissions.DocumentVersionCreate, Permissions.DocumentLink, Permissions.DocumentUnlink,
                Permissions.ReportRead
            },

            [SystemRoles.QA] = new[]
            {
                Permissions.ApprovalRead, Permissions.ApprovalApproveQa, Permissions.ApprovalReject,
                Permissions.ApprovalRequestRevision,
                Permissions.ValidationRead, Permissions.ValidationCreate, Permissions.ValidationStart,
                Permissions.ValidationCheckExecute,
                Permissions.ChangeRead, Permissions.ReleaseRead,
                Permissions.DocumentRead, Permissions.DocumentCreate, Permissions.DocumentUpload, Permissions.DocumentDownload,
                Permissions.DocumentVersionCreate, Permissions.DocumentLink, Permissions.DocumentUnlink,
                Permissions.ReportRead
            },

            [SystemRoles.ReleaseManager] = new[]
            {
                Permissions.ReleaseRead, Permissions.ReleaseCreate, Permissions.ReleaseUpdate,
                Permissions.ReleaseSchedule, Permissions.ReleaseCancel,
                Permissions.ApprovalRead, Permissions.ApprovalApproveReleaseManager, Permissions.ApprovalReject,
                Permissions.ApprovalRequestRevision,
                Permissions.ChangeRead, Permissions.ExecutionRead,
                Permissions.DocumentRead, Permissions.DocumentCreate, Permissions.DocumentUpload, Permissions.DocumentDownload,
                Permissions.DocumentVersionCreate, Permissions.DocumentUpdate, Permissions.DocumentArchive,
                Permissions.DocumentLink, Permissions.DocumentUnlink,
                Permissions.AuditRead, Permissions.ReportRead
            },

            [SystemRoles.Executor] = new[]
            {
                Permissions.ExecutionRead, Permissions.ExecutionCreate, Permissions.ExecutionStart,
                Permissions.ExecutionStepStart, Permissions.ExecutionStepComplete, Permissions.ExecutionStepFail,
                Permissions.ExecutionRollback,
                Permissions.ReleaseRead, Permissions.ChangeRead, Permissions.ValidationRead,
                Permissions.DocumentRead, Permissions.DocumentCreate, Permissions.DocumentUpload, Permissions.DocumentDownload,
                Permissions.DocumentVersionCreate, Permissions.DocumentLink, Permissions.DocumentUnlink
            },

            [SystemRoles.Validator] = new[]
            {
                Permissions.ValidationRead, Permissions.ValidationCreate, Permissions.ValidationStart,
                Permissions.ValidationCheckExecute,
                Permissions.ExecutionRead, Permissions.ReleaseRead, Permissions.ChangeRead,
                Permissions.DocumentRead, Permissions.DocumentCreate, Permissions.DocumentUpload, Permissions.DocumentDownload,
                Permissions.DocumentVersionCreate, Permissions.DocumentLink, Permissions.DocumentUnlink
            },

            [SystemRoles.Auditor] = new[]
            {
                Permissions.AuditRead, Permissions.ChangeRead, Permissions.ApprovalRead,
                Permissions.ReleaseRead, Permissions.ExecutionRead, Permissions.ValidationRead,
                Permissions.DocumentRead, Permissions.DocumentDownload, Permissions.DocumentAuditRead,
                Permissions.AuditSecurityRead, Permissions.AuditExport, Permissions.ReportRead, Permissions.ReportExport
            },

            // Admin gets every permission in the catalog.
            [SystemRoles.Admin] = Permissions.AllCodes.ToList()
        };

        // Every role can read/manage-preferences/archive its own notifications.
        // (Admin already has all permissions via the catalog.)
        var result = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var (role, perms) in baseMap)
        {
            if (role == SystemRoles.Admin) { result[role] = perms; continue; }
            result[role] = perms.Concat(NotificationSelfPermissions).Concat(WorkflowPermissionsFor(role))
                .Concat(IntegrationPermissionsFor(role)).Concat(OperationsPermissionsFor(role)).Distinct().ToList();
        }
        return result;
    }

    /// <summary>Flattens the permission set granted by a collection of role names.</summary>
    public static IReadOnlySet<string> PermissionsForRoles(IEnumerable<string> roleNames)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var role in roleNames)
        {
            if (Map.TryGetValue(role, out var perms))
                foreach (var p in perms) set.Add(p);
        }
        return set;
    }
}
