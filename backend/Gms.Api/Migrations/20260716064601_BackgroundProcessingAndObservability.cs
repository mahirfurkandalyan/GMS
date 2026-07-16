using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gms.Api.Migrations
{
    /// <inheritdoc />
    public partial class BackgroundProcessingAndObservability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueSoonNotifiedAt",
                table: "WorkflowStepInstances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OverdueNotifiedAt",
                table: "WorkflowStepInstances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "NotificationDeliveries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptAt",
                table: "NotificationDeliveries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockedBy",
                table: "NotificationDeliveries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "NotificationDeliveries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAt",
                table: "NotificationDeliveries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "NotificationDeliveries",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "LockedBy",
                table: "IntegrationExecutions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "IntegrationExecutions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAt",
                table: "IntegrationExecutions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkerHeartbeats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkerName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSucceededAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastFailedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerHeartbeats", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { new Guid("5b4c14e2-2131-ce12-d645-8976d3bc901f"), "operations.manage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kontrollü teşhis: worker'ı elle bir kez çalıştırma.", "OPERATIONS", "Operasyon yönetimi" },
                    { new Guid("965c4de8-de4d-b63c-ad25-4e0cdd44f2da"), "operations.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Arka plan işleme birikimlerini ve worker durumunu görüntüleme.", "OPERATIONS", "Operasyon durumu görüntüleme" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "AssignedAt", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("1d0cae16-303b-a926-d5f0-3e6764f4d33b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("965c4de8-de4d-b63c-ad25-4e0cdd44f2da"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("d8377375-f290-7b34-92b3-1fc81f322f12"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("965c4de8-de4d-b63c-ad25-4e0cdd44f2da"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("e3f022c0-2e92-1b2e-bb6f-28a795a64c2d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5b4c14e2-2131-ce12-d645-8976d3bc901f"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("fd84458d-a7c9-58b5-125e-5adc9d8f6213"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("965c4de8-de4d-b63c-ad25-4e0cdd44f2da"), new Guid("a1111111-1111-1111-1111-111111111108") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepInstances_Status_DueAt",
                table: "WorkflowStepInstances",
                columns: new[] { "Status", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_Channel_Status",
                table: "NotificationDeliveries",
                columns: new[] { "Channel", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_NextAttemptAt",
                table: "NotificationDeliveries",
                column: "NextAttemptAt");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationExecutions_LockedUntil",
                table: "IntegrationExecutions",
                column: "LockedUntil");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationExecutions_Status_NextAttemptAt",
                table: "IntegrationExecutions",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerHeartbeats_WorkerName_InstanceId",
                table: "WorkerHeartbeats",
                columns: new[] { "WorkerName", "InstanceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkerHeartbeats");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowStepInstances_Status_DueAt",
                table: "WorkflowStepInstances");

            migrationBuilder.DropIndex(
                name: "IX_NotificationDeliveries_Channel_Status",
                table: "NotificationDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_NotificationDeliveries_NextAttemptAt",
                table: "NotificationDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationExecutions_LockedUntil",
                table: "IntegrationExecutions");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationExecutions_Status_NextAttemptAt",
                table: "IntegrationExecutions");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("1d0cae16-303b-a926-d5f0-3e6764f4d33b"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("d8377375-f290-7b34-92b3-1fc81f322f12"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("e3f022c0-2e92-1b2e-bb6f-28a795a64c2d"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("fd84458d-a7c9-58b5-125e-5adc9d8f6213"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("5b4c14e2-2131-ce12-d645-8976d3bc901f"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("965c4de8-de4d-b63c-ad25-4e0cdd44f2da"));

            migrationBuilder.DropColumn(
                name: "DueSoonNotifiedAt",
                table: "WorkflowStepInstances");

            migrationBuilder.DropColumn(
                name: "OverdueNotifiedAt",
                table: "WorkflowStepInstances");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "LastAttemptAt",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "LockedBy",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "LockedBy",
                table: "IntegrationExecutions");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "IntegrationExecutions");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "IntegrationExecutions");
        }
    }
}
