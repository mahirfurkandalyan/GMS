using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gms.Api.Migrations
{
    /// <inheritdoc />
    public partial class IntegrationHubDomain : Migration
    {
        // The nine existing UNIONs (Change..Notification..Workflow) shared by both view variants.
        private const string UnifiedAuditWithWorkflowUnions = @"
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
FROM NotificationEvents e INNER JOIN Notifications n ON n.Id = e.NotificationId
UNION ALL
SELECT e.Id, CAST('WORKFLOW' AS nvarchar(20)), CAST('WorkflowEvents' AS nvarchar(50)),
       CAST(e.EventType AS nvarchar(50)), CAST(e.Description AS nvarchar(1000)), e.ActorUserId,
       CAST('WorkflowInstance' AS nvarchar(40)), e.WorkflowInstanceId, CAST(wi.InstanceNo AS nvarchar(50)),
       wi.RelatedProjectId, wi.RelatedEnvironmentId, CAST(NULL AS nvarchar(20)), CAST(NULL AS nvarchar(64)), e.CreatedAt
FROM WorkflowEvents e INNER JOIN WorkflowInstances wi ON wi.Id = e.WorkflowInstanceId";

        // The INTEGRATION UNION added by this migration.
        private const string UnifiedAuditIntegrationUnion = @"
UNION ALL
SELECT e.Id, CAST('INTEGRATION' AS nvarchar(20)), CAST('IntegrationEvents' AS nvarchar(50)),
       CAST(e.EventType AS nvarchar(50)), CAST(e.Description AS nvarchar(1000)), e.ActorUserId,
       CAST(CASE WHEN e.IntegrationExecutionId IS NULL THEN 'IntegrationDefinition' ELSE 'IntegrationExecution' END AS nvarchar(40)),
       COALESCE(e.IntegrationExecutionId, e.IntegrationDefinitionId),
       CAST(COALESCE(x.ExecutionNo, d.IntegrationNo) AS nvarchar(50)),
       CAST(NULL AS uniqueidentifier), CAST(NULL AS uniqueidentifier),
       CAST(CASE WHEN x.Status IN ('Succeeded') THEN 'Succeeded' WHEN x.Status IN ('Failed','DeadLetter') THEN 'Failed' ELSE NULL END AS nvarchar(20)),
       CAST(NULL AS nvarchar(64)), e.CreatedAt
FROM IntegrationEvents e
     INNER JOIN IntegrationDefinitions d ON d.Id = e.IntegrationDefinitionId
     LEFT JOIN IntegrationExecutions x ON x.Id = e.IntegrationExecutionId";

        private static readonly string ViewWithoutIntegration =
            "CREATE VIEW vw_UnifiedAuditRecords AS" + UnifiedAuditWithWorkflowUnions + ";";
        private static readonly string ViewWithIntegration =
            "CREATE VIEW vw_UnifiedAuditRecords AS" + UnifiedAuditWithWorkflowUnions + UnifiedAuditIntegrationUnion + ";";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntegrationDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AuthenticationType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSuccessfulConnectionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastFailedConnectionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalObjectLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InternalObjectType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InternalObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalObjectType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExternalObjectId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ExternalObjectKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ExternalUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalObjectLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalObjectLinks_IntegrationDefinitions_IntegrationDefinitionId",
                        column: x => x.IntegrationDefinitionId,
                        principalTable: "IntegrationDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CredentialType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    KeyName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EncryptedValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    MaskedValue = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RotatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationCredentials_IntegrationDefinitions_IntegrationDefinitionId",
                        column: x => x.IntegrationDefinitionId,
                        principalTable: "IntegrationDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationEndpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationEndpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationEndpoints_IntegrationDefinitions_IntegrationDefinitionId",
                        column: x => x.IntegrationDefinitionId,
                        principalTable: "IntegrationDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IntegrationDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ObjectType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResponseSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationExecutions_IntegrationDefinitions_IntegrationDefinitionId",
                        column: x => x.IntegrationDefinitionId,
                        principalTable: "IntegrationDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ObjectType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TargetEndpointId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationSubscriptions_IntegrationDefinitions_IntegrationDefinitionId",
                        column: x => x.IntegrationDefinitionId,
                        principalTable: "IntegrationDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IntegrationDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationEvents_IntegrationDefinitions_IntegrationDefinitionId",
                        column: x => x.IntegrationDefinitionId,
                        principalTable: "IntegrationDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationEvents_IntegrationExecutions_IntegrationExecutionId",
                        column: x => x.IntegrationExecutionId,
                        principalTable: "IntegrationExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationExecutionAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNo = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DurationMilliseconds = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationExecutionAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationExecutionAttempts_IntegrationExecutions_IntegrationExecutionId",
                        column: x => x.IntegrationExecutionId,
                        principalTable: "IntegrationExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "NotificationTemplates",
                columns: new[] { "Id", "BodyTemplate", "Code", "CreatedAt", "IsSystem", "Module", "Name", "SubjectTemplate" },
                values: new object[,]
                {
                    { new Guid("00836baf-48a3-2493-6cfc-12cf8ff9c56a"), "{{IntegrationName}} entegrasyonunun '{{KeyName}}' kimlik bilgisi döndürüldü.", "IntegrationCredentialRotated", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "INTEGRATION", "Kimlik bilgisi döndürüldü", "Kimlik bilgisi döndürüldü: {{IntegrationNo}}" },
                    { new Guid("8405a5f7-df47-cd02-b547-6b2881bf8530"), "{{IntegrationNo}} ({{IntegrationName}}) entegrasyonunun bağlantı testi başarısız oldu: {{Error}}", "IntegrationConnectionFailed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "INTEGRATION", "Entegrasyon bağlantısı başarısız", "Bağlantı başarısız: {{IntegrationNo}}" },
                    { new Guid("d179baa5-4731-d5c5-716e-e3c71eee93ce"), "{{ExecutionNo}} yürütmesi ({{IntegrationName}}) başarısız oldu: {{Error}}", "IntegrationExecutionFailed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "INTEGRATION", "Entegrasyon yürütmesi başarısız", "Yürütme başarısız: {{ExecutionNo}}" },
                    { new Guid("d19d771b-bbc6-d36b-efb8-eade550ecd8c"), "{{IntegrationName}} entegrasyonuna gelen bir webhook reddedildi: {{Reason}}", "IncomingWebhookRejected", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "INTEGRATION", "Gelen webhook reddedildi", "Webhook reddedildi: {{IntegrationNo}}" },
                    { new Guid("e0683217-3ff1-0a7b-c738-a43a23b70efe"), "{{ExecutionNo}} yürütmesi ({{IntegrationName}}) azami deneme sonrası ölü mektup kutusuna alındı.", "IntegrationDeadLettered", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "INTEGRATION", "Entegrasyon ölü mektup kutusuna alındı", "Ölü mektup: {{ExecutionNo}}" }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { new Guid("143aa5d4-f471-752b-c4bf-ac7a381f21f9"), "integration.update", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Entegrasyon tanımı/uç nokta/abonelik güncelleme.", "INTEGRATION", "Entegrasyon güncelleme" },
                    { new Guid("1c091ec7-23b5-c474-e597-395203cff9cb"), "integration.activate", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Entegrasyonu aktif/pasif yapma ve bağlantı testi.", "INTEGRATION", "Entegrasyon aktifleştirme" },
                    { new Guid("1f1a0e77-a9f0-8c06-12db-356bb7945aef"), "integration.subscription.manage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Olay aboneliklerini yönetme.", "INTEGRATION", "Abonelik yönetimi" },
                    { new Guid("39ce4609-83a1-4720-078d-7e92de0710ac"), "integration.link.manage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dış nesne bağlarını oluşturma/kaldırma.", "INTEGRATION", "Dış nesne bağı yönetimi" },
                    { new Guid("5a7ea0a4-6111-3373-66e2-1419986c3a89"), "integration.create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yeni entegrasyon tanımı oluşturma.", "INTEGRATION", "Entegrasyon oluşturma" },
                    { new Guid("5ed189aa-8268-72e8-d20e-8ce16d5aeeed"), "integration.retry", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Başarısız yürütmeyi yeniden deneme.", "INTEGRATION", "Yürütme yeniden deneme" },
                    { new Guid("8b70fd13-3fdf-abc4-0925-d2da8b847527"), "integration.cancel", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bekleyen/başarısız yürütmeyi iptal etme.", "INTEGRATION", "Yürütme iptali" },
                    { new Guid("95a0e569-ab03-5bd2-9b55-4bdbe95b64b0"), "integration.credential.manage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Şifreli kimlik bilgisi ekleme/döndürme/silme (hassas).", "INTEGRATION", "Kimlik bilgisi yönetimi" },
                    { new Guid("b03f374d-59da-569c-2402-9bc33d5d5177"), "integration.execute", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Giden çağrı yürütme / bekleyenleri gönderme.", "INTEGRATION", "Entegrasyon yürütme" },
                    { new Guid("d1486ec1-956b-4d1d-9510-f5aea1e16b21"), "integration.endpoint.manage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Entegrasyon uç noktalarını yönetme.", "INTEGRATION", "Uç nokta yönetimi" },
                    { new Guid("d53a2e29-4b08-4939-28f4-bdbff90fd0ce"), "integration.audit.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Entegrasyon denetim kayıtlarını görüntüleme.", "INTEGRATION", "Entegrasyon denetimi" },
                    { new Guid("e95e572d-953a-6471-8f7e-93d327388def"), "integration.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Entegrasyon tanımlarını ve yürütmelerini görüntüleme.", "INTEGRATION", "Entegrasyon görüntüleme" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "AssignedAt", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("12e4ae8b-58f8-45cb-5867-03c94e621bef"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5ed189aa-8268-72e8-d20e-8ce16d5aeeed"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("199eca70-a921-8610-859a-bbdbd702b614"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e95e572d-953a-6471-8f7e-93d327388def"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("27bc0358-7a07-94d2-05a6-d9cc23239882"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e95e572d-953a-6471-8f7e-93d327388def"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("33188462-7d73-4da6-5a8b-a5a542fee770"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("1f1a0e77-a9f0-8c06-12db-356bb7945aef"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("4271a79e-6661-8e5e-1fbe-695caaa788be"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e95e572d-953a-6471-8f7e-93d327388def"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("4f5e142f-03a5-3cc2-a17e-03033a5494c8"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d1486ec1-956b-4d1d-9510-f5aea1e16b21"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("592a18c8-7b9c-7395-b62e-662ca2fb521d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8b70fd13-3fdf-abc4-0925-d2da8b847527"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("64f66f5f-1093-8b4c-dc15-885d16ef9c84"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5a7ea0a4-6111-3373-66e2-1419986c3a89"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("68a91c49-db5e-f4ec-775f-acfa9de92b98"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b03f374d-59da-569c-2402-9bc33d5d5177"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("8870559f-55e2-618f-0213-f91a482ac20e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("95a0e569-ab03-5bd2-9b55-4bdbe95b64b0"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("959e59c9-16cb-e63c-c9aa-058dcadd7fa0"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d53a2e29-4b08-4939-28f4-bdbff90fd0ce"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("aa2d51a7-8664-c3d2-eaaa-65ced6480e7d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("39ce4609-83a1-4720-078d-7e92de0710ac"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("b82ac7f0-1c31-c847-c940-84c56dd050cd"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d53a2e29-4b08-4939-28f4-bdbff90fd0ce"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("cc9c4bc5-16b7-bb2b-9b89-d1afd72a6921"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("143aa5d4-f471-752b-c4bf-ac7a381f21f9"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("d0562445-a8c2-ea28-0d44-cbc517043834"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e95e572d-953a-6471-8f7e-93d327388def"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("d61c6b1f-88dc-c2d9-bf49-bca66e584d53"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("39ce4609-83a1-4720-078d-7e92de0710ac"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("fb379e12-8b36-0510-e518-58943c13d419"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("1c091ec7-23b5-c474-e597-395203cff9cb"), new Guid("a1111111-1111-1111-1111-111111111105") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalObjectLinks_ExternalObjectType_ExternalObjectId",
                table: "ExternalObjectLinks",
                columns: new[] { "ExternalObjectType", "ExternalObjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalObjectLinks_IntegrationDefinitionId_InternalObjectType_InternalObjectId_ExternalObjectType_ExternalObjectId",
                table: "ExternalObjectLinks",
                columns: new[] { "IntegrationDefinitionId", "InternalObjectType", "InternalObjectId", "ExternalObjectType", "ExternalObjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalObjectLinks_InternalObjectType_InternalObjectId",
                table: "ExternalObjectLinks",
                columns: new[] { "InternalObjectType", "InternalObjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationCredentials_IntegrationDefinitionId",
                table: "IntegrationCredentials",
                column: "IntegrationDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationCredentials_IntegrationDefinitionId_KeyName",
                table: "IntegrationCredentials",
                columns: new[] { "IntegrationDefinitionId", "KeyName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDefinitions_Category",
                table: "IntegrationDefinitions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDefinitions_Code",
                table: "IntegrationDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDefinitions_IntegrationNo",
                table: "IntegrationDefinitions",
                column: "IntegrationNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDefinitions_Provider",
                table: "IntegrationDefinitions",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDefinitions_Status",
                table: "IntegrationDefinitions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationEndpoints_Direction",
                table: "IntegrationEndpoints",
                column: "Direction");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationEndpoints_IntegrationDefinitionId",
                table: "IntegrationEndpoints",
                column: "IntegrationDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationEndpoints_IsActive",
                table: "IntegrationEndpoints",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationEvents_IntegrationDefinitionId_CreatedAt",
                table: "IntegrationEvents",
                columns: new[] { "IntegrationDefinitionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationEvents_IntegrationExecutionId_CreatedAt",
                table: "IntegrationEvents",
                columns: new[] { "IntegrationExecutionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationExecutionAttempts_CreatedAt",
                table: "IntegrationExecutionAttempts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationExecutionAttempts_IntegrationExecutionId_AttemptNo",
                table: "IntegrationExecutionAttempts",
                columns: new[] { "IntegrationExecutionId", "AttemptNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationExecutionAttempts_Status",
                table: "IntegrationExecutionAttempts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationExecutions_CorrelationId",
                table: "IntegrationExecutions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationExecutions_CreatedAt",
                table: "IntegrationExecutions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationExecutions_ExecutionNo",
                table: "IntegrationExecutions",
                column: "ExecutionNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationExecutions_IntegrationDefinitionId",
                table: "IntegrationExecutions",
                column: "IntegrationDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationExecutions_ObjectType_ObjectId",
                table: "IntegrationExecutions",
                columns: new[] { "ObjectType", "ObjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationExecutions_Status",
                table: "IntegrationExecutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationSubscriptions_EventType",
                table: "IntegrationSubscriptions",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationSubscriptions_IntegrationDefinitionId_IsActive",
                table: "IntegrationSubscriptions",
                columns: new[] { "IntegrationDefinitionId", "IsActive" });

            // Rebuild the unified audit view to include the INTEGRATION module (IntegrationEvents).
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_UnifiedAuditRecords;");
            migrationBuilder.Sql(ViewWithIntegration);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the previous (pre-integration) unified audit view before dropping the tables.
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_UnifiedAuditRecords;");
            migrationBuilder.Sql(ViewWithoutIntegration);

            migrationBuilder.DropTable(
                name: "ExternalObjectLinks");

            migrationBuilder.DropTable(
                name: "IntegrationCredentials");

            migrationBuilder.DropTable(
                name: "IntegrationEndpoints");

            migrationBuilder.DropTable(
                name: "IntegrationEvents");

            migrationBuilder.DropTable(
                name: "IntegrationExecutionAttempts");

            migrationBuilder.DropTable(
                name: "IntegrationSubscriptions");

            migrationBuilder.DropTable(
                name: "IntegrationExecutions");

            migrationBuilder.DropTable(
                name: "IntegrationDefinitions");

            migrationBuilder.DeleteData(
                table: "NotificationTemplates",
                keyColumn: "Id",
                keyValue: new Guid("00836baf-48a3-2493-6cfc-12cf8ff9c56a"));

            migrationBuilder.DeleteData(
                table: "NotificationTemplates",
                keyColumn: "Id",
                keyValue: new Guid("8405a5f7-df47-cd02-b547-6b2881bf8530"));

            migrationBuilder.DeleteData(
                table: "NotificationTemplates",
                keyColumn: "Id",
                keyValue: new Guid("d179baa5-4731-d5c5-716e-e3c71eee93ce"));

            migrationBuilder.DeleteData(
                table: "NotificationTemplates",
                keyColumn: "Id",
                keyValue: new Guid("d19d771b-bbc6-d36b-efb8-eade550ecd8c"));

            migrationBuilder.DeleteData(
                table: "NotificationTemplates",
                keyColumn: "Id",
                keyValue: new Guid("e0683217-3ff1-0a7b-c738-a43a23b70efe"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("12e4ae8b-58f8-45cb-5867-03c94e621bef"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("199eca70-a921-8610-859a-bbdbd702b614"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("27bc0358-7a07-94d2-05a6-d9cc23239882"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("33188462-7d73-4da6-5a8b-a5a542fee770"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("4271a79e-6661-8e5e-1fbe-695caaa788be"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("4f5e142f-03a5-3cc2-a17e-03033a5494c8"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("592a18c8-7b9c-7395-b62e-662ca2fb521d"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("64f66f5f-1093-8b4c-dc15-885d16ef9c84"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("68a91c49-db5e-f4ec-775f-acfa9de92b98"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("8870559f-55e2-618f-0213-f91a482ac20e"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("959e59c9-16cb-e63c-c9aa-058dcadd7fa0"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("aa2d51a7-8664-c3d2-eaaa-65ced6480e7d"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("b82ac7f0-1c31-c847-c940-84c56dd050cd"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("cc9c4bc5-16b7-bb2b-9b89-d1afd72a6921"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("d0562445-a8c2-ea28-0d44-cbc517043834"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("d61c6b1f-88dc-c2d9-bf49-bca66e584d53"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("fb379e12-8b36-0510-e518-58943c13d419"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("143aa5d4-f471-752b-c4bf-ac7a381f21f9"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("1c091ec7-23b5-c474-e597-395203cff9cb"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("1f1a0e77-a9f0-8c06-12db-356bb7945aef"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("39ce4609-83a1-4720-078d-7e92de0710ac"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("5a7ea0a4-6111-3373-66e2-1419986c3a89"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("5ed189aa-8268-72e8-d20e-8ce16d5aeeed"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("8b70fd13-3fdf-abc4-0925-d2da8b847527"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("95a0e569-ab03-5bd2-9b55-4bdbe95b64b0"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("b03f374d-59da-569c-2402-9bc33d5d5177"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("d1486ec1-956b-4d1d-9510-f5aea1e16b21"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("d53a2e29-4b08-4939-28f4-bdbff90fd0ce"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("e95e572d-953a-6471-8f7e-93d327388def"));
        }
    }
}
