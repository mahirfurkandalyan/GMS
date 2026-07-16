using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gms.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Environments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Environments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Environments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Releases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PlannedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                table: "Customers",
                columns: new[] { "Id", "Code", "CreatedAt", "Name", "Status" },
                values: new object[,]
                {
                    { new Guid("d4444444-4444-4444-4444-444444444401"), "ABDI", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Abdi İbrahim", "Active" },
                    { new Guid("d4444444-4444-4444-4444-444444444402"), "BILIM", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bilim İlaç", "Active" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("a1111111-1111-1111-1111-111111111101"), "Değişiklik/sürüm talebi oluşturan kullanıcı.", "Requester" },
                    { new Guid("a1111111-1111-1111-1111-111111111102"), "Teknik tasarım ve mimari değerlendirme yapan kullanıcı.", "Architect" },
                    { new Guid("a1111111-1111-1111-1111-111111111103"), "Onaylanan değişiklikleri yürüten kullanıcı.", "Executor" },
                    { new Guid("a1111111-1111-1111-1111-111111111104"), "Doğrulama ve kalite kontrol yapan kullanıcı.", "QA" },
                    { new Guid("a1111111-1111-1111-1111-111111111105"), "Sistem yöneticisi — tüm yetkiler.", "Admin" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "Status" },
                values: new object[,]
                {
                    { new Guid("b2222222-2222-2222-2222-222222222201"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "requester@gms.local", "Requester User", "Active" },
                    { new Guid("b2222222-2222-2222-2222-222222222202"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "architect@gms.local", "Architect User", "Active" },
                    { new Guid("b2222222-2222-2222-2222-222222222203"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "executor@gms.local", "Executor User", "Active" },
                    { new Guid("b2222222-2222-2222-2222-222222222204"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "qa@gms.local", "QA Specialist", "Active" },
                    { new Guid("b2222222-2222-2222-2222-222222222205"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@gms.local", "System Administrator", "Active" }
                });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Code", "CreatedAt", "CustomerId", "Description", "Name", "Status" },
                values: new object[,]
                {
                    { new Guid("e5555555-5555-5555-5555-555555555501"), "EBR-MIG", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d4444444-4444-4444-4444-444444444401"), "Elektronik Batch Record geçiş projesi.", "EBR Migration", "Active" },
                    { new Guid("e5555555-5555-5555-5555-555555555502"), "MES-UPG", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d4444444-4444-4444-4444-444444444402"), "MES sürüm yükseltme projesi.", "MES Upgrade", "Active" }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "AppUserId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("c3333333-3333-3333-3333-333333333301"), new Guid("b2222222-2222-2222-2222-222222222201"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("c3333333-3333-3333-3333-333333333302"), new Guid("b2222222-2222-2222-2222-222222222202"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("c3333333-3333-3333-3333-333333333303"), new Guid("b2222222-2222-2222-2222-222222222203"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("c3333333-3333-3333-3333-333333333304"), new Guid("b2222222-2222-2222-2222-222222222204"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("c3333333-3333-3333-3333-333333333305"), new Guid("b2222222-2222-2222-2222-222222222205"), new Guid("a1111111-1111-1111-1111-111111111105") }
                });

            migrationBuilder.InsertData(
                table: "Environments",
                columns: new[] { "Id", "CreatedAt", "Name", "ProjectId", "Status", "Type" },
                values: new object[,]
                {
                    { new Guid("f6666666-6666-6666-6666-666666666601"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DEV", new Guid("e5555555-5555-5555-5555-555555555501"), "Active", "Geliştirme" },
                    { new Guid("f6666666-6666-6666-6666-666666666602"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TEST", new Guid("e5555555-5555-5555-5555-555555555501"), "Active", "Test" },
                    { new Guid("f6666666-6666-6666-6666-666666666603"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "UAT", new Guid("e5555555-5555-5555-5555-555555555501"), "Active", "Kullanıcı Kabul" },
                    { new Guid("f6666666-6666-6666-6666-666666666604"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PROD", new Guid("e5555555-5555-5555-5555-555555555501"), "Active", "Üretim" },
                    { new Guid("f6666666-6666-6666-6666-666666666611"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DEV", new Guid("e5555555-5555-5555-5555-555555555502"), "Active", "Geliştirme" },
                    { new Guid("f6666666-6666-6666-6666-666666666612"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TEST", new Guid("e5555555-5555-5555-5555-555555555502"), "Active", "Test" },
                    { new Guid("f6666666-6666-6666-6666-666666666613"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "UAT", new Guid("e5555555-5555-5555-5555-555555555502"), "Active", "Kullanıcı Kabul" },
                    { new Guid("f6666666-6666-6666-6666-666666666614"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PROD", new Guid("e5555555-5555-5555-5555-555555555502"), "Active", "Üretim" }
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
                name: "IX_Customers_Code",
                table: "Customers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Environments_ProjectId",
                table: "Environments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Code",
                table: "Projects",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CustomerId",
                table: "Projects",
                column: "CustomerId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_AppUserId_RoleId",
                table: "UserRoles",
                columns: new[] { "AppUserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Releases");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Environments");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
