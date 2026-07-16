using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gms.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialChangeDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChangeRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    BusinessReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeClass = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RiskScore = table.Column<int>(type: "int", nullable: false),
                    PlannedImplementationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedRollbackDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceSystem = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SourceReference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangeRequests_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChangeRequests_Environments_EnvironmentId",
                        column: x => x.EnvironmentId,
                        principalTable: "Environments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChangeRequests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChangeRequests_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChangeAffectedAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssetName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Criticality = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeAffectedAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangeAffectedAssets_ChangeRequests_ChangeRequestId",
                        column: x => x.ChangeRequestId,
                        principalTable: "ChangeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChangeAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangeAuditEvents_ChangeRequests_ChangeRequestId",
                        column: x => x.ChangeRequestId,
                        principalTable: "ChangeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChangeDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DocumentName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangeDocuments_ChangeRequests_ChangeRequestId",
                        column: x => x.ChangeRequestId,
                        principalTable: "ChangeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChangeRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionNo = table.Column<int>(type: "int", nullable: false),
                    TechnicalSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ImplementationNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    DeploymentInstructions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SqlScript = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    RollbackScript = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    RollbackStrategy = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RollbackOwner = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangeRevisions_ChangeRequests_ChangeRequestId",
                        column: x => x.ChangeRequestId,
                        principalTable: "ChangeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ChangeRequests",
                columns: new[] { "Id", "BusinessReason", "ChangeClass", "ChangeNo", "ChangeType", "CreatedAt", "CreatedByUserId", "CustomerId", "Description", "EnvironmentId", "PlannedImplementationDate", "PlannedRollbackDate", "Priority", "ProjectId", "RiskLevel", "RiskScore", "SourceReference", "SourceSystem", "Status", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11110001-0000-0000-0000-000000000001"), "Operatör verimliliğini artırmak için arayüz iyileştirmesi gereklidir.", "Normal", "CHG-2026-000001", "ApplicationDeployment", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b2222222-2222-2222-2222-222222222201"), new Guid("d4444444-4444-4444-4444-444444444401"), "Operatör ekranlarında kullanılabilirlik güncellemesi.", new Guid("f6666666-6666-6666-6666-666666666604"), new DateTime(2026, 8, 1, 2, 0, 0, 0, DateTimeKind.Utc), null, "Medium", new Guid("e5555555-5555-5555-5555-555555555501"), "Medium", 45, "JIRA-2001", "JIRA", "Draft", "Operatör arayüzü dağıtımı", null },
                    { new Guid("11110002-0000-0000-0000-000000000002"), "Yanlış durum bilgisi üretim raporlamasını bozuyor; acil düzeltme gerekli.", "Emergency", "CHG-2026-000002", "SqlDataFix", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b2222222-2222-2222-2222-222222222205"), new Guid("d4444444-4444-4444-4444-444444444401"), "Yanlış işaretlenmiş parti kayıtlarının acil düzeltmesi.", new Guid("f6666666-6666-6666-6666-666666666604"), new DateTime(2026, 7, 20, 2, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 20, 4, 0, 0, 0, DateTimeKind.Utc), "Critical", new Guid("e5555555-5555-5555-5555-555555555501"), "Critical", 110, "JIRA-2002", "JIRA", "Submitted", "Üretim veri düzeltmesi", null },
                    { new Guid("11110003-0000-0000-0000-000000000003"), "Denetim izlenebilirliği için inceleyen kullanıcı bilgisi tutulmalı.", "Normal", "CHG-2026-000003", "DatabaseSchemaChange", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b2222222-2222-2222-2222-222222222201"), new Guid("d4444444-4444-4444-4444-444444444401"), "batch_record tablosuna reviewed_by kolonu eklenecek.", new Guid("f6666666-6666-6666-6666-666666666604"), new DateTime(2026, 7, 25, 2, 0, 0, 0, DateTimeKind.Utc), null, "High", new Guid("e5555555-5555-5555-5555-555555555501"), "High", 75, "JIRA-2003", "JIRA", "Draft", "Veritabanı şeması güncellemesi", null }
                });

            migrationBuilder.InsertData(
                table: "ChangeAffectedAssets",
                columns: new[] { "Id", "AssetName", "AssetType", "ChangeRequestId", "Criticality", "Description" },
                values: new object[,]
                {
                    { new Guid("31110001-0000-0000-0000-000000000001"), "Operatör Arayüzü", "Application", new Guid("11110001-0000-0000-0000-000000000001"), "High", "Operatör web uygulaması." },
                    { new Guid("31110002-0000-0000-0000-000000000002"), "EBR Üretim Veritabanı", "Database", new Guid("11110002-0000-0000-0000-000000000002"), "Critical", "Birincil üretim veritabanı." },
                    { new Guid("31110003-0000-0000-0000-000000000003"), "batch_record Tablosu", "Table", new Guid("11110003-0000-0000-0000-000000000003"), "High", "Parti kayıt tablosu." }
                });

            migrationBuilder.InsertData(
                table: "ChangeAuditEvents",
                columns: new[] { "Id", "ActorUserId", "ChangeRequestId", "CreatedAt", "Description", "EventType" },
                values: new object[,]
                {
                    { new Guid("51110001-0000-0000-0000-000000000001"), new Guid("b2222222-2222-2222-2222-222222222201"), new Guid("11110001-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Değişiklik oluşturuldu.", "ChangeCreated" },
                    { new Guid("51110002-0000-0000-0000-000000000002"), new Guid("b2222222-2222-2222-2222-222222222205"), new Guid("11110002-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Değişiklik oluşturuldu.", "ChangeCreated" },
                    { new Guid("51110003-0000-0000-0000-000000000003"), new Guid("b2222222-2222-2222-2222-222222222201"), new Guid("11110003-0000-0000-0000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Değişiklik oluşturuldu.", "ChangeCreated" }
                });

            migrationBuilder.InsertData(
                table: "ChangeDocuments",
                columns: new[] { "Id", "ChangeRequestId", "CreatedAt", "DocumentName", "DocumentType", "Status", "Version" },
                values: new object[,]
                {
                    { new Guid("41110001-0000-0000-0000-000000000001"), new Guid("11110001-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "UAT Test Kanıtı", "TestEvidence", "Active", "v1" },
                    { new Guid("41110002-0000-0000-0000-000000000002"), new Guid("11110002-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Veri Düzeltme Betiği", "SqlScript", "Active", "v1" },
                    { new Guid("41110003-0000-0000-0000-000000000003"), new Guid("11110003-0000-0000-0000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Şema Değişikliği Betiği", "SqlScript", "Active", "v1" }
                });

            migrationBuilder.InsertData(
                table: "ChangeRevisions",
                columns: new[] { "Id", "ChangeRequestId", "CreatedAt", "CreatedByUserId", "DeploymentInstructions", "EstimatedDurationMinutes", "ImplementationNotes", "RevisionNo", "RollbackOwner", "RollbackScript", "RollbackStrategy", "SqlScript", "TechnicalSummary" },
                values: new object[,]
                {
                    { new Guid("21110001-0000-0000-0000-000000000001"), new Guid("11110001-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b2222222-2222-2222-2222-222222222201"), "CI paketini PROD ortamına dağıt.", 30, "", 1, "Ali Vural", "Önceki artifact sürümüne geri dön (v1.1).", "Artifact geri alma", "", "Angular derlemesi PROD'a dağıtılacak." },
                    { new Guid("21110002-0000-0000-0000-000000000002"), new Guid("11110002-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b2222222-2222-2222-2222-222222222205"), "", 20, "", 1, "System Administrator", "UPDATE batch_record SET status='PENDING' WHERE status='REVIEWED' AND updated_today=1;", "Ters UPDATE + yedekten geri yükleme", "UPDATE batch_record SET status='REVIEWED' WHERE status='PENDING';", "Parti durum düzeltmesi (UPDATE) uygulanacak." },
                    { new Guid("21110003-0000-0000-0000-000000000003"), new Guid("11110003-0000-0000-0000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b2222222-2222-2222-2222-222222222201"), "", 45, "", 1, "", "", "", "ALTER TABLE batch_record ADD reviewed_by NVARCHAR(100) NULL;", "ALTER TABLE ile kolon eklenecek." }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChangeAffectedAssets_ChangeRequestId",
                table: "ChangeAffectedAssets",
                column: "ChangeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeAuditEvents_ChangeRequestId",
                table: "ChangeAuditEvents",
                column: "ChangeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeAuditEvents_CreatedAt",
                table: "ChangeAuditEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeDocuments_ChangeRequestId",
                table: "ChangeDocuments",
                column: "ChangeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeRequests_ChangeNo",
                table: "ChangeRequests",
                column: "ChangeNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChangeRequests_CreatedAt",
                table: "ChangeRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeRequests_CreatedByUserId",
                table: "ChangeRequests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeRequests_CustomerId",
                table: "ChangeRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeRequests_EnvironmentId",
                table: "ChangeRequests",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeRequests_ProjectId",
                table: "ChangeRequests",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeRequests_RiskLevel",
                table: "ChangeRequests",
                column: "RiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeRequests_Status",
                table: "ChangeRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeRevisions_ChangeRequestId_RevisionNo",
                table: "ChangeRevisions",
                columns: new[] { "ChangeRequestId", "RevisionNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChangeAffectedAssets");

            migrationBuilder.DropTable(
                name: "ChangeAuditEvents");

            migrationBuilder.DropTable(
                name: "ChangeDocuments");

            migrationBuilder.DropTable(
                name: "ChangeRevisions");

            migrationBuilder.DropTable(
                name: "ChangeRequests");
        }
    }
}
