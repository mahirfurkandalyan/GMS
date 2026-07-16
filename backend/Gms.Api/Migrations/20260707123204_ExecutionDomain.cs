using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gms.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExecutionDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeploymentRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReleasePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverallResult = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeploymentRuns_ReleasePlans_ReleasePlanId",
                        column: x => x.ReleasePlanId,
                        principalTable: "ReleasePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeploymentRuns_Users_ExecutedByUserId",
                        column: x => x.ExecutedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeploymentEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeploymentRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeploymentEvents_DeploymentRuns_DeploymentRunId",
                        column: x => x.DeploymentRunId,
                        principalTable: "DeploymentRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeploymentEvents_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeploymentSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeploymentRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReleasePlanItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExecutionResult = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RollbackExecuted = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeploymentSteps_DeploymentRuns_DeploymentRunId",
                        column: x => x.DeploymentRunId,
                        principalTable: "DeploymentRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeploymentSteps_ReleasePlanItems_ReleasePlanItemId",
                        column: x => x.ReleasePlanItemId,
                        principalTable: "ReleasePlanItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeploymentSteps_Users_ExecutedByUserId",
                        column: x => x.ExecutedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentEvents_ActorUserId",
                table: "DeploymentEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentEvents_CreatedAt",
                table: "DeploymentEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentEvents_DeploymentRunId",
                table: "DeploymentEvents",
                column: "DeploymentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentRuns_CreatedAt",
                table: "DeploymentRuns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentRuns_ExecutedByUserId",
                table: "DeploymentRuns",
                column: "ExecutedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentRuns_ExecutionNo",
                table: "DeploymentRuns",
                column: "ExecutionNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentRuns_ReleasePlanId",
                table: "DeploymentRuns",
                column: "ReleasePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentRuns_Status",
                table: "DeploymentRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentSteps_DeploymentRunId_StepOrder",
                table: "DeploymentSteps",
                columns: new[] { "DeploymentRunId", "StepOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentSteps_ExecutedByUserId",
                table: "DeploymentSteps",
                column: "ExecutedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentSteps_ReleasePlanItemId",
                table: "DeploymentSteps",
                column: "ReleasePlanItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeploymentEvents");

            migrationBuilder.DropTable(
                name: "DeploymentSteps");

            migrationBuilder.DropTable(
                name: "DeploymentRuns");
        }
    }
}
