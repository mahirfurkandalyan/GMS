using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gms.Api.Migrations
{
    /// <inheritdoc />
    public partial class WorkflowEngineDomain : Migration
    {
        // The eight domain-audit UNIONs shared by both view variants (Change..Notification).
        private const string UnifiedAuditBaseUnions = @"
SELECT e.Id AS RecordId, CAST('CHANGE' AS nvarchar(20)) AS SourceModule, CAST('ChangeAuditEvents' AS nvarchar(50)) AS SourceTable,
       CAST(e.EventType AS nvarchar(50)) AS EventType, CAST(e.Description AS nvarchar(1000)) AS Description, e.ActorUserId,
       CAST('ChangeRequest' AS nvarchar(40)) AS ObjectType, e.ChangeRequestId AS ObjectId, CAST(c.ChangeNo AS nvarchar(50)) AS ObjectNumber,
       c.ProjectId AS RelatedProjectId, c.EnvironmentId AS RelatedEnvironmentId,
       CAST(NULL AS nvarchar(20)) AS Result, CAST(NULL AS nvarchar(64)) AS IpAddress, e.CreatedAt
FROM ChangeAuditEvents e INNER JOIN ChangeRequests c ON c.Id = e.ChangeRequestId
UNION ALL
SELECT e.Id, CAST('APPROVAL' AS nvarchar(20)), CAST('ApprovalAuditEvents' AS nvarchar(50)),
       CAST(e.EventType AS nvarchar(50)), CAST(e.Description AS nvarchar(1000)), e.ActorUserId,
       CAST('ApprovalRequest' AS nvarchar(40)), e.ApprovalRequestId, CAST(a.ApprovalNo AS nvarchar(50)),
       rc.ProjectId, rc.EnvironmentId, CAST(NULL AS nvarchar(20)), CAST(NULL AS nvarchar(64)), e.CreatedAt
FROM ApprovalAuditEvents e INNER JOIN ApprovalRequests a ON a.Id = e.ApprovalRequestId
     LEFT JOIN ChangeRequests rc ON rc.Id = a.RelatedObjectId AND a.RelatedObjectType = 'ChangeRequest'
UNION ALL
SELECT e.Id, CAST('RELEASE' AS nvarchar(20)), CAST('ReleaseAuditEvents' AS nvarchar(50)),
       CAST(e.EventType AS nvarchar(50)), CAST(e.Description AS nvarchar(1000)), e.ActorUserId,
       CAST('ReleasePlan' AS nvarchar(40)), e.ReleasePlanId, CAST(r.ReleaseNo AS nvarchar(50)),
       r.ProjectId, r.EnvironmentId, CAST(NULL AS nvarchar(20)), CAST(NULL AS nvarchar(64)), e.CreatedAt
FROM ReleaseAuditEvents e INNER JOIN ReleasePlans r ON r.Id = e.ReleasePlanId
UNION ALL
SELECT e.Id, CAST('EXECUTION' AS nvarchar(20)), CAST('DeploymentEvents' AS nvarchar(50)),
       CAST(e.EventType AS nvarchar(50)), CAST(e.Description AS nvarchar(1000)), e.ActorUserId,
       CAST('DeploymentRun' AS nvarchar(40)), e.DeploymentRunId, CAST(d.ExecutionNo AS nvarchar(50)),
       r.ProjectId, r.EnvironmentId, CAST(NULL AS nvarchar(20)), CAST(NULL AS nvarchar(64)), e.CreatedAt
FROM DeploymentEvents e INNER JOIN DeploymentRuns d ON d.Id = e.DeploymentRunId
     INNER JOIN ReleasePlans r ON r.Id = d.ReleasePlanId
UNION ALL
SELECT e.Id, CAST('VALIDATION' AS nvarchar(20)), CAST('ValidationEvents' AS nvarchar(50)),
       CAST(e.EventType AS nvarchar(50)), CAST(e.Description AS nvarchar(1000)), e.ActorUserId,
       CAST('ValidationRun' AS nvarchar(40)), e.ValidationRunId, CAST(v.ValidationNo AS nvarchar(50)),
       r.ProjectId, r.EnvironmentId, CAST(NULL AS nvarchar(20)), CAST(NULL AS nvarchar(64)), e.CreatedAt
FROM ValidationEvents e INNER JOIN ValidationRuns v ON v.Id = e.ValidationRunId
     INNER JOIN DeploymentRuns d ON d.Id = v.DeploymentRunId
     INNER JOIN ReleasePlans r ON r.Id = d.ReleasePlanId
UNION ALL
SELECT e.Id, CAST('DOCUMENT' AS nvarchar(20)), CAST('DocumentAuditEvents' AS nvarchar(50)),
       CAST(e.EventType AS nvarchar(50)), CAST(e.Description AS nvarchar(1000)), e.ActorUserId,
       CAST('Document' AS nvarchar(40)), e.DocumentId, CAST(doc.DocumentNo AS nvarchar(50)),
       CAST(NULL AS uniqueidentifier), CAST(NULL AS uniqueidentifier), CAST(NULL AS nvarchar(20)), CAST(NULL AS nvarchar(64)), e.CreatedAt
FROM DocumentAuditEvents e INNER JOIN Documents doc ON doc.Id = e.DocumentId
UNION ALL
SELECT e.Id, CAST('SECURITY' AS nvarchar(20)), CAST('SecurityAuditEvents' AS nvarchar(50)),
       CAST(e.EventType AS nvarchar(50)), CAST(e.Description AS nvarchar(1000)), e.UserId,
       CAST('Security' AS nvarchar(40)), e.UserId, CAST(e.Email AS nvarchar(50)),
       CAST(NULL AS uniqueidentifier), CAST(NULL AS uniqueidentifier), CAST(e.Result AS nvarchar(20)), CAST(e.IpAddress AS nvarchar(64)), e.CreatedAt
FROM SecurityAuditEvents e
UNION ALL
SELECT e.Id, CAST('NOTIFICATION' AS nvarchar(20)), CAST('NotificationEvents' AS nvarchar(50)),
       CAST(e.EventType AS nvarchar(50)), CAST(e.Description AS nvarchar(1000)), e.ActorUserId,
       CAST('Notification' AS nvarchar(40)), e.NotificationId, CAST(n.NotificationNo AS nvarchar(50)),
       CAST(NULL AS uniqueidentifier), CAST(NULL AS uniqueidentifier), CAST(NULL AS nvarchar(20)), CAST(NULL AS nvarchar(64)), e.CreatedAt
FROM NotificationEvents e INNER JOIN Notifications n ON n.Id = e.NotificationId";

        private static readonly string UnifiedAuditViewWithoutWorkflow =
            "CREATE VIEW vw_UnifiedAuditRecords AS" + UnifiedAuditBaseUnions + ";";

        private static readonly string UnifiedAuditViewWithWorkflow =
            "CREATE VIEW vw_UnifiedAuditRecords AS" + UnifiedAuditBaseUnions + @"
UNION ALL
SELECT e.Id, CAST('WORKFLOW' AS nvarchar(20)), CAST('WorkflowEvents' AS nvarchar(50)),
       CAST(e.EventType AS nvarchar(50)), CAST(e.Description AS nvarchar(1000)), e.ActorUserId,
       CAST('WorkflowInstance' AS nvarchar(40)), e.WorkflowInstanceId, CAST(wi.InstanceNo AS nvarchar(50)),
       wi.RelatedProjectId, wi.RelatedEnvironmentId, CAST(NULL AS nvarchar(20)), CAST(NULL AS nvarchar(64)), e.CreatedAt
FROM WorkflowEvents e INNER JOIN WorkflowInstances wi ON wi.Id = e.WorkflowInstanceId;";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TriggerObjectType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TriggerEvent = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChangeClass = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActiveVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartStepKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowVersions_WorkflowDefinitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalTable: "WorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstanceNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggerObjectType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TriggerObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggerObjectNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CurrentStepInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelatedProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelatedEnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContextJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StartedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowInstances_WorkflowDefinitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalTable: "WorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowInstances_WorkflowVersions_WorkflowVersionId",
                        column: x => x.WorkflowVersionId,
                        principalTable: "WorkflowVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStepDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    StepType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    AssignedRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssignedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DueInHours = table.Column<int>(type: "int", nullable: true),
                    NotificationTemplateCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    NotificationRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStepDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowStepDefinitions_WorkflowVersions_WorkflowVersionId",
                        column: x => x.WorkflowVersionId,
                        principalTable: "WorkflowVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTransitionDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStepKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ToStepKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ConditionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ConditionField = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Operator = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ExpectedValue = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTransitionDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitionDefinitions_WorkflowVersions_WorkflowVersionId",
                        column: x => x.WorkflowVersionId,
                        principalTable: "WorkflowVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowStepInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowEvents_WorkflowInstances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStepInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    StepType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AssignedRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssignedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActionedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStepInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowStepInstances_WorkflowInstances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "NotificationTemplates",
                columns: new[] { "Id", "BodyTemplate", "Code", "CreatedAt", "IsSystem", "Module", "Name", "SubjectTemplate" },
                values: new object[,]
                {
                    { new Guid("0ed3ca1e-3763-e185-b07f-8f20aa3b5a95"), "{{ChangeNo}} değişikliğinin '{{StepName}}' adımının son tarihi ({{DueAt}}) yaklaşıyor.", "WorkflowTaskDueSoon", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "WORKFLOW", "Workflow görevi süresi yaklaşıyor", "Görev süresi yaklaşıyor: {{StepName}}" },
                    { new Guid("3604fe71-7d66-08e6-6c17-864b6e77520f"), "{{ChangeNo}} numaralı değişikliğin onay akışı '{{StepName}}' adımında reddedildi.", "WorkflowRejected", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "WORKFLOW", "Workflow reddedildi", "Akış reddedildi: {{ChangeNo}}" },
                    { new Guid("455f0664-1270-d875-451b-0bfeccd8af79"), "{{ChangeNo}} numaralı değişikliğin onay akışı ({{WorkflowName}}) iptal edildi.", "WorkflowCancelled", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "WORKFLOW", "Workflow iptal edildi", "Akış iptal edildi: {{ChangeNo}}" },
                    { new Guid("8bdce82a-4fd1-6297-f654-54d8f7f761f5"), "{{ChangeNo}} değişikliğinin '{{StepName}}' adımı son tarihini ({{DueAt}}) geçti.", "WorkflowTaskOverdue", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "WORKFLOW", "Workflow görevi gecikti", "Görev gecikti: {{StepName}}" },
                    { new Guid("8e2d8852-d9c7-bc04-fe76-d520e5fffed6"), "{{ChangeNo}} numaralı değişikliğin onay akışı ({{WorkflowName}}) tamamlandı ve değişiklik onaylandı.", "WorkflowCompleted", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "WORKFLOW", "Workflow tamamlandı", "Akış tamamlandı: {{ChangeNo}}" },
                    { new Guid("cc4aa076-f556-96e1-cfd8-17aebed6ec0a"), "{{ChangeNo}} numaralı değişikliğin '{{StepName}}' adımı ({{WorkflowName}}) sizin aksiyonunuzu bekliyor.", "WorkflowTaskAssigned", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "WORKFLOW", "Workflow görevi atandı", "Göreviniz bekliyor: {{StepName}}" }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { new Guid("18c14756-50e3-7391-218e-04ce74204c2c"), "workflow.task.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Atanan görevleri görüntüleme.", "WORKFLOW", "Workflow görevi görüntüleme" },
                    { new Guid("38e49ddd-d6dd-6da1-d7c5-2f6f40a89a89"), "workflow.instance.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Workflow örneklerini görüntüleme.", "WORKFLOW", "Workflow örneği görüntüleme" },
                    { new Guid("43cad4fb-7bdb-8afb-424c-98a8f7dbb4eb"), "workflow.instance.start", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Workflow örneği başlatma.", "WORKFLOW", "Workflow başlatma" },
                    { new Guid("5d528578-f344-5ad5-106e-8830c77588d8"), "workflow.definition.update", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Taslak versiyon adım/geçişlerini düzenleme.", "WORKFLOW", "Workflow tanımı güncelleme" },
                    { new Guid("61258223-c927-82ba-1c3f-4567e2f1267b"), "workflow.admin.override", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Atama dışı yönetici müdahalesi (denetlenir).", "WORKFLOW", "Workflow yönetici override" },
                    { new Guid("75c444a5-a5ca-bc76-74a2-4285230caabb"), "workflow.task.reject", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Atanan onay adımını reddetme.", "WORKFLOW", "Workflow görevi reddi" },
                    { new Guid("87ab24e6-ed75-904c-49ad-015649535f0d"), "workflow.definition.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Workflow tanımlarını/versiyonlarını görüntüleme.", "WORKFLOW", "Workflow tanımı görüntüleme" },
                    { new Guid("88448308-513f-6497-f557-c0b724bfbef6"), "workflow.definition.create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Workflow tanımı/taslak versiyon oluşturma.", "WORKFLOW", "Workflow tanımı oluşturma" },
                    { new Guid("8cd4504c-f307-b8e1-a146-aeebbc42c1ca"), "workflow.instance.cancel", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Workflow örneğini iptal etme.", "WORKFLOW", "Workflow iptali" },
                    { new Guid("bad559b9-ae81-21c2-0f14-558258239bc7"), "workflow.instance.pause", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Workflow örneğini duraklatma.", "WORKFLOW", "Workflow duraklatma" },
                    { new Guid("e7b825a1-6db0-0559-6d41-40e922b7cf17"), "workflow.definition.publish", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bir versiyonu doğrulayıp yayınlama (immutable).", "WORKFLOW", "Workflow yayınlama" },
                    { new Guid("e876a387-8528-144e-e79e-3e65a4b5f8a1"), "workflow.task.complete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Atanan onay/görev adımını tamamlama.", "WORKFLOW", "Workflow görevi tamamlama" },
                    { new Guid("efd3ed09-db1f-2a2f-5a7f-5d3f19228ff5"), "workflow.definition.activate", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yayınlanmış versiyonu aktif yapma.", "WORKFLOW", "Workflow aktifleştirme" },
                    { new Guid("f6ce732d-1c74-9489-4faa-35778127f820"), "workflow.definition.archive", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Workflow tanımını arşivleme.", "WORKFLOW", "Workflow arşivleme" },
                    { new Guid("fd7432dc-668e-4687-2a6c-9cccf7361ce0"), "workflow.instance.resume", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Duraklatılmış workflow'u sürdürme.", "WORKFLOW", "Workflow devam" }
                });

            migrationBuilder.InsertData(
                table: "WorkflowDefinitions",
                columns: new[] { "Id", "ActiveVersionId", "Category", "ChangeClass", "Code", "CreatedAt", "CreatedByUserId", "Description", "IsSystem", "Name", "Status", "TriggerEvent", "TriggerObjectType", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("5d087898-4f7d-8654-6112-dfb54b45a76a"), new Guid("1f1bfe70-af19-76a2-f637-40d5fea1962b"), "ChangeManagement", "Emergency", "CHANGE_EMERGENCY_DEFAULT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b2222222-2222-2222-2222-222222222205"), "Acil değişiklikler; Mimari + Yayın Yöneticisi + Admin onayı.", true, "Acil Değişiklik Akışı", "Active", "ChangeSubmitted", "ChangeRequest", null },
                    { new Guid("c497907e-05a2-2789-e49f-0c93816cc587"), new Guid("54a48a04-d65e-3705-1043-8d211f9c489a"), "ChangeManagement", "Normal", "CHANGE_NORMAL_DEFAULT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b2222222-2222-2222-2222-222222222205"), "Normal değişiklikler; yüksek/kritik risk için Yayın Yöneticisi onayı eklenir.", true, "Normal Değişiklik Akışı", "Active", "ChangeSubmitted", "ChangeRequest", null },
                    { new Guid("d85892c5-aaf7-0f2e-aa8b-546cc64795f2"), new Guid("79014796-e57b-7b75-7a08-51c263f2e4fd"), "ChangeManagement", "Standard", "CHANGE_STANDARD_DEFAULT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b2222222-2222-2222-2222-222222222205"), "Düşük riskli standart değişiklikler için kısa onay akışı.", true, "Standart Değişiklik Akışı", "Active", "ChangeSubmitted", "ChangeRequest", null }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "AssignedAt", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("2dc4da2c-a719-4ea4-fda5-c3d3da208c3f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("88448308-513f-6497-f557-c0b724bfbef6"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("31eb4ef5-3c1f-bf5b-ac13-4581760e0029"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("18c14756-50e3-7391-218e-04ce74204c2c"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("32e1c3cb-9ac6-e99c-d847-0d99fcf70bec"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("87ab24e6-ed75-904c-49ad-015649535f0d"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("54eb731b-17f0-d5f1-6081-6a089eb9dff8"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5d528578-f344-5ad5-106e-8830c77588d8"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("55ec936e-63d7-651d-1aa0-202ce5a58845"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8cd4504c-f307-b8e1-a146-aeebbc42c1ca"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("60b09336-25c5-9614-12a0-2e76bf893ce4"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e876a387-8528-144e-e79e-3e65a4b5f8a1"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("899fc721-424b-566b-efe3-a30941ee329a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e7b825a1-6db0-0559-6d41-40e922b7cf17"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("906f1c01-6cef-c499-3362-ddf69b3f2804"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("75c444a5-a5ca-bc76-74a2-4285230caabb"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("993fec3d-0bfe-473a-765d-6bdf7e950c3f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("bad559b9-ae81-21c2-0f14-558258239bc7"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("b919fe90-6b7f-04d6-69a2-27eba86aab5c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f6ce732d-1c74-9489-4faa-35778127f820"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("ba3f2de6-1697-69b7-3600-74ae2574a656"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("43cad4fb-7bdb-8afb-424c-98a8f7dbb4eb"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("c2575da3-e38b-e6bb-fa75-8837c1334a3e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("61258223-c927-82ba-1c3f-4567e2f1267b"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("ed24c546-7923-6298-69d4-301a85215440"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("fd7432dc-668e-4687-2a6c-9cccf7361ce0"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("f48b912a-ccf1-19aa-c58c-7e2650314586"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("38e49ddd-d6dd-6da1-d7c5-2f6f40a89a89"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("fc6ebea2-debb-2d72-eee8-5b917615e127"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("efd3ed09-db1f-2a2f-5a7f-5d3f19228ff5"), new Guid("a1111111-1111-1111-1111-111111111105") }
                });

            migrationBuilder.InsertData(
                table: "WorkflowVersions",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "Notes", "PublishedAt", "PublishedByUserId", "StartStepKey", "Status", "VersionNumber", "WorkflowDefinitionId" },
                values: new object[,]
                {
                    { new Guid("1f1bfe70-af19-76a2-f637-40d5fea1962b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b2222222-2222-2222-2222-222222222205"), "İlk yayınlanan sürüm (seed).", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b2222222-2222-2222-2222-222222222205"), "START", "Published", 1, new Guid("5d087898-4f7d-8654-6112-dfb54b45a76a") },
                    { new Guid("54a48a04-d65e-3705-1043-8d211f9c489a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b2222222-2222-2222-2222-222222222205"), "İlk yayınlanan sürüm (seed).", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b2222222-2222-2222-2222-222222222205"), "START", "Published", 1, new Guid("c497907e-05a2-2789-e49f-0c93816cc587") },
                    { new Guid("79014796-e57b-7b75-7a08-51c263f2e4fd"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b2222222-2222-2222-2222-222222222205"), "İlk yayınlanan sürüm (seed).", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b2222222-2222-2222-2222-222222222205"), "START", "Published", 1, new Guid("d85892c5-aaf7-0f2e-aa8b-546cc64795f2") }
                });

            migrationBuilder.InsertData(
                table: "WorkflowStepDefinitions",
                columns: new[] { "Id", "AssignedRole", "AssignedUserId", "Description", "DueInHours", "IsRequired", "Name", "NotificationRole", "NotificationTemplateCode", "StepKey", "StepOrder", "StepType", "WorkflowVersionId" },
                values: new object[,]
                {
                    { new Guid("0b3de375-7f0f-d1aa-eabd-8bc77762adcd"), null, null, null, null, true, "Başlangıç", null, null, "START", 1, "Start", new Guid("54a48a04-d65e-3705-1043-8d211f9c489a") },
                    { new Guid("1dfc4215-9264-78d0-b241-03447f23eb44"), null, null, null, null, true, "Risk Değerlendirmesi", null, null, "RISK", 4, "Condition", new Guid("54a48a04-d65e-3705-1043-8d211f9c489a") },
                    { new Guid("29dd5a82-08ae-68b1-1061-5624f0e6bf98"), "ReleaseManager", null, null, 48, true, "Yayın Yöneticisi Onayı", null, null, "RM", 3, "Approval", new Guid("1f1bfe70-af19-76a2-f637-40d5fea1962b") },
                    { new Guid("30360cc8-8273-1a29-4f0d-cb61c29f5d52"), "QA", null, null, 48, true, "Kalite (QA) Onayı", null, null, "QA", 3, "Approval", new Guid("54a48a04-d65e-3705-1043-8d211f9c489a") },
                    { new Guid("4315cf61-3725-9647-eb8f-894d0070a6d2"), "Architect", null, null, 48, true, "Mimari Onayı", null, null, "ARCH", 2, "Approval", new Guid("1f1bfe70-af19-76a2-f637-40d5fea1962b") },
                    { new Guid("85ddc5f3-e435-a11d-838c-98ddfe0368c4"), null, null, null, null, true, "Bitiş", null, null, "END", 6, "End", new Guid("54a48a04-d65e-3705-1043-8d211f9c489a") },
                    { new Guid("afa91b9f-35a4-b5c5-ee0b-0f8f2fc19a80"), "Architect", null, null, 48, true, "Mimari Onayı", null, null, "ARCH", 2, "Approval", new Guid("79014796-e57b-7b75-7a08-51c263f2e4fd") },
                    { new Guid("b1641268-4cd0-c4d6-37e3-8f7a3a23c0a6"), "Architect", null, null, 48, true, "Mimari Onayı", null, null, "ARCH", 2, "Approval", new Guid("54a48a04-d65e-3705-1043-8d211f9c489a") },
                    { new Guid("b73213fb-d7bd-9056-f8e3-e6f755e2e595"), "Admin", null, null, 48, true, "Admin Onayı", null, null, "ADMIN", 4, "Approval", new Guid("1f1bfe70-af19-76a2-f637-40d5fea1962b") },
                    { new Guid("c44c1a61-245e-418d-b2c5-94a015220495"), null, null, null, null, true, "Bitiş", null, null, "END", 5, "End", new Guid("1f1bfe70-af19-76a2-f637-40d5fea1962b") },
                    { new Guid("ca0c8829-0d58-d890-f290-c4276fb5b8a9"), null, null, null, null, true, "Başlangıç", null, null, "START", 1, "Start", new Guid("1f1bfe70-af19-76a2-f637-40d5fea1962b") },
                    { new Guid("e50cc572-f89d-d825-2054-44a1526cc53a"), null, null, null, null, true, "Bitiş", null, null, "END", 3, "End", new Guid("79014796-e57b-7b75-7a08-51c263f2e4fd") },
                    { new Guid("eaa6ea76-4bf1-b58e-a0e1-e3b80e8e4ee2"), null, null, null, null, true, "Başlangıç", null, null, "START", 1, "Start", new Guid("79014796-e57b-7b75-7a08-51c263f2e4fd") },
                    { new Guid("ff441dcb-13fb-b6d8-09ea-e9c97f4e4718"), "ReleaseManager", null, null, 48, true, "Yayın Yöneticisi Onayı", null, null, "RM", 5, "Approval", new Guid("54a48a04-d65e-3705-1043-8d211f9c489a") }
                });

            migrationBuilder.InsertData(
                table: "WorkflowTransitionDefinitions",
                columns: new[] { "Id", "ConditionField", "ConditionType", "Description", "ExpectedValue", "FromStepKey", "Operator", "Priority", "ToStepKey", "WorkflowVersionId" },
                values: new object[,]
                {
                    { new Guid("00c103b9-9315-20e0-b9ca-a36a14f4a3c1"), null, "Always", null, null, "ADMIN", null, 1, "END", new Guid("1f1bfe70-af19-76a2-f637-40d5fea1962b") },
                    { new Guid("1638ec71-41d6-f19e-bc64-ceb77d002034"), null, "Always", null, null, "RM", null, 1, "ADMIN", new Guid("1f1bfe70-af19-76a2-f637-40d5fea1962b") },
                    { new Guid("290c6ec4-eee6-e156-3559-b8371d2dd91c"), null, "Always", null, null, "ARCH", null, 1, "QA", new Guid("54a48a04-d65e-3705-1043-8d211f9c489a") },
                    { new Guid("293368ac-9034-2c9b-381e-cbd14208b7c0"), null, "Always", null, null, "START", null, 1, "ARCH", new Guid("79014796-e57b-7b75-7a08-51c263f2e4fd") },
                    { new Guid("36290054-14eb-16a7-6cd6-a7113a47943c"), null, "Always", null, null, "RM", null, 1, "END", new Guid("54a48a04-d65e-3705-1043-8d211f9c489a") },
                    { new Guid("3c7976c9-42b6-75f9-bc72-75054650086b"), "riskLevel", "RiskLevel", null, "High", "RISK", "Equals", 2, "RM", new Guid("54a48a04-d65e-3705-1043-8d211f9c489a") },
                    { new Guid("4382a511-cfbf-cc1f-74f6-1acbf679ecf0"), null, "Always", null, null, "ARCH", null, 1, "END", new Guid("79014796-e57b-7b75-7a08-51c263f2e4fd") },
                    { new Guid("47c1a616-27e2-8899-c394-1e97417f66ad"), "riskLevel", "RiskLevel", null, "Critical", "RISK", "Equals", 1, "RM", new Guid("54a48a04-d65e-3705-1043-8d211f9c489a") },
                    { new Guid("573a6b41-b87e-0a48-4d33-49f32c39b652"), null, "Always", null, null, "START", null, 1, "ARCH", new Guid("1f1bfe70-af19-76a2-f637-40d5fea1962b") },
                    { new Guid("64abee9a-0bcb-045c-c581-8f64d3baa78d"), null, "Always", null, null, "ARCH", null, 1, "RM", new Guid("1f1bfe70-af19-76a2-f637-40d5fea1962b") },
                    { new Guid("8899837f-1088-2a8c-0819-a44c10e17b59"), null, "Always", null, null, "START", null, 1, "ARCH", new Guid("54a48a04-d65e-3705-1043-8d211f9c489a") },
                    { new Guid("a698846f-2fd2-903f-8865-0051a53fec2d"), null, "Always", null, null, "QA", null, 1, "RISK", new Guid("54a48a04-d65e-3705-1043-8d211f9c489a") },
                    { new Guid("a8e85d8e-1e58-212d-c940-e80d37324a64"), null, "Always", null, null, "RISK", null, 3, "END", new Guid("54a48a04-d65e-3705-1043-8d211f9c489a") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_Category",
                table: "WorkflowDefinitions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_Code",
                table: "WorkflowDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_Status",
                table: "WorkflowDefinitions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_TriggerObjectType_ChangeClass",
                table: "WorkflowDefinitions",
                columns: new[] { "TriggerObjectType", "ChangeClass" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEvents_CreatedAt",
                table: "WorkflowEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEvents_WorkflowInstanceId",
                table: "WorkflowEvents",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_CreatedAt",
                table: "WorkflowInstances",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_InstanceNo",
                table: "WorkflowInstances",
                column: "InstanceNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_Status",
                table: "WorkflowInstances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_TriggerObjectType_TriggerObjectId",
                table: "WorkflowInstances",
                columns: new[] { "TriggerObjectType", "TriggerObjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_WorkflowDefinitionId",
                table: "WorkflowInstances",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_WorkflowVersionId",
                table: "WorkflowInstances",
                column: "WorkflowVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepDefinitions_WorkflowVersionId_StepKey",
                table: "WorkflowStepDefinitions",
                columns: new[] { "WorkflowVersionId", "StepKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepDefinitions_WorkflowVersionId_StepOrder",
                table: "WorkflowStepDefinitions",
                columns: new[] { "WorkflowVersionId", "StepOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepInstances_AssignedRole",
                table: "WorkflowStepInstances",
                column: "AssignedRole");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepInstances_AssignedUserId",
                table: "WorkflowStepInstances",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepInstances_WorkflowInstanceId",
                table: "WorkflowStepInstances",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepInstances_WorkflowInstanceId_Status",
                table: "WorkflowStepInstances",
                columns: new[] { "WorkflowInstanceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitionDefinitions_WorkflowVersionId",
                table: "WorkflowTransitionDefinitions",
                column: "WorkflowVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitionDefinitions_WorkflowVersionId_FromStepKey_Priority",
                table: "WorkflowTransitionDefinitions",
                columns: new[] { "WorkflowVersionId", "FromStepKey", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVersions_Status",
                table: "WorkflowVersions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVersions_WorkflowDefinitionId_VersionNumber",
                table: "WorkflowVersions",
                columns: new[] { "WorkflowDefinitionId", "VersionNumber" },
                unique: true);

            // Rebuild the unified audit view to include the WORKFLOW module (WorkflowEvents).
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_UnifiedAuditRecords;");
            migrationBuilder.Sql(UnifiedAuditViewWithWorkflow);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the previous (pre-workflow) unified audit view before dropping the tables.
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_UnifiedAuditRecords;");
            migrationBuilder.Sql(UnifiedAuditViewWithoutWorkflow);

            migrationBuilder.DropTable(
                name: "WorkflowEvents");

            migrationBuilder.DropTable(
                name: "WorkflowStepDefinitions");

            migrationBuilder.DropTable(
                name: "WorkflowStepInstances");

            migrationBuilder.DropTable(
                name: "WorkflowTransitionDefinitions");

            migrationBuilder.DropTable(
                name: "WorkflowInstances");

            migrationBuilder.DropTable(
                name: "WorkflowVersions");

            migrationBuilder.DropTable(
                name: "WorkflowDefinitions");

            migrationBuilder.DeleteData(
                table: "NotificationTemplates",
                keyColumn: "Id",
                keyValue: new Guid("0ed3ca1e-3763-e185-b07f-8f20aa3b5a95"));

            migrationBuilder.DeleteData(
                table: "NotificationTemplates",
                keyColumn: "Id",
                keyValue: new Guid("3604fe71-7d66-08e6-6c17-864b6e77520f"));

            migrationBuilder.DeleteData(
                table: "NotificationTemplates",
                keyColumn: "Id",
                keyValue: new Guid("455f0664-1270-d875-451b-0bfeccd8af79"));

            migrationBuilder.DeleteData(
                table: "NotificationTemplates",
                keyColumn: "Id",
                keyValue: new Guid("8bdce82a-4fd1-6297-f654-54d8f7f761f5"));

            migrationBuilder.DeleteData(
                table: "NotificationTemplates",
                keyColumn: "Id",
                keyValue: new Guid("8e2d8852-d9c7-bc04-fe76-d520e5fffed6"));

            migrationBuilder.DeleteData(
                table: "NotificationTemplates",
                keyColumn: "Id",
                keyValue: new Guid("cc4aa076-f556-96e1-cfd8-17aebed6ec0a"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("2dc4da2c-a719-4ea4-fda5-c3d3da208c3f"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("31eb4ef5-3c1f-bf5b-ac13-4581760e0029"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("32e1c3cb-9ac6-e99c-d847-0d99fcf70bec"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("54eb731b-17f0-d5f1-6081-6a089eb9dff8"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("55ec936e-63d7-651d-1aa0-202ce5a58845"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("60b09336-25c5-9614-12a0-2e76bf893ce4"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("899fc721-424b-566b-efe3-a30941ee329a"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("906f1c01-6cef-c499-3362-ddf69b3f2804"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("993fec3d-0bfe-473a-765d-6bdf7e950c3f"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("b919fe90-6b7f-04d6-69a2-27eba86aab5c"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("ba3f2de6-1697-69b7-3600-74ae2574a656"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("c2575da3-e38b-e6bb-fa75-8837c1334a3e"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("ed24c546-7923-6298-69d4-301a85215440"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("f48b912a-ccf1-19aa-c58c-7e2650314586"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("fc6ebea2-debb-2d72-eee8-5b917615e127"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("18c14756-50e3-7391-218e-04ce74204c2c"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("38e49ddd-d6dd-6da1-d7c5-2f6f40a89a89"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("43cad4fb-7bdb-8afb-424c-98a8f7dbb4eb"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("5d528578-f344-5ad5-106e-8830c77588d8"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("61258223-c927-82ba-1c3f-4567e2f1267b"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("75c444a5-a5ca-bc76-74a2-4285230caabb"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("87ab24e6-ed75-904c-49ad-015649535f0d"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("88448308-513f-6497-f557-c0b724bfbef6"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("8cd4504c-f307-b8e1-a146-aeebbc42c1ca"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("bad559b9-ae81-21c2-0f14-558258239bc7"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("e7b825a1-6db0-0559-6d41-40e922b7cf17"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("e876a387-8528-144e-e79e-3e65a4b5f8a1"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("efd3ed09-db1f-2a2f-5a7f-5d3f19228ff5"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("f6ce732d-1c74-9489-4faa-35778127f820"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("fd7432dc-668e-4687-2a6c-9cccf7361ce0"));
        }
    }
}
