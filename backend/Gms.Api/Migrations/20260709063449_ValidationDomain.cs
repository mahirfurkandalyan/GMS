using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gms.Api.Migrations
{
    /// <inheritdoc />
    public partial class ValidationDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ValidationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeploymentRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValidationNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ValidationType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverallResult = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationRuns_DeploymentRuns_DeploymentRunId",
                        column: x => x.DeploymentRunId,
                        principalTable: "DeploymentRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ValidationRuns_Users_ValidatedByUserId",
                        column: x => x.ValidatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ValidationChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValidationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckOrder = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ExpectedResult = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActualResult = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationChecks_Users_ExecutedByUserId",
                        column: x => x.ExecutedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ValidationChecks_ValidationRuns_ValidationRunId",
                        column: x => x.ValidationRunId,
                        principalTable: "ValidationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValidationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValidationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationEvents_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ValidationEvents_ValidationRuns_ValidationRunId",
                        column: x => x.ValidationRunId,
                        principalTable: "ValidationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValidationEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValidationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationEvidences_ValidationRuns_ValidationRunId",
                        column: x => x.ValidationRunId,
                        principalTable: "ValidationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ValidationChecks_ExecutedByUserId",
                table: "ValidationChecks",
                column: "ExecutedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationChecks_ValidationRunId_CheckOrder",
                table: "ValidationChecks",
                columns: new[] { "ValidationRunId", "CheckOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValidationEvents_ActorUserId",
                table: "ValidationEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationEvents_CreatedAt",
                table: "ValidationEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationEvents_ValidationRunId",
                table: "ValidationEvents",
                column: "ValidationRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationEvidences_ValidationRunId",
                table: "ValidationEvidences",
                column: "ValidationRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRuns_CreatedAt",
                table: "ValidationRuns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRuns_DeploymentRunId",
                table: "ValidationRuns",
                column: "DeploymentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRuns_Status",
                table: "ValidationRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRuns_ValidatedByUserId",
                table: "ValidationRuns",
                column: "ValidatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRuns_ValidationNo",
                table: "ValidationRuns",
                column: "ValidationNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ValidationChecks");

            migrationBuilder.DropTable(
                name: "ValidationEvents");

            migrationBuilder.DropTable(
                name: "ValidationEvidences");

            migrationBuilder.DropTable(
                name: "ValidationRuns");
        }
    }
}
