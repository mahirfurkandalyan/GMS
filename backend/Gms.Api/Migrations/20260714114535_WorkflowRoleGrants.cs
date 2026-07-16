using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gms.Api.Migrations
{
    /// <inheritdoc />
    public partial class WorkflowRoleGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "AssignedAt", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("06e22396-75c8-0f4e-a778-4f34ebcd0a40"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("75c444a5-a5ca-bc76-74a2-4285230caabb"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("09d3d0c0-3b02-0828-dead-e7595924b9a5"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("18c14756-50e3-7391-218e-04ce74204c2c"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("22bd4d9e-d84a-b664-14d0-04b6b0270ca2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("38e49ddd-d6dd-6da1-d7c5-2f6f40a89a89"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("2ea24dc6-8bff-ac9d-228e-3fe369971d69"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("38e49ddd-d6dd-6da1-d7c5-2f6f40a89a89"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("32c3a967-b6c5-4e03-8e68-7a241b139dc0"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("38e49ddd-d6dd-6da1-d7c5-2f6f40a89a89"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("3a63266e-97a9-656d-4117-982042d57187"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("38e49ddd-d6dd-6da1-d7c5-2f6f40a89a89"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("53fc5e67-d14f-c940-39d5-1764c5301b3d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("18c14756-50e3-7391-218e-04ce74204c2c"), new Guid("a1111111-1111-1111-1111-111111111103") },
                    { new Guid("57f2286c-802c-b92b-826c-a7197ac0a4be"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("75c444a5-a5ca-bc76-74a2-4285230caabb"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("5915ad5d-0774-7f7f-7b80-bf73e549c7a0"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("18c14756-50e3-7391-218e-04ce74204c2c"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("5a59043a-415a-a007-1815-172e5a820524"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("38e49ddd-d6dd-6da1-d7c5-2f6f40a89a89"), new Guid("a1111111-1111-1111-1111-111111111101") },
                    { new Guid("7b94c2d5-5d73-9f10-72bf-00ee08311299"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e876a387-8528-144e-e79e-3e65a4b5f8a1"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("7f9b04c0-191e-a947-5261-9b6a161659e0"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("38e49ddd-d6dd-6da1-d7c5-2f6f40a89a89"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("822463a7-7266-8aff-2da0-ad076bae58fd"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("18c14756-50e3-7391-218e-04ce74204c2c"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("8bd82ad1-11ea-55d5-72f3-99986adcc075"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("18c14756-50e3-7391-218e-04ce74204c2c"), new Guid("a1111111-1111-1111-1111-111111111107") },
                    { new Guid("98de91ad-d57e-218e-fa1f-6d8c8f689c54"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("75c444a5-a5ca-bc76-74a2-4285230caabb"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("9a100968-b4a5-80af-c8da-7cd5a061d900"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("38e49ddd-d6dd-6da1-d7c5-2f6f40a89a89"), new Guid("a1111111-1111-1111-1111-111111111106") },
                    { new Guid("a2d56cdf-779a-fbba-c5fe-2ae0e5105042"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("18c14756-50e3-7391-218e-04ce74204c2c"), new Guid("a1111111-1111-1111-1111-111111111104") },
                    { new Guid("b14228fc-22dd-3bfa-f885-af2421af1ca6"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("18c14756-50e3-7391-218e-04ce74204c2c"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("bfd5be04-2744-cdfa-95ee-59a2a47dcb95"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e876a387-8528-144e-e79e-3e65a4b5f8a1"), new Guid("a1111111-1111-1111-1111-111111111102") },
                    { new Guid("cb77051a-5244-cfc2-7ee1-f32cf1eb3aef"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("87ab24e6-ed75-904c-49ad-015649535f0d"), new Guid("a1111111-1111-1111-1111-111111111108") },
                    { new Guid("ddf7529c-ca57-62d2-71e7-450c1f2952f2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e876a387-8528-144e-e79e-3e65a4b5f8a1"), new Guid("a1111111-1111-1111-1111-111111111106") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("06e22396-75c8-0f4e-a778-4f34ebcd0a40"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("09d3d0c0-3b02-0828-dead-e7595924b9a5"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("22bd4d9e-d84a-b664-14d0-04b6b0270ca2"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("2ea24dc6-8bff-ac9d-228e-3fe369971d69"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("32c3a967-b6c5-4e03-8e68-7a241b139dc0"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("3a63266e-97a9-656d-4117-982042d57187"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("53fc5e67-d14f-c940-39d5-1764c5301b3d"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("57f2286c-802c-b92b-826c-a7197ac0a4be"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("5915ad5d-0774-7f7f-7b80-bf73e549c7a0"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("5a59043a-415a-a007-1815-172e5a820524"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("7b94c2d5-5d73-9f10-72bf-00ee08311299"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("7f9b04c0-191e-a947-5261-9b6a161659e0"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("822463a7-7266-8aff-2da0-ad076bae58fd"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("8bd82ad1-11ea-55d5-72f3-99986adcc075"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("98de91ad-d57e-218e-fa1f-6d8c8f689c54"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("9a100968-b4a5-80af-c8da-7cd5a061d900"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("a2d56cdf-779a-fbba-c5fe-2ae0e5105042"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("b14228fc-22dd-3bfa-f885-af2421af1ca6"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("bfd5be04-2744-cdfa-95ee-59a2a47dcb95"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("cb77051a-5244-cfc2-7ee1-f32cf1eb3aef"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("ddf7529c-ca57-62d2-71e7-450c1f2952f2"));
        }
    }
}
