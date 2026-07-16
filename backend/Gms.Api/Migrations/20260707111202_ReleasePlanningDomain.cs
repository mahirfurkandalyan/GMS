using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gms.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReleasePlanningDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Releases");

            migrationBuilder.CreateTable(
                name: "ReleasePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReleaseNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReleaseType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RiskScore = table.Column<int>(type: "int", nullable: false),
                    TotalEstimatedMinutes = table.Column<int>(type: "int", nullable: false),
                    PlannedDeploymentStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedDeploymentEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RollbackWindow = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    BusinessOwner = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TechnicalOwner = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ReleaseManagerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleasePlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleasePlans_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReleasePlans_Environments_EnvironmentId",
                        column: x => x.EnvironmentId,
                        principalTable: "Environments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReleasePlans_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReleasePlans_Users_ReleaseManagerUserId",
                        column: x => x.ReleaseManagerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReleasePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseAuditEvents_ReleasePlans_ReleasePlanId",
                        column: x => x.ReleasePlanId,
                        principalTable: "ReleasePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReleaseAuditEvents_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseDeploymentPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReleasePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeploymentStrategy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CommunicationPlan = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RollbackStrategy = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DowntimeExpected = table.Column<bool>(type: "bit", nullable: false),
                    EstimatedDowntimeMinutes = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseDeploymentPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseDeploymentPlans_ReleasePlans_ReleasePlanId",
                        column: x => x.ReleasePlanId,
                        principalTable: "ReleasePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReleasePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DocumentName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseDocuments_ReleasePlans_ReleasePlanId",
                        column: x => x.ReleasePlanId,
                        principalTable: "ReleasePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReleasePlanItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReleasePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeploymentOrder = table.Column<int>(type: "int", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false),
                    RollbackRequired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleasePlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleasePlanItems_ChangeRequests_ChangeRequestId",
                        column: x => x.ChangeRequestId,
                        principalTable: "ChangeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReleasePlanItems_ReleasePlans_ReleasePlanId",
                        column: x => x.ReleasePlanId,
                        principalTable: "ReleasePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseAuditEvents_ActorUserId",
                table: "ReleaseAuditEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseAuditEvents_CreatedAt",
                table: "ReleaseAuditEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseAuditEvents_ReleasePlanId",
                table: "ReleaseAuditEvents",
                column: "ReleasePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseDeploymentPlans_ReleasePlanId",
                table: "ReleaseDeploymentPlans",
                column: "ReleasePlanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseDocuments_ReleasePlanId",
                table: "ReleaseDocuments",
                column: "ReleasePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleasePlanItems_ChangeRequestId",
                table: "ReleasePlanItems",
                column: "ChangeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleasePlanItems_ReleasePlanId_ChangeRequestId",
                table: "ReleasePlanItems",
                columns: new[] { "ReleasePlanId", "ChangeRequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleasePlanItems_ReleasePlanId_DeploymentOrder",
                table: "ReleasePlanItems",
                columns: new[] { "ReleasePlanId", "DeploymentOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleasePlans_CreatedAt",
                table: "ReleasePlans",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReleasePlans_CustomerId",
                table: "ReleasePlans",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleasePlans_EnvironmentId",
                table: "ReleasePlans",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleasePlans_ProjectId",
                table: "ReleasePlans",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleasePlans_ReleaseManagerUserId",
                table: "ReleasePlans",
                column: "ReleaseManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleasePlans_ReleaseNo",
                table: "ReleasePlans",
                column: "ReleaseNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleasePlans_Status",
                table: "ReleasePlans",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReleaseAuditEvents");

            migrationBuilder.DropTable(
                name: "ReleaseDeploymentPlans");

            migrationBuilder.DropTable(
                name: "ReleaseDocuments");

            migrationBuilder.DropTable(
                name: "ReleasePlanItems");

            migrationBuilder.DropTable(
                name: "ReleasePlans");

            migrationBuilder.CreateTable(
                name: "Releases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PlannedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Releases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Releases_Environments_EnvironmentId",
                        column: x => x.EnvironmentId,
                        principalTable: "Environments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Releases_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Releases_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Releases",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "Description", "EnvironmentId", "Name", "PlannedDate", "ProjectId", "Status", "Version" },
                values: new object[,]
                {
                    { new Guid("07777777-7777-7777-7777-777777777701"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b2222222-2222-2222-2222-222222222205"), "EBR Migration üretim yayını.", new Guid("f6666666-6666-6666-6666-666666666604"), "REL-2026-001", new DateTime(2026, 7, 15, 10, 0, 0, 0, DateTimeKind.Utc), new Guid("e5555555-5555-5555-5555-555555555501"), "Planned", "v1.0" },
                    { new Guid("07777777-7777-7777-7777-777777777702"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b2222222-2222-2222-2222-222222222201"), "MES Upgrade UAT taslak yayını.", new Guid("f6666666-6666-6666-6666-666666666613"), "REL-2026-002", new DateTime(2026, 8, 1, 9, 0, 0, 0, DateTimeKind.Utc), new Guid("e5555555-5555-5555-5555-555555555502"), "Draft", "v0.9" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Releases_CreatedByUserId",
                table: "Releases",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Releases_EnvironmentId",
                table: "Releases",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Releases_ProjectId",
                table: "Releases",
                column: "ProjectId");
        }
    }
}
