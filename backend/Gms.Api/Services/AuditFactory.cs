using Gms.Api.Domain;

namespace Gms.Api.Services;

/// <summary>
/// Lightweight factory for append-only audit events. Removes the duplicated
/// audit-builder logic that previously lived in three places. Not an event bus —
/// just a single place that constructs audit entities consistently.
/// </summary>
public static class AuditFactory
{
    public static ChangeAuditEvent Change(string type, string description, Guid actorUserId, DateTime now) => new()
    {
        Id = Guid.NewGuid(), EventType = type, Description = description, ActorUserId = actorUserId, CreatedAt = now
    };

    public static ApprovalAuditEvent Approval(string type, string description, Guid actorUserId, DateTime now) => new()
    {
        Id = Guid.NewGuid(), EventType = type, Description = description, ActorUserId = actorUserId, CreatedAt = now
    };

    public static ReleaseAuditEvent Release(string type, string description, Guid actorUserId, DateTime now) => new()
    {
        Id = Guid.NewGuid(), EventType = type, Description = description, ActorUserId = actorUserId, CreatedAt = now
    };

    public static DeploymentEvent Deployment(string type, string description, Guid actorUserId, DateTime now) => new()
    {
        Id = Guid.NewGuid(), EventType = type, Description = description, ActorUserId = actorUserId, CreatedAt = now
    };

    public static ValidationEvent Validation(string type, string description, Guid actorUserId, DateTime now) => new()
    {
        Id = Guid.NewGuid(), EventType = type, Description = description, ActorUserId = actorUserId, CreatedAt = now
    };

    public static DocumentAuditEvent Document(string type, string description, Guid actorUserId, DateTime now) => new()
    {
        Id = Guid.NewGuid(), EventType = type, Description = description, ActorUserId = actorUserId, CreatedAt = now
    };

    public static NotificationEvent Notification(string type, string description, Guid actorUserId, DateTime now) => new()
    {
        Id = Guid.NewGuid(), EventType = type, Description = description, ActorUserId = actorUserId, CreatedAt = now
    };

    public static WorkflowEvent Workflow(string type, string description, Guid actorUserId, DateTime now, Guid? stepInstanceId = null) => new()
    {
        Id = Guid.NewGuid(), EventType = type, Description = description, ActorUserId = actorUserId,
        WorkflowStepInstanceId = stepInstanceId, CreatedAt = now
    };

    public static IntegrationEvent Integration(string type, string description, Guid definitionId, Guid? actorUserId,
        DateTime now, Guid? executionId = null) => new()
    {
        Id = Guid.NewGuid(), EventType = type, Description = description,
        IntegrationDefinitionId = definitionId, IntegrationExecutionId = executionId,
        ActorUserId = actorUserId, CreatedAt = now
    };
}
