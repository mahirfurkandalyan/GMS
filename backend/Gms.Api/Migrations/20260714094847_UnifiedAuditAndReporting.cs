using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gms.Api.Migrations
{
    /// <inheritdoc />
    public partial class UnifiedAuditAndReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalAuditEvents_ApprovalRequests_ApprovalRequestId",
                table: "ApprovalAuditEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_ChangeAuditEvents_ChangeRequests_ChangeRequestId",
                table: "ChangeAuditEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentEvents_DeploymentRuns_DeploymentRunId",
                table: "DeploymentEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentAuditEvents_Documents_DocumentId",
                table: "DocumentAuditEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationEvents_Notifications_NotificationId",
                table: "NotificationEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_ReleaseAuditEvents_ReleasePlans_ReleasePlanId",
                table: "ReleaseAuditEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_ValidationEvents_ValidationRuns_ValidationRunId",
                table: "ValidationEvents");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a71b71a5-d43e-dbca-8091-22f56c01b7b9"),
                column: "Description",
                value: "Birleşik denetim kayıtlarını görüntüleme.");

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { new Guid("16f92817-3826-4e5a-a8de-f2f077693de1"), "audit.export", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Denetim kayıtlarını CSV dışa aktarma.", "AUDIT", "Denetim dışa aktarma" },
                    { new Guid("18af58f6-bfe9-9675-93d2-bf2155d0b8f4"), "report.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Raporları ve metrikleri görüntüleme.", "REPORT", "Rapor görüntüleme" },
                    { new Guid("36e6e982-7328-0cce-1f0d-dc0e68f45b1c"), "report.manage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Rapor tanımlarını yönetme.", "REPORT", "Rapor yönetimi" },
                    { new Guid("3a87b815-2aa3-5208-0fa9-499771b8f8f9"), "audit.security.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Güvenlik denetim kayıtlarını görüntüleme.", "AUDIT", "Güvenlik denetimi" },
                    { new Guid("f4f88ffb-b6c7-9ecd-3cf8-905e2e5d00bf"), "report.export", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Rapor veri kümelerini CSV dışa aktarma.", "REPORT", "Rapor dışa aktarma" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "AssignedAt", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("7959a86d-a5ad-286b-92f5-126dbdea4111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a71b71a5-d43e-dbca-8091-22f56c01b7b9"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("05235e5f-abfa-fb1f-3857-9a3b48375f56"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("36e6e982-7328-0cce-1f0d-dc0e68f45b1c"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("2055b2b8-3c8f-93c0-cf10-e7dd9d56289e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("18af58f6-bfe9-9675-93d2-bf2155d0b8f4"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("30b10eaf-96de-06d1-94a6-5ffb8178956d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f4f88ffb-b6c7-9ecd-3cf8-905e2e5d00bf"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("42356574-4d05-e170-f394-8989d3f9b85c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("16f92817-3826-4e5a-a8de-f2f077693de1"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("4d2b8d12-76a1-9431-6227-c38bfe692c38"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("18af58f6-bfe9-9675-93d2-bf2155d0b8f4"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("5ec36ea0-da0e-7d21-02df-a4d5efeb08d1"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("3a87b815-2aa3-5208-0fa9-499771b8f8f9"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("676cf064-00a8-bda1-66e2-6e6fe3079fb3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("18af58f6-bfe9-9675-93d2-bf2155d0b8f4"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("763fecd6-f969-5ca6-d861-16972d7decc6"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("3a87b815-2aa3-5208-0fa9-499771b8f8f9"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("8ed2b400-72f5-67e1-ffdb-ca43e2337305"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("18af58f6-bfe9-9675-93d2-bf2155d0b8f4"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("941c4859-7183-5e01-61a7-3e9b8bf413ad"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f4f88ffb-b6c7-9ecd-3cf8-905e2e5d00bf"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("a994c312-96eb-d5d7-c5cb-761e5da35e8f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("18af58f6-bfe9-9675-93d2-bf2155d0b8f4"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("e95187e8-3466-094c-f536-8c80931901eb"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("16f92817-3826-4e5a-a8de-f2f077693de1"), new Guid("a1111111-1111-1111-1111-111111111105") }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalAuditEvents_ApprovalRequests_ApprovalRequestId",
                table: "ApprovalAuditEvents",
                column: "ApprovalRequestId",
                principalTable: "ApprovalRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeAuditEvents_ChangeRequests_ChangeRequestId",
                table: "ChangeAuditEvents",
                column: "ChangeRequestId",
                principalTable: "ChangeRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentEvents_DeploymentRuns_DeploymentRunId",
                table: "DeploymentEvents",
                column: "DeploymentRunId",
                principalTable: "DeploymentRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentAuditEvents_Documents_DocumentId",
                table: "DocumentAuditEvents",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationEvents_Notifications_NotificationId",
                table: "NotificationEvents",
                column: "NotificationId",
                principalTable: "Notifications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReleaseAuditEvents_ReleasePlans_ReleasePlanId",
                table: "ReleaseAuditEvents",
                column: "ReleasePlanId",
                principalTable: "ReleasePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ValidationEvents_ValidationRuns_ValidationRunId",
                table: "ValidationEvents",
                column: "ValidationRunId",
                principalTable: "ValidationRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Unified audit read view: UNION ALL over the domain audit tables (read-only).
            migrationBuilder.Sql(@"
CREATE VIEW vw_UnifiedAuditRecords AS
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
FROM NotificationEvents e INNER JOIN Notifications n ON n.Id = e.NotificationId;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_UnifiedAuditRecords;");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalAuditEvents_ApprovalRequests_ApprovalRequestId",
                table: "ApprovalAuditEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_ChangeAuditEvents_ChangeRequests_ChangeRequestId",
                table: "ChangeAuditEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentEvents_DeploymentRuns_DeploymentRunId",
                table: "DeploymentEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentAuditEvents_Documents_DocumentId",
                table: "DocumentAuditEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationEvents_Notifications_NotificationId",
                table: "NotificationEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_ReleaseAuditEvents_ReleasePlans_ReleasePlanId",
                table: "ReleaseAuditEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_ValidationEvents_ValidationRuns_ValidationRunId",
                table: "ValidationEvents");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("05235e5f-abfa-fb1f-3857-9a3b48375f56"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("2055b2b8-3c8f-93c0-cf10-e7dd9d56289e"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("30b10eaf-96de-06d1-94a6-5ffb8178956d"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("42356574-4d05-e170-f394-8989d3f9b85c"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("4d2b8d12-76a1-9431-6227-c38bfe692c38"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("5ec36ea0-da0e-7d21-02df-a4d5efeb08d1"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("676cf064-00a8-bda1-66e2-6e6fe3079fb3"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("763fecd6-f969-5ca6-d861-16972d7decc6"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("7959a86d-a5ad-286b-92f5-126dbdea4111"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("8ed2b400-72f5-67e1-ffdb-ca43e2337305"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("941c4859-7183-5e01-61a7-3e9b8bf413ad"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("a994c312-96eb-d5d7-c5cb-761e5da35e8f"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("e95187e8-3466-094c-f536-8c80931901eb"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("16f92817-3826-4e5a-a8de-f2f077693de1"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("18af58f6-bfe9-9675-93d2-bf2155d0b8f4"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("36e6e982-7328-0cce-1f0d-dc0e68f45b1c"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("3a87b815-2aa3-5208-0fa9-499771b8f8f9"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("f4f88ffb-b6c7-9ecd-3cf8-905e2e5d00bf"));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a71b71a5-d43e-dbca-8091-22f56c01b7b9"),
                column: "Description",
                value: "Denetim kayıtlarını görüntüleme.");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalAuditEvents_ApprovalRequests_ApprovalRequestId",
                table: "ApprovalAuditEvents",
                column: "ApprovalRequestId",
                principalTable: "ApprovalRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeAuditEvents_ChangeRequests_ChangeRequestId",
                table: "ChangeAuditEvents",
                column: "ChangeRequestId",
                principalTable: "ChangeRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentEvents_DeploymentRuns_DeploymentRunId",
                table: "DeploymentEvents",
                column: "DeploymentRunId",
                principalTable: "DeploymentRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentAuditEvents_Documents_DocumentId",
                table: "DocumentAuditEvents",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationEvents_Notifications_NotificationId",
                table: "NotificationEvents",
                column: "NotificationId",
                principalTable: "Notifications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReleaseAuditEvents_ReleasePlans_ReleasePlanId",
                table: "ReleaseAuditEvents",
                column: "ReleasePlanId",
                principalTable: "ReleasePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ValidationEvents_ValidationRuns_ValidationRunId",
                table: "ValidationEvents",
                column: "ValidationRunId",
                principalTable: "ValidationRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
