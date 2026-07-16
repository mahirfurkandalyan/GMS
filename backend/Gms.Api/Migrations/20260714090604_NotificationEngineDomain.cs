using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gms.Api.Migrations
{
    /// <inheritdoc />
    public partial class NotificationEngineDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    InAppEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SubjectTemplate = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BodyTemplate = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveries_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationEvents_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "NotificationTemplates",
                columns: new[] { "Id", "BodyTemplate", "Code", "CreatedAt", "IsSystem", "Module", "Name", "SubjectTemplate" },
                values: new object[,]
                {
                    { new Guid("08ac98f7-c024-864f-4928-b3cccf8add77"), "{{ChangeNo}} numaralı '{{Title}}' değişikliği incelemeye gönderildi.", "ChangeSubmitted", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "CHANGE", "Değişiklik gönderildi", "Yeni değişiklik incelemede: {{ChangeNo}}" },
                    { new Guid("0b54fd46-550b-cb5a-d2cc-46bda2299f2b"), "{{DocumentNo}} dokümanına yeni sürüm ({{Version}}) eklendi.", "DocumentVersionCreated", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DOCUMENT", "Yeni doküman sürümü", "Yeni sürüm: {{DocumentNo}}" },
                    { new Guid("1fb858fb-9d63-68b7-0e96-c20e09fd9e18"), "{{ChangeNo}} numaralı değişikliğin onayı reddedildi.", "ApprovalRejected", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "APPROVAL", "Onay reddedildi", "Onay reddedildi: {{ChangeNo}}" },
                    { new Guid("256c5503-c827-59c7-d9fa-f6499c0a0319"), "{{ChangeNo}} numaralı değişiklik tüm onay adımlarından geçti ve onaylandı.", "ApprovalApproved", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "APPROVAL", "Değişiklik onaylandı", "Değişiklik onaylandı: {{ChangeNo}}" },
                    { new Guid("3ec0b3d3-2a62-f87e-54ea-c52a7974c829"), "{{ValidationNo}} numaralı doğrulama başarıyla tamamlandı; yayın kabul edildi.", "ValidationPassed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "VALIDATION", "Doğrulama geçti", "Doğrulama geçti: {{ValidationNo}}" },
                    { new Guid("8ff16183-30cb-b46d-932c-b1e84527c750"), "{{ExecutionNo}} numaralı yürütme başarıyla tamamlandı.", "ExecutionCompleted", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "EXECUTION", "Yürütme tamamlandı", "Yürütme tamamlandı: {{ExecutionNo}}" },
                    { new Guid("9894be04-16b3-a5be-043c-5f577c052dc0"), "{{DocumentNo}} dokümanına yeni bir dosya yüklendi.", "DocumentUploaded", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DOCUMENT", "Doküman yüklendi", "Doküman yüklendi: {{DocumentNo}}" },
                    { new Guid("aed3ace7-b561-bbb4-5eb6-18a98687741e"), "{{ExecutionNo}} numaralı yürütme başlatıldı.", "ExecutionStarted", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "EXECUTION", "Yürütme başladı", "Yürütme başladı: {{ExecutionNo}}" },
                    { new Guid("b18267f1-a092-6c49-9c07-4fe307b584aa"), "{{ReleaseNo}} numaralı yayın tamamlandı.", "ReleaseCompleted", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "RELEASE", "Yayın tamamlandı", "Yayın tamamlandı: {{ReleaseNo}}" },
                    { new Guid("b285caf3-89d2-9887-833a-faaf1afc5b22"), "{{ExecutionNo}} numaralı yürütme başarısız oldu; geri alma gerekebilir.", "ExecutionFailed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "EXECUTION", "Yürütme başarısız", "Yürütme başarısız: {{ExecutionNo}}" },
                    { new Guid("ccd2eab1-3d69-c544-a3ee-b99bf0bfbbe9"), "{{ApprovalNo}} onay talebinde '{{StepName}}' adımı onayınızı bekliyor.", "ApprovalRequired", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "APPROVAL", "Onay bekleniyor", "Onayınız bekleniyor: {{ApprovalNo}}" },
                    { new Guid("e38170bd-a812-a03c-b1b1-70d4d1fe5b24"), "{{ValidationNo}} numaralı doğrulama başarısız oldu.", "ValidationFailed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "VALIDATION", "Doğrulama başarısız", "Doğrulama başarısız: {{ValidationNo}}" },
                    { new Guid("e70b129b-d7d4-188e-672e-e3d668537feb"), "{{ReleaseNo}} numaralı '{{Name}}' yayını zamanlandı.", "ReleaseScheduled", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "RELEASE", "Yayın zamanlandı", "Yayın zamanlandı: {{ReleaseNo}}" },
                    { new Guid("f0b36a79-7648-285d-62f9-de829cf7ede3"), "Çok sayıda başarısız denemeden sonra hesabınız geçici olarak kilitlendi.", "SecurityLockedOut", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SECURITY", "Hesap kilitlendi", "Hesabınız kilitlendi" },
                    { new Guid("f2e16b94-391f-9112-3bb2-5c231b615063"), "Hesabınızda başarısız bir giriş denemesi yapıldı.", "SecurityLoginFailed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SECURITY", "Başarısız giriş denemesi", "Hesabınızda başarısız giriş" },
                    { new Guid("ff9d2f95-1259-0756-0b98-ad344b55e073"), "Hesabınızın parolası değiştirildi. Bu işlemi siz yapmadıysanız yöneticinize başvurun.", "PasswordChanged", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SECURITY", "Parola değiştirildi", "Parolanız değiştirildi" }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { new Guid("13f7ecbf-128e-e87b-ed9d-c4d1eb1a217c"), "notification.preference", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kendi bildirim tercihlerini yönetme.", "NOTIFICATION", "Bildirim tercihleri" },
                    { new Guid("25924074-2865-fea2-691f-32074bba25f9"), "notification.template.manage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bildirim şablonlarını görüntüleme/güncelleme.", "NOTIFICATION", "Şablon yönetimi" },
                    { new Guid("3aea9a8c-ed7e-9f82-7495-91fd9b275717"), "notification.manage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bildirimleri yönetme (yönetici).", "NOTIFICATION", "Bildirim yönetimi" },
                    { new Guid("7beb1805-c726-548e-8990-0da803f5a65f"), "notification.broadcast", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tüm kullanıcılara/role toplu bildirim gönderme.", "NOTIFICATION", "Toplu bildirim" },
                    { new Guid("be0e6a26-caac-2baf-05f1-1f8dd5cdee4a"), "notification.archive", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kendi bildirimlerini arşivleme.", "NOTIFICATION", "Bildirim arşivleme" },
                    { new Guid("c114e1f4-dd51-a26a-86df-67d0771752b6"), "notification.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kendi bildirimlerini görüntüleme ve okundu işaretleme.", "NOTIFICATION", "Bildirim görüntüleme" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "AssignedAt", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("00ea7f8a-56c3-0f8a-210c-17aa561198e7"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c114e1f4-dd51-a26a-86df-67d0771752b6"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("026b18d3-0a34-1f11-2e31-936ccda348c3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("13f7ecbf-128e-e87b-ed9d-c4d1eb1a217c"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("158d3ddf-c999-1764-cbc9-b8f0d2be91d6"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("be0e6a26-caac-2baf-05f1-1f8dd5cdee4a"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("15a58c7c-57ec-fb24-2644-76da2550c81d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("3aea9a8c-ed7e-9f82-7495-91fd9b275717"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("1c28c938-f147-755b-d6b2-b8b8266c663f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("25924074-2865-fea2-691f-32074bba25f9"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("1ed58cbf-874d-aadc-8d14-14bb0728c4f7"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("be0e6a26-caac-2baf-05f1-1f8dd5cdee4a"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("1f402de3-1f84-a431-a147-cf02de1adf72"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("be0e6a26-caac-2baf-05f1-1f8dd5cdee4a"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("1f6a423b-58a3-a2e8-5405-6df5e2c6eb3b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c114e1f4-dd51-a26a-86df-67d0771752b6"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("2fc6062e-a4ce-f581-3010-bd9ad0cf5aff"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c114e1f4-dd51-a26a-86df-67d0771752b6"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("34c3906b-1517-d7b2-6cc9-d190aba04936"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c114e1f4-dd51-a26a-86df-67d0771752b6"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("3f880926-0d2b-7933-7d77-270e1fc802d1"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("be0e6a26-caac-2baf-05f1-1f8dd5cdee4a"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("4bb99ff6-e4b6-1849-0050-4b1932876fe6"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("13f7ecbf-128e-e87b-ed9d-c4d1eb1a217c"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("4fef4191-2f30-09b2-e8d6-6c094f5b31a7"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("13f7ecbf-128e-e87b-ed9d-c4d1eb1a217c"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("5857153a-45e7-dbc5-96f3-2de5aec8a7a8"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("13f7ecbf-128e-e87b-ed9d-c4d1eb1a217c"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("5dc2e66a-f3a3-4f3f-e0be-4c2bd8553b38"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c114e1f4-dd51-a26a-86df-67d0771752b6"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("65504f90-6447-7fca-194a-35daa5cadc9d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("13f7ecbf-128e-e87b-ed9d-c4d1eb1a217c"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("7094bb61-5a45-dd7b-44cb-e902035b013d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("13f7ecbf-128e-e87b-ed9d-c4d1eb1a217c"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("7b0c7cd3-9877-da53-72a5-45ee26b3019f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("be0e6a26-caac-2baf-05f1-1f8dd5cdee4a"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("7e6c5b2e-2cca-7a5c-2d79-95fd7d170694"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("be0e6a26-caac-2baf-05f1-1f8dd5cdee4a"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("8aeb95c2-0afc-4799-aaf7-2e5af19c378c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("be0e6a26-caac-2baf-05f1-1f8dd5cdee4a"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("8d63a596-cc26-2082-9c3c-180145851980"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("13f7ecbf-128e-e87b-ed9d-c4d1eb1a217c"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("97b1bc33-6871-db2e-16d3-7ad1b9e33c37"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("be0e6a26-caac-2baf-05f1-1f8dd5cdee4a"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("9916802b-3e3d-8776-6d5e-ccf0380d9416"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("7beb1805-c726-548e-8990-0da803f5a65f"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("b0e05e20-43b5-42c7-06b6-7c7ab33472b7"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("13f7ecbf-128e-e87b-ed9d-c4d1eb1a217c"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("c753f9bc-019b-321e-801a-89eb481f978f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c114e1f4-dd51-a26a-86df-67d0771752b6"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("da488bb3-2150-ba16-b364-4951b8019830"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c114e1f4-dd51-a26a-86df-67d0771752b6"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("dc1c74c1-8cb1-307b-7005-204a04a92084"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c114e1f4-dd51-a26a-86df-67d0771752b6"), new Guid("a1111111-1111-1111-1111-111111111108") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_NotificationId",
                table: "NotificationDeliveries",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationEvents_CreatedAt",
                table: "NotificationEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationEvents_NotificationId",
                table: "NotificationEvents",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId_Module",
                table: "NotificationPreferences",
                columns: new[] { "UserId", "Module" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_NotificationNo",
                table: "Notifications",
                column: "NotificationNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId_Status",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Code",
                table: "NotificationTemplates",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationDeliveries");

            migrationBuilder.DropTable(
                name: "NotificationEvents");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "NotificationTemplates");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("00ea7f8a-56c3-0f8a-210c-17aa561198e7"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("026b18d3-0a34-1f11-2e31-936ccda348c3"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("158d3ddf-c999-1764-cbc9-b8f0d2be91d6"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("15a58c7c-57ec-fb24-2644-76da2550c81d"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("1c28c938-f147-755b-d6b2-b8b8266c663f"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("1ed58cbf-874d-aadc-8d14-14bb0728c4f7"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("1f402de3-1f84-a431-a147-cf02de1adf72"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("1f6a423b-58a3-a2e8-5405-6df5e2c6eb3b"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("2fc6062e-a4ce-f581-3010-bd9ad0cf5aff"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("34c3906b-1517-d7b2-6cc9-d190aba04936"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("3f880926-0d2b-7933-7d77-270e1fc802d1"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("4bb99ff6-e4b6-1849-0050-4b1932876fe6"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("4fef4191-2f30-09b2-e8d6-6c094f5b31a7"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("5857153a-45e7-dbc5-96f3-2de5aec8a7a8"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("5dc2e66a-f3a3-4f3f-e0be-4c2bd8553b38"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("65504f90-6447-7fca-194a-35daa5cadc9d"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("7094bb61-5a45-dd7b-44cb-e902035b013d"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("7b0c7cd3-9877-da53-72a5-45ee26b3019f"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("7e6c5b2e-2cca-7a5c-2d79-95fd7d170694"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("8aeb95c2-0afc-4799-aaf7-2e5af19c378c"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("8d63a596-cc26-2082-9c3c-180145851980"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("97b1bc33-6871-db2e-16d3-7ad1b9e33c37"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("9916802b-3e3d-8776-6d5e-ccf0380d9416"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("b0e05e20-43b5-42c7-06b6-7c7ab33472b7"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("c753f9bc-019b-321e-801a-89eb481f978f"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("da488bb3-2150-ba16-b364-4951b8019830"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("dc1c74c1-8cb1-307b-7005-204a04a92084"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("13f7ecbf-128e-e87b-ed9d-c4d1eb1a217c"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("25924074-2865-fea2-691f-32074bba25f9"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("3aea9a8c-ed7e-9f82-7495-91fd9b275717"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("7beb1805-c726-548e-8990-0da803f5a65f"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("be0e6a26-caac-2baf-05f1-1f8dd5cdee4a"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("c114e1f4-dd51-a26a-86df-67d0771752b6"));
        }
    }
}
