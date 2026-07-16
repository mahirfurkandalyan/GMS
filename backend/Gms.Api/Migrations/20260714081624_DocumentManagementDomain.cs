using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gms.Api.Migrations
{
    /// <inheritdoc />
    public partial class DocumentManagementDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HashAlgorithm = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CurrentHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentAuditEvents_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentDownloads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DownloadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DownloadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentDownloads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentDownloads_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentLinks_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Extension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256Hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentVersions_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentVersions_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { new Guid("12279021-e96a-8351-eef2-b877c0b7f77b"), "document.unlink", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Doküman bağını kaldırma.", "DOCUMENT", "Doküman bağını kaldırma" },
                    { new Guid("3fdf46d1-b81c-1f99-88f9-ad0f8237a96f"), "document.delete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dokümanı yumuşak silme (soft delete).", "DOCUMENT", "Doküman silme" },
                    { new Guid("4c0cf3d9-9873-bb6b-2c6e-5b0428105543"), "document.audit.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Doküman denetim ve indirme geçmişini görüntüleme.", "DOCUMENT", "Doküman denetimi" },
                    { new Guid("4db19e4d-4e81-dfac-345d-6c2e81e5b4bf"), "document.link", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dokümanı bir iş nesnesine bağlama.", "DOCUMENT", "Doküman bağlama" },
                    { new Guid("54800adc-feed-b03c-2531-c71815da5615"), "document.update", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Doküman metadata güncelleme.", "DOCUMENT", "Doküman güncelleme" },
                    { new Guid("5e8d9382-8a85-1a5b-62db-536be39955a6"), "document.create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yeni doküman (metadata) oluşturma.", "DOCUMENT", "Doküman oluşturma" },
                    { new Guid("b94ffc66-da80-d759-cdd7-425edfe60216"), "document.archive", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dokümanı arşivleme.", "DOCUMENT", "Doküman arşivleme" },
                    { new Guid("d2e117b6-58d2-89c7-8c3e-78d2aadedf92"), "document.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dokümanları listeleme ve görüntüleme.", "DOCUMENT", "Doküman görüntüleme" },
                    { new Guid("d80fb99f-5e5c-7c2c-b9ad-fcfee675e82c"), "document.version.create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dokümana yeni sürüm ekleme.", "DOCUMENT", "Yeni sürüm" },
                    { new Guid("f2e7ed5b-4bca-b47b-4c1e-3361ed8e8c01"), "document.upload", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dokümana ilk dosyayı yükleme.", "DOCUMENT", "Doküman yükleme" },
                    { new Guid("f43d45d8-e071-3620-5b7b-f3569cc3a546"), "document.download", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Doküman sürümlerini indirme.", "DOCUMENT", "Doküman indirme" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "AssignedAt", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("022a34bd-c0bc-5c10-0bbf-085c27b6481e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d2e117b6-58d2-89c7-8c3e-78d2aadedf92"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("041799e4-2a1c-a6bf-9cab-82be402b13fc"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d2e117b6-58d2-89c7-8c3e-78d2aadedf92"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("042990af-bab9-976c-a8f0-c7eeb3f473c1"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f2e7ed5b-4bca-b47b-4c1e-3361ed8e8c01"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("08a0db0c-8d6b-adfd-bc35-642f5b54f2ff"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f43d45d8-e071-3620-5b7b-f3569cc3a546"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("0c4289ae-cfcb-ba04-3236-164294f8ef0d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4db19e4d-4e81-dfac-345d-6c2e81e5b4bf"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("0f889e89-e29e-7d47-2950-197ad88f987c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f43d45d8-e071-3620-5b7b-f3569cc3a546"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("137dd896-e9f0-9c96-ac5e-83f0a58b38f4"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4db19e4d-4e81-dfac-345d-6c2e81e5b4bf"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("14b839cd-232a-2e14-df3d-7b0d995a3a83"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f43d45d8-e071-3620-5b7b-f3569cc3a546"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("23a05560-7d3e-73cc-9374-9f65c6f226bb"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f43d45d8-e071-3620-5b7b-f3569cc3a546"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("251ba98f-3d22-062c-4b99-f38fe7a1f4a9"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d80fb99f-5e5c-7c2c-b9ad-fcfee675e82c"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("25c48b71-6c01-5c9b-24ff-34b425cc032e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4db19e4d-4e81-dfac-345d-6c2e81e5b4bf"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("3148b1f2-5f1a-507b-2fee-d454e2d21ffb"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f43d45d8-e071-3620-5b7b-f3569cc3a546"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("3187ae53-47c9-fdea-4464-f82a31f0e69a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4db19e4d-4e81-dfac-345d-6c2e81e5b4bf"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("3379cc82-2875-0e62-ee9d-e5b818c831b9"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("54800adc-feed-b03c-2531-c71815da5615"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("375f092b-b8b1-1770-4763-d584544209ff"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b94ffc66-da80-d759-cdd7-425edfe60216"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("4cc1bfb2-5825-fd86-bfce-d1089955859c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5e8d9382-8a85-1a5b-62db-536be39955a6"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("51bfb2de-8319-32ab-905f-407cf9c9e2a9"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d2e117b6-58d2-89c7-8c3e-78d2aadedf92"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("520d6525-1b0b-c435-d8dc-448bcf959848"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("12279021-e96a-8351-eef2-b877c0b7f77b"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("5368d3b0-0109-613e-ff9d-8684c1590015"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d2e117b6-58d2-89c7-8c3e-78d2aadedf92"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("572f9d0b-de10-d7c5-32ff-5457bf2e8e26"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5e8d9382-8a85-1a5b-62db-536be39955a6"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("5d87ef07-6d8c-5719-e64f-8d325664609e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d2e117b6-58d2-89c7-8c3e-78d2aadedf92"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("5e3469ab-a218-4366-89a4-9d01073d5db4"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f43d45d8-e071-3620-5b7b-f3569cc3a546"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("609eb536-16e6-f60a-a3b5-c5522cd30980"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("54800adc-feed-b03c-2531-c71815da5615"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("6aa7f2d6-6eec-92b0-5fed-8ea1b6e19e8b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4c0cf3d9-9873-bb6b-2c6e-5b0428105543"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("6bd09c1b-d56a-6a8e-017a-dd8779751dd8"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4db19e4d-4e81-dfac-345d-6c2e81e5b4bf"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("6cdc7b90-bc1c-b382-6df9-8661ff6d3379"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d80fb99f-5e5c-7c2c-b9ad-fcfee675e82c"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("75b00027-e413-a933-b7e8-1e50ce292d1d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f2e7ed5b-4bca-b47b-4c1e-3361ed8e8c01"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("7ae46bdf-2473-ece8-43c0-c43534161f5d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5e8d9382-8a85-1a5b-62db-536be39955a6"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("7b7dcea4-ae9a-d4d9-f5e4-86866e9dda35"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d80fb99f-5e5c-7c2c-b9ad-fcfee675e82c"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("7d63d89a-206a-1e52-de32-aef7691e020c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4db19e4d-4e81-dfac-345d-6c2e81e5b4bf"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("83191fc6-5d34-a21b-6ccb-eade959e0bc3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4c0cf3d9-9873-bb6b-2c6e-5b0428105543"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("8380e6ad-515c-a6ce-7f13-5b979bd4835d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d80fb99f-5e5c-7c2c-b9ad-fcfee675e82c"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("894339d3-ac37-49d6-9974-586d1d97f575"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("12279021-e96a-8351-eef2-b877c0b7f77b"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("90b5d0fd-de11-cadc-60c6-9ded11ed3f03"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d80fb99f-5e5c-7c2c-b9ad-fcfee675e82c"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("94bc8aea-9f22-1feb-2cc3-016f1528ea78"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f2e7ed5b-4bca-b47b-4c1e-3361ed8e8c01"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("981e2d4b-6980-6cad-57c9-cd626daabe50"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d2e117b6-58d2-89c7-8c3e-78d2aadedf92"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("9dc0d7e0-8556-0d1c-630d-a8830583a9b6"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f2e7ed5b-4bca-b47b-4c1e-3361ed8e8c01"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("a812b6bc-d051-0c68-bbdb-724273a65204"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b94ffc66-da80-d759-cdd7-425edfe60216"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("a97cc4e5-bbc4-bd81-1d82-023ec6dbbe0b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("12279021-e96a-8351-eef2-b877c0b7f77b"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("aeed3a6f-7251-131a-ab9b-bc4ac9d4d4af"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f43d45d8-e071-3620-5b7b-f3569cc3a546"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("b2946d54-ba36-7746-c574-288bc145ff38"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5e8d9382-8a85-1a5b-62db-536be39955a6"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("b333b27c-8eca-3f06-9c56-97f9f5e70d5e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d80fb99f-5e5c-7c2c-b9ad-fcfee675e82c"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("b6c647f1-d528-12d9-2cd3-66a070584b22"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f2e7ed5b-4bca-b47b-4c1e-3361ed8e8c01"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("c0e665b3-6a42-0b80-7d12-c62320a013b8"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("12279021-e96a-8351-eef2-b877c0b7f77b"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("c4a2c989-4141-5831-7ada-7f94b7a35c22"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("54800adc-feed-b03c-2531-c71815da5615"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("c635ed9f-aa54-76b6-cd77-8d8cd218e0c2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f43d45d8-e071-3620-5b7b-f3569cc3a546"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("c68e2f26-717e-3dac-74fb-4be691ac94e6"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("12279021-e96a-8351-eef2-b877c0b7f77b"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("c87610f9-5f87-353b-ca8a-b6f4af066e4a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f2e7ed5b-4bca-b47b-4c1e-3361ed8e8c01"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("cc511813-86d5-0fbf-ad11-3f6d20eeacd7"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4db19e4d-4e81-dfac-345d-6c2e81e5b4bf"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("d7b48fda-ef71-b57d-022e-2fcd691e6d00"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5e8d9382-8a85-1a5b-62db-536be39955a6"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("d821fcf8-110c-4edc-fafe-8a59dd820ea3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5e8d9382-8a85-1a5b-62db-536be39955a6"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("de77f64b-c085-4376-b1cb-a7beee24b7d4"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f2e7ed5b-4bca-b47b-4c1e-3361ed8e8c01"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("eb78cda0-1e64-6b77-bf2f-20c305535a30"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d80fb99f-5e5c-7c2c-b9ad-fcfee675e82c"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("f316217b-e79d-6dc8-9877-a8cdad8a7e85"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("12279021-e96a-8351-eef2-b877c0b7f77b"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("f46a507c-83fc-ef7a-489a-6a3895a9cfe1"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d2e117b6-58d2-89c7-8c3e-78d2aadedf92"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("f9b2511d-bcee-c539-955c-686a3d51f117"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("12279021-e96a-8351-eef2-b877c0b7f77b"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("f9b7b220-ea0f-3cf1-61c6-5d1ca4c4a81f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("3fdf46d1-b81c-1f99-88f9-ad0f8237a96f"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("fc0363a7-c0ac-77d7-d53d-6c837b42c04a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d2e117b6-58d2-89c7-8c3e-78d2aadedf92"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("fd468e82-98f9-b844-dc96-0392f966f22e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5e8d9382-8a85-1a5b-62db-536be39955a6"), new Guid("a1111111-1111-1111-1111-111111111106") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAuditEvents_CreatedAt",
                table: "DocumentAuditEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAuditEvents_DocumentId",
                table: "DocumentAuditEvents",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDownloads_DocumentId",
                table: "DocumentDownloads",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDownloads_DownloadedAt",
                table: "DocumentDownloads",
                column: "DownloadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentLinks_DocumentId_ObjectType_ObjectId",
                table: "DocumentLinks",
                columns: new[] { "DocumentId", "ObjectType", "ObjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentLinks_ObjectType_ObjectId",
                table: "DocumentLinks",
                columns: new[] { "ObjectType", "ObjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Category",
                table: "Documents",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CreatedAt",
                table: "Documents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentNo",
                table: "Documents",
                column: "DocumentNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_OwnerUserId",
                table: "Documents",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Status",
                table: "Documents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_DocumentId_VersionNumber",
                table: "DocumentVersions",
                columns: new[] { "DocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_UploadedByUserId",
                table: "DocumentVersions",
                column: "UploadedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentAuditEvents");

            migrationBuilder.DropTable(
                name: "DocumentDownloads");

            migrationBuilder.DropTable(
                name: "DocumentLinks");

            migrationBuilder.DropTable(
                name: "DocumentVersions");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("022a34bd-c0bc-5c10-0bbf-085c27b6481e"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("041799e4-2a1c-a6bf-9cab-82be402b13fc"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("042990af-bab9-976c-a8f0-c7eeb3f473c1"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("08a0db0c-8d6b-adfd-bc35-642f5b54f2ff"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("0c4289ae-cfcb-ba04-3236-164294f8ef0d"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("0f889e89-e29e-7d47-2950-197ad88f987c"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("137dd896-e9f0-9c96-ac5e-83f0a58b38f4"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("14b839cd-232a-2e14-df3d-7b0d995a3a83"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("23a05560-7d3e-73cc-9374-9f65c6f226bb"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("251ba98f-3d22-062c-4b99-f38fe7a1f4a9"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("25c48b71-6c01-5c9b-24ff-34b425cc032e"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("3148b1f2-5f1a-507b-2fee-d454e2d21ffb"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("3187ae53-47c9-fdea-4464-f82a31f0e69a"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("3379cc82-2875-0e62-ee9d-e5b818c831b9"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("375f092b-b8b1-1770-4763-d584544209ff"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("4cc1bfb2-5825-fd86-bfce-d1089955859c"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("51bfb2de-8319-32ab-905f-407cf9c9e2a9"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("520d6525-1b0b-c435-d8dc-448bcf959848"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("5368d3b0-0109-613e-ff9d-8684c1590015"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("572f9d0b-de10-d7c5-32ff-5457bf2e8e26"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("5d87ef07-6d8c-5719-e64f-8d325664609e"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("5e3469ab-a218-4366-89a4-9d01073d5db4"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("609eb536-16e6-f60a-a3b5-c5522cd30980"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("6aa7f2d6-6eec-92b0-5fed-8ea1b6e19e8b"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("6bd09c1b-d56a-6a8e-017a-dd8779751dd8"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("6cdc7b90-bc1c-b382-6df9-8661ff6d3379"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("75b00027-e413-a933-b7e8-1e50ce292d1d"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("7ae46bdf-2473-ece8-43c0-c43534161f5d"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("7b7dcea4-ae9a-d4d9-f5e4-86866e9dda35"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("7d63d89a-206a-1e52-de32-aef7691e020c"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("83191fc6-5d34-a21b-6ccb-eade959e0bc3"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("8380e6ad-515c-a6ce-7f13-5b979bd4835d"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("894339d3-ac37-49d6-9974-586d1d97f575"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("90b5d0fd-de11-cadc-60c6-9ded11ed3f03"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("94bc8aea-9f22-1feb-2cc3-016f1528ea78"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("981e2d4b-6980-6cad-57c9-cd626daabe50"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("9dc0d7e0-8556-0d1c-630d-a8830583a9b6"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("a812b6bc-d051-0c68-bbdb-724273a65204"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("a97cc4e5-bbc4-bd81-1d82-023ec6dbbe0b"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("aeed3a6f-7251-131a-ab9b-bc4ac9d4d4af"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("b2946d54-ba36-7746-c574-288bc145ff38"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("b333b27c-8eca-3f06-9c56-97f9f5e70d5e"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("b6c647f1-d528-12d9-2cd3-66a070584b22"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("c0e665b3-6a42-0b80-7d12-c62320a013b8"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("c4a2c989-4141-5831-7ada-7f94b7a35c22"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("c635ed9f-aa54-76b6-cd77-8d8cd218e0c2"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("c68e2f26-717e-3dac-74fb-4be691ac94e6"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("c87610f9-5f87-353b-ca8a-b6f4af066e4a"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("cc511813-86d5-0fbf-ad11-3f6d20eeacd7"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("d7b48fda-ef71-b57d-022e-2fcd691e6d00"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("d821fcf8-110c-4edc-fafe-8a59dd820ea3"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("de77f64b-c085-4376-b1cb-a7beee24b7d4"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("eb78cda0-1e64-6b77-bf2f-20c305535a30"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("f316217b-e79d-6dc8-9877-a8cdad8a7e85"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("f46a507c-83fc-ef7a-489a-6a3895a9cfe1"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("f9b2511d-bcee-c539-955c-686a3d51f117"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("f9b7b220-ea0f-3cf1-61c6-5d1ca4c4a81f"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("fc0363a7-c0ac-77d7-d53d-6c837b42c04a"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("fd468e82-98f9-b844-dc96-0392f966f22e"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("12279021-e96a-8351-eef2-b877c0b7f77b"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("3fdf46d1-b81c-1f99-88f9-ad0f8237a96f"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("4c0cf3d9-9873-bb6b-2c6e-5b0428105543"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("4db19e4d-4e81-dfac-345d-6c2e81e5b4bf"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("54800adc-feed-b03c-2531-c71815da5615"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("5e8d9382-8a85-1a5b-62db-536be39955a6"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("b94ffc66-da80-d759-cdd7-425edfe60216"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("d2e117b6-58d2-89c7-8c3e-78d2aadedf92"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("d80fb99f-5e5c-7c2c-b9ad-fcfee675e82c"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("f2e7ed5b-4bca-b47b-4c1e-3361ed8e8c01"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("f43d45d8-e071-3620-5b7b-f3569cc3a546"));
        }
    }
}
