using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gms.Api.Migrations
{
    /// <inheritdoc />
    public partial class IdentityAndRbacDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles");

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEnd",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Users",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "UserRoles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedByUserId",
                table: "UserRoles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Roles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemRole",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReasonRevoked = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecurityAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { new Guid("005e39a2-163e-679d-5d15-308a93e113b6"), "approval.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Onay taleplerini görüntüleme.", "APPROVAL", "Onay görüntüleme" },
                    { new Guid("00626bc6-6c23-2661-34a2-793e6d858052"), "approval.request-revision", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Onayda revizyon talep etme.", "APPROVAL", "Revizyon talebi" },
                    { new Guid("02e46a11-e05e-8317-80f9-d1672bdd960b"), "change.submit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Değişikliği incelemeye gönderme.", "CHANGE", "Değişiklik gönderme" },
                    { new Guid("07e2bcb8-f997-6e22-5713-10c2498813f1"), "execution.step.start", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yürütme adımını başlatma.", "EXECUTION", "Adım başlatma" },
                    { new Guid("0addb09b-2bed-0638-0907-93a029f7a584"), "approval.approve.release-manager", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yayın yöneticisi onay adımını onaylama.", "APPROVAL", "Yayın yöneticisi onayı" },
                    { new Guid("0f3e32ac-caea-a8ab-c8df-e5d60a89bb14"), "change.create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yeni değişiklik oluşturma.", "CHANGE", "Değişiklik oluşturma" },
                    { new Guid("19382a01-4b1a-b59a-cf67-aac328ef75ba"), "admin.users.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kullanıcıları görüntüleme.", "ADMINISTRATION", "Kullanıcı görüntüleme" },
                    { new Guid("27e68165-6b30-5b14-fa12-d434fc5ec322"), "admin.roles.manage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Rol/izin yönetimi.", "ADMINISTRATION", "Rol yönetimi" },
                    { new Guid("4468d67a-8e58-9863-3f21-9cf8c67091a9"), "validation.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Doğrulamaları görüntüleme.", "VALIDATION", "Doğrulama görüntüleme" },
                    { new Guid("4d247959-0cdd-cca6-9814-bd354ee7748a"), "change.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Değişiklikleri listeleme ve görüntüleme.", "CHANGE", "Değişiklik görüntüleme" },
                    { new Guid("57045883-98f3-d735-7c4c-916800f58d88"), "approval.approve.qa", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "QA onay adımını onaylama.", "APPROVAL", "QA onayı" },
                    { new Guid("582aa68d-bf01-3db0-27b8-723c15b4b836"), "execution.step.complete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yürütme adımını tamamlama.", "EXECUTION", "Adım tamamlama" },
                    { new Guid("5ec83450-d64f-1280-91b2-6aa7cd9e90ea"), "approval.approve.admin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Admin onay adımını onaylama.", "APPROVAL", "Admin onayı" },
                    { new Guid("6d42889c-8458-36e5-6e8e-34d78196ad8c"), "approval.reject", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Onay adımını reddetme.", "APPROVAL", "Onay reddi" },
                    { new Guid("7ba22f42-c829-1e1e-ca45-045bd517d467"), "execution.start", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yürütmeyi başlatma.", "EXECUTION", "Yürütme başlatma" },
                    { new Guid("7c0a0aac-e497-34fb-3f24-545fcdb75be5"), "release.complete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yayını manuel tamamlama.", "RELEASE", "Yayın tamamlama" },
                    { new Guid("81effa15-eb47-fe85-333f-d7b3cda1869f"), "release.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yayın planlarını görüntüleme.", "RELEASE", "Yayın görüntüleme" },
                    { new Guid("87671c6d-1e98-757d-406e-02dfc16f43f9"), "admin.users.manage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kullanıcı oluşturma/güncelleme/rol atama.", "ADMINISTRATION", "Kullanıcı yönetimi" },
                    { new Guid("9123aaf7-8468-e8bc-8e2e-2efd3cc00b63"), "change.update", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Değişiklik alanlarını güncelleme.", "CHANGE", "Değişiklik güncelleme" },
                    { new Guid("954522db-ed42-78da-0496-dac0ccd3fbdb"), "change.revision.create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Değişiklik revizyonu ekleme.", "CHANGE", "Revizyon oluşturma" },
                    { new Guid("9cdaa276-b5b6-6f2c-73ba-3c0b2843c8aa"), "admin.roles.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Rolleri görüntüleme.", "ADMINISTRATION", "Rol görüntüleme" },
                    { new Guid("a1a18aca-bd4c-02a5-d2b5-eb8cd22bd042"), "release.cancel", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yayını iptal etme.", "RELEASE", "Yayın iptali" },
                    { new Guid("a6afbdd8-d92c-f61e-ce0c-03036196c13c"), "validation.start", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Doğrulamayı başlatma.", "VALIDATION", "Doğrulama başlatma" },
                    { new Guid("a71b71a5-d43e-dbca-8091-22f56c01b7b9"), "audit.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Denetim kayıtlarını görüntüleme.", "AUDIT", "Denetim görüntüleme" },
                    { new Guid("aba6fbc6-2cc7-0884-c406-0382c0ea4d7f"), "release.update", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yayın planını güncelleme.", "RELEASE", "Yayın güncelleme" },
                    { new Guid("b9b28aa8-11cf-795b-5a8b-54644268e870"), "execution.rollback", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yürütmeyi geri alma.", "EXECUTION", "Yürütme geri alma" },
                    { new Guid("bd717b38-8d24-5b11-8c9f-8ac416231964"), "execution.step.fail", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yürütme adımını başarısız işaretleme.", "EXECUTION", "Adım başarısız" },
                    { new Guid("c7654de4-1947-c445-a710-118157151b44"), "change.cancel", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Değişikliği iptal etme.", "CHANGE", "Değişiklik iptali" },
                    { new Guid("d03e665f-6ecc-47b2-7d95-57de5df2c82b"), "release.schedule", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yayını zamanlama.", "RELEASE", "Yayın zamanlama" },
                    { new Guid("db2b8bf7-12d7-8e29-b6bf-94a59f59789c"), "validation.create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Doğrulama oluşturma.", "VALIDATION", "Doğrulama oluşturma" },
                    { new Guid("e65fb6ad-98f5-fff7-9da3-77d68f910505"), "execution.create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yürütme oluşturma.", "EXECUTION", "Yürütme oluşturma" },
                    { new Guid("e6e6f0a1-f741-2c5b-9040-2be3379153cc"), "validation.check.execute", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Doğrulama kontrolünü yürütme (pass/fail).", "VALIDATION", "Kontrol yürütme" },
                    { new Guid("eedd60a0-1213-3658-cbc1-c49a516d877c"), "execution.read", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yürütmeleri görüntüleme.", "EXECUTION", "Yürütme görüntüleme" },
                    { new Guid("f61f7ad1-a486-788d-ac0b-3755c529181d"), "release.create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yeni yayın planı oluşturma.", "RELEASE", "Yayın oluşturma" },
                    { new Guid("fe391c82-c075-7dd8-a66c-f96e836c10f3"), "approval.approve.architect", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mimari onay adımını onaylama.", "APPROVAL", "Mimari onayı" }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1111111-1111-1111-1111-111111111101"),
                columns: new[] { "CreatedAt", "IsActive", "IsSystemRole" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1111111-1111-1111-1111-111111111102"),
                columns: new[] { "CreatedAt", "Description", "IsActive", "IsSystemRole" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Teknik tasarım ve mimari onay yapan kullanıcı.", true, true });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1111111-1111-1111-1111-111111111103"),
                columns: new[] { "CreatedAt", "IsActive", "IsSystemRole" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1111111-1111-1111-1111-111111111104"),
                columns: new[] { "CreatedAt", "Description", "IsActive", "IsSystemRole" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kalite onayı ve doğrulama yapan kullanıcı.", true, true });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1111111-1111-1111-1111-111111111105"),
                columns: new[] { "CreatedAt", "IsActive", "IsSystemRole" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "IsSystemRole", "Name" },
                values: new object[,]
                {
                    { new Guid("a1111111-1111-1111-1111-111111111106"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yayın planlama ve yayın onayı yapan kullanıcı.", true, true, "ReleaseManager" },
                    { new Guid("a1111111-1111-1111-1111-111111111107"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yürütme sonrası doğrulama yapan kullanıcı.", true, true, "Validator" },
                    { new Guid("a1111111-1111-1111-1111-111111111108"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Denetim kayıtlarını okuyan kullanıcı.", true, true, "Auditor" }
                });

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("c3333333-3333-3333-3333-333333333301"),
                columns: new[] { "AssignedAt", "AssignedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("c3333333-3333-3333-3333-333333333302"),
                columns: new[] { "AssignedAt", "AssignedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("c3333333-3333-3333-3333-333333333303"),
                columns: new[] { "AssignedAt", "AssignedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("c3333333-3333-3333-3333-333333333304"),
                columns: new[] { "AssignedAt", "AssignedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("c3333333-3333-3333-3333-333333333305"),
                columns: new[] { "AssignedAt", "AssignedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("b2222222-2222-2222-2222-222222222201"),
                columns: new[] { "FailedLoginCount", "IsActive", "LastLoginAt", "LockoutEnd", "NormalizedEmail", "PasswordHash", "UpdatedAt" },
                values: new object[] { 0, true, null, null, "REQUESTER@GMS.LOCAL", "", null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("b2222222-2222-2222-2222-222222222202"),
                columns: new[] { "FailedLoginCount", "IsActive", "LastLoginAt", "LockoutEnd", "NormalizedEmail", "PasswordHash", "UpdatedAt" },
                values: new object[] { 0, true, null, null, "ARCHITECT@GMS.LOCAL", "", null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("b2222222-2222-2222-2222-222222222203"),
                columns: new[] { "FailedLoginCount", "IsActive", "LastLoginAt", "LockoutEnd", "NormalizedEmail", "PasswordHash", "UpdatedAt" },
                values: new object[] { 0, true, null, null, "EXECUTOR@GMS.LOCAL", "", null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("b2222222-2222-2222-2222-222222222204"),
                columns: new[] { "FailedLoginCount", "IsActive", "LastLoginAt", "LockoutEnd", "NormalizedEmail", "PasswordHash", "UpdatedAt" },
                values: new object[] { 0, true, null, null, "QA@GMS.LOCAL", "", null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("b2222222-2222-2222-2222-222222222205"),
                columns: new[] { "FailedLoginCount", "IsActive", "LastLoginAt", "LockoutEnd", "NormalizedEmail", "PasswordHash", "UpdatedAt" },
                values: new object[] { 0, true, null, null, "ADMIN@GMS.LOCAL", "", null });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FailedLoginCount", "FullName", "IsActive", "LastLoginAt", "LockoutEnd", "NormalizedEmail", "PasswordHash", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("b2222222-2222-2222-2222-222222222206"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "release.manager@gms.local", 0, "Release Manager", true, null, null, "RELEASE.MANAGER@GMS.LOCAL", "", "Active", null },
                    { new Guid("b2222222-2222-2222-2222-222222222207"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "validator@gms.local", 0, "Validator User", true, null, null, "VALIDATOR@GMS.LOCAL", "", "Active", null },
                    { new Guid("b2222222-2222-2222-2222-222222222208"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "auditor@gms.local", 0, "Auditor User", true, null, null, "AUDITOR@GMS.LOCAL", "", "Active", null }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "AssignedAt", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("00ea30fd-375d-b904-06f3-5d598923d3ee"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("81effa15-eb47-fe85-333f-d7b3cda1869f"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("06271b5a-be06-7b2c-401f-df512ec31391"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("005e39a2-163e-679d-5d15-308a93e113b6"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("075c8e6a-3233-5a72-0855-01821e917e30"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("582aa68d-bf01-3db0-27b8-723c15b4b836"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("0c58a077-16b9-386f-d5d6-f3f30675d840"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("6d42889c-8458-36e5-6e8e-34d78196ad8c"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("0c9ce3c1-7879-f911-b63b-ac2cfd48f325"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("7ba22f42-c829-1e1e-ca45-045bd517d467"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("0f682043-3247-fd0a-d68c-e27ab5db9470"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4468d67a-8e58-9863-3f21-9cf8c67091a9"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("11436f8c-1803-c330-11b6-6ff53394b257"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("bd717b38-8d24-5b11-8c9f-8ac416231964"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("12459e15-eac3-4175-d36f-8356b64bf506"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4468d67a-8e58-9863-3f21-9cf8c67091a9"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("12dc0cb7-8e96-fc4c-d1a2-c2a90205424d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("6d42889c-8458-36e5-6e8e-34d78196ad8c"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("134430e7-7cc9-ba49-2fef-81c380fae539"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("9123aaf7-8468-e8bc-8e2e-2efd3cc00b63"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("148552eb-b9a9-4393-91e6-2c95cdd8dff4"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("fe391c82-c075-7dd8-a66c-f96e836c10f3"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("152cd524-6679-ad1b-8e77-ec1c937c6e00"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("eedd60a0-1213-3658-cbc1-c49a516d877c"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("153372cb-90dc-b10e-1140-de442bbdd93c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4d247959-0cdd-cca6-9814-bd354ee7748a"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("16d7fbfc-7c2c-4512-9fd3-460ae29f5c4d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("81effa15-eb47-fe85-333f-d7b3cda1869f"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("16dae36b-4d5d-ae39-b323-bd72c90a49af"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("6d42889c-8458-36e5-6e8e-34d78196ad8c"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("19995fb7-de49-be20-0d37-347b21e8458c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4468d67a-8e58-9863-3f21-9cf8c67091a9"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("1ee3869c-652e-1425-424d-380ae8204d30"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("81effa15-eb47-fe85-333f-d7b3cda1869f"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("21d3eec1-7e8f-44be-6d0a-ced40f78a873"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("07e2bcb8-f997-6e22-5713-10c2498813f1"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("22aea7ac-4a46-8849-6fdf-1616765558a5"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("81effa15-eb47-fe85-333f-d7b3cda1869f"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("27183249-fb38-8aad-de04-2c0d8b499b66"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("eedd60a0-1213-3658-cbc1-c49a516d877c"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("281a18e3-f003-e620-b8f8-daf184b02895"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4d247959-0cdd-cca6-9814-bd354ee7748a"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("2992cd5a-b8c8-2ddd-796d-a042c6187c0f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("eedd60a0-1213-3658-cbc1-c49a516d877c"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("2a08a781-de99-de3b-4373-02f53a864e95"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("bd717b38-8d24-5b11-8c9f-8ac416231964"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("2ed4dc64-f2bc-fa46-edbc-59dc7ecb11c8"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a1a18aca-bd4c-02a5-d2b5-eb8cd22bd042"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("3232e72f-5a15-5e5b-d072-6f0065f8d82f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("81effa15-eb47-fe85-333f-d7b3cda1869f"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("33ed0796-4421-ce2b-5293-73cc1055a5c5"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d03e665f-6ecc-47b2-7d95-57de5df2c82b"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("341c152d-eee4-1548-9bb7-a232927443ec"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4d247959-0cdd-cca6-9814-bd354ee7748a"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("34def353-6be4-09d2-1af4-58b2064ca602"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("02e46a11-e05e-8317-80f9-d1672bdd960b"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("3a8197d1-14a0-e262-d0b5-499c7c89508b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f61f7ad1-a486-788d-ac0b-3755c529181d"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("3ba3e8ea-0580-018c-cc7b-9a1570c94927"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("27e68165-6b30-5b14-fa12-d434fc5ec322"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("4490b698-e2f3-ff42-09a6-b63582e811f2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("db2b8bf7-12d7-8e29-b6bf-94a59f59789c"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("4aaca861-2f7a-6f73-ceb2-807debf35904"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("aba6fbc6-2cc7-0884-c406-0382c0ea4d7f"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("4c2e28b8-4233-8dae-3cab-066d512d9def"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a6afbdd8-d92c-f61e-ce0c-03036196c13c"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("4fb44f41-431d-ecfb-f9a1-0ae38019ffab"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a71b71a5-d43e-dbca-8091-22f56c01b7b9"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("53656d79-37e4-3ad8-3923-21ae9fca9eaf"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("005e39a2-163e-679d-5d15-308a93e113b6"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("54bc4135-2ca5-3158-0fbb-867506d15dcc"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0f3e32ac-caea-a8ab-c8df-e5d60a89bb14"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("5a4b433c-d194-07b0-535c-a38f59f44f80"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c7654de4-1947-c445-a710-118157151b44"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("5e115d36-04f4-1f81-c0ae-e19b4cd79730"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e6e6f0a1-f741-2c5b-9040-2be3379153cc"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("62322cc3-b265-49c3-2204-615a67f3b4f8"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a71b71a5-d43e-dbca-8091-22f56c01b7b9"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("626186a3-8062-fde6-4c42-09e770f5129a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0addb09b-2bed-0638-0907-93a029f7a584"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("63633d37-bad8-3367-37b0-dc26054f120b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00626bc6-6c23-2661-34a2-793e6d858052"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("65bec82b-4385-a3d5-9777-d188703960e3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5ec83450-d64f-1280-91b2-6aa7cd9e90ea"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("6faeff4b-3550-151e-6cc7-61c3f37a7dda"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("81effa15-eb47-fe85-333f-d7b3cda1869f"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("744fce8b-cbec-7805-c984-cb15c120230b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("7c0a0aac-e497-34fb-3f24-545fcdb75be5"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("74b9474b-fbb4-6861-e031-db7bb4129df2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e6e6f0a1-f741-2c5b-9040-2be3379153cc"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("764daa1c-0a44-e9b0-9180-5368b662f556"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("9cdaa276-b5b6-6f2c-73ba-3c0b2843c8aa"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("76fe4897-ace3-aa61-5672-32c9d82ae89e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e65fb6ad-98f5-fff7-9da3-77d68f910505"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("7b94b21f-d59c-dbfe-939d-95ac6f93564d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("19382a01-4b1a-b59a-cf67-aac328ef75ba"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("7d12e9b9-b6f6-5331-9791-69739959c448"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("87671c6d-1e98-757d-406e-02dfc16f43f9"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("7dac31dc-3b25-bd43-0951-a2104c152b8d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4468d67a-8e58-9863-3f21-9cf8c67091a9"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("7e13c600-a6ce-e8eb-34b3-a0085a6da28d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b9b28aa8-11cf-795b-5a8b-54644268e870"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("7f6f3e80-c4f6-6f7d-94ec-3a7da9bbfebb"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("005e39a2-163e-679d-5d15-308a93e113b6"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("8285c643-bc15-e818-6258-596209ce2011"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00626bc6-6c23-2661-34a2-793e6d858052"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("82ca7182-27fd-1c91-8ad7-146247695eda"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0addb09b-2bed-0638-0907-93a029f7a584"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("879f5075-6751-70f5-6bf6-6af942bb3087"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("81effa15-eb47-fe85-333f-d7b3cda1869f"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("88b82655-0108-dbca-084d-2791f8b0f406"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("eedd60a0-1213-3658-cbc1-c49a516d877c"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("88df3ccf-9ece-0e09-7584-73257e3a39ab"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("07e2bcb8-f997-6e22-5713-10c2498813f1"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("8f7d4f3e-b08e-888d-bef4-b36b011b0000"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("582aa68d-bf01-3db0-27b8-723c15b4b836"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("9411d798-6d08-328e-dffb-a826125e5ea4"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("02e46a11-e05e-8317-80f9-d1672bdd960b"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("95b7cf8e-077e-fbeb-4690-fcf15d092900"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f61f7ad1-a486-788d-ac0b-3755c529181d"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("9d5ea0e9-be06-cc5f-8cff-290fd810816b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4d247959-0cdd-cca6-9814-bd354ee7748a"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("a1a809f2-23dc-b0dd-87cc-4a0c32282bf1"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e65fb6ad-98f5-fff7-9da3-77d68f910505"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("a1f94165-9f4d-ae62-88e5-a6b08e2e1c30"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4d247959-0cdd-cca6-9814-bd354ee7748a"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("a4156bbe-1f9a-fcee-8928-40488feff64a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e6e6f0a1-f741-2c5b-9040-2be3379153cc"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("a431da39-b1bc-9e69-de5e-6b187551e499"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("eedd60a0-1213-3658-cbc1-c49a516d877c"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("a615be8f-4708-4b08-c5f1-ad392b7c69a3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("eedd60a0-1213-3658-cbc1-c49a516d877c"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("ab94b34e-4926-7cbc-d6db-5d237b9f67da"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("57045883-98f3-d735-7c4c-916800f58d88"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("ac3abecd-d216-4643-0ed8-689b627b6342"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0f3e32ac-caea-a8ab-c8df-e5d60a89bb14"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("b47cb68b-9d0d-d9c7-6acc-aac009989a48"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("fe391c82-c075-7dd8-a66c-f96e836c10f3"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("b77491d3-6bc5-c5d4-fb92-076a855cf25d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4468d67a-8e58-9863-3f21-9cf8c67091a9"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("bb7f9694-cd6d-9a62-1055-01d28d82df39"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("57045883-98f3-d735-7c4c-916800f58d88"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("bce6bcdb-eb33-28da-67e5-800859a5d8b2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("eedd60a0-1213-3658-cbc1-c49a516d877c"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("c8d50f00-27df-5768-7e65-3ebb0a549d2b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c7654de4-1947-c445-a710-118157151b44"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("cdd65888-ca18-5307-9a0f-faab877e8b60"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a1a18aca-bd4c-02a5-d2b5-eb8cd22bd042"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("ce8d33d1-d3f0-a397-5eea-61f46cd4449d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("6d42889c-8458-36e5-6e8e-34d78196ad8c"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("cef8542a-3df2-2b9d-10c1-ed2d942ef2c4"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("954522db-ed42-78da-0496-dac0ccd3fbdb"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("cfa2a50f-9fd7-2265-d803-82e999e59281"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b9b28aa8-11cf-795b-5a8b-54644268e870"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("d0af3634-39d0-90a7-d1da-717c75a31269"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("db2b8bf7-12d7-8e29-b6bf-94a59f59789c"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("d1b3c46f-5a39-bb16-d323-cc2168019f8b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4d247959-0cdd-cca6-9814-bd354ee7748a"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("d4070f1a-9c6e-1641-ddac-641c6725334e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("81effa15-eb47-fe85-333f-d7b3cda1869f"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("d653a260-ae7b-99d5-0c39-ebd82425d7c9"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4d247959-0cdd-cca6-9814-bd354ee7748a"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("de452359-c746-a34b-a5af-8e3671804c3a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d03e665f-6ecc-47b2-7d95-57de5df2c82b"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("e450332e-d147-3398-4da2-079be6860f4e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("005e39a2-163e-679d-5d15-308a93e113b6"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("e73bc45a-7b13-64ba-1318-bb20d7d476ef"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4468d67a-8e58-9863-3f21-9cf8c67091a9"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("e9064f36-b33a-3067-f7be-1455a5f5ea5f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("aba6fbc6-2cc7-0884-c406-0382c0ea4d7f"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("eb1516e0-66fd-33bd-2339-9085f4e2562e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a6afbdd8-d92c-f61e-ce0c-03036196c13c"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("eb2c139c-974d-fb96-90c5-d1b1f154984d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("db2b8bf7-12d7-8e29-b6bf-94a59f59789c"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("ebc3e27c-d2d5-99dd-2d27-eeda759df4f8"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("005e39a2-163e-679d-5d15-308a93e113b6"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("ec5ecd70-f939-6562-9996-72720246464d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("9123aaf7-8468-e8bc-8e2e-2efd3cc00b63"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("ed315550-b5e9-6218-73b1-734754299887"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00626bc6-6c23-2661-34a2-793e6d858052"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("ed3371f5-2cf5-9676-e85a-483c0365bdaf"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00626bc6-6c23-2661-34a2-793e6d858052"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("ed91170a-7b37-a71b-80d9-ade32971dc00"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a6afbdd8-d92c-f61e-ce0c-03036196c13c"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("ee614f2e-20f9-f644-2de3-73d298f2435a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("005e39a2-163e-679d-5d15-308a93e113b6"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("f04039d6-2f8b-7f1b-a049-d9bb43058a07"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("7ba22f42-c829-1e1e-ca45-045bd517d467"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("f3e4bb80-d306-c438-8600-a485bc9806cf"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4d247959-0cdd-cca6-9814-bd354ee7748a"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("f6f64512-e5c8-78df-25fa-a72b197c04ea"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("954522db-ed42-78da-0496-dac0ccd3fbdb"), new Guid("a1111111-1111-1111-1111-111111111105") },
                    { new Guid("fb04dbf4-01fc-70d9-da0c-90dcd94b5b00"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4468d67a-8e58-9863-3f21-9cf8c67091a9"), new Guid("a1111111-1111-1111-1111-111111111103") }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "AppUserId", "AssignedAt", "AssignedByUserId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("c3333333-3333-3333-3333-333333333306"), new Guid("b2222222-2222-2222-2222-222222222206"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("c3333333-3333-3333-3333-333333333307"), new Guid("b2222222-2222-2222-2222-222222222207"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("c3333333-3333-3333-3333-333333333308"), new Guid("b2222222-2222-2222-2222-222222222208"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("a1111111-1111-1111-1111-111111111108") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_AppUserId",
                table: "RefreshTokens",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_CreatedAt",
                table: "SecurityAuditEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_EventType",
                table: "SecurityAuditEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_UserId",
                table: "SecurityAuditEvents",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SecurityAuditEvents");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users");

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("c3333333-3333-3333-3333-333333333306"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("c3333333-3333-3333-3333-333333333307"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: new Guid("c3333333-3333-3333-3333-333333333308"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1111111-1111-1111-1111-111111111106"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1111111-1111-1111-1111-111111111107"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1111111-1111-1111-1111-111111111108"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("b2222222-2222-2222-2222-222222222206"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("b2222222-2222-2222-2222-222222222207"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("b2222222-2222-2222-2222-222222222208"));

            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "AssignedByUserId",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "IsSystemRole",
                table: "Roles");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1111111-1111-1111-1111-111111111102"),
                column: "Description",
                value: "Teknik tasarım ve mimari değerlendirme yapan kullanıcı.");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1111111-1111-1111-1111-111111111104"),
                column: "Description",
                value: "Doğrulama ve kalite kontrol yapan kullanıcı.");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
