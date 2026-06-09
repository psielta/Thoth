using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thoth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFutureTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FutureTaskId",
                table: "prompts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "future_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkingDirectoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IssueGithubId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_future_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_future_tasks_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_future_tasks_working_directories_WorkingDirectoryId",
                        column: x => x.WorkingDirectoryId,
                        principalTable: "working_directories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "future_task_labels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FutureTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_future_task_labels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_future_task_labels_future_tasks_FutureTaskId",
                        column: x => x.FutureTaskId,
                        principalTable: "future_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_prompts_FutureTaskId",
                table: "prompts",
                column: "FutureTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_future_task_labels_FutureTaskId_Label",
                table: "future_task_labels",
                columns: new[] { "FutureTaskId", "Label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_future_tasks_OwnerId",
                table: "future_tasks",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_future_tasks_WorkingDirectoryId_Status",
                table: "future_tasks",
                columns: new[] { "WorkingDirectoryId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_future_tasks_WorkingDirectoryId_UpdatedAtUtc",
                table: "future_tasks",
                columns: new[] { "WorkingDirectoryId", "UpdatedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_prompts_future_tasks_FutureTaskId",
                table: "prompts",
                column: "FutureTaskId",
                principalTable: "future_tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_prompts_future_tasks_FutureTaskId",
                table: "prompts");

            migrationBuilder.DropTable(
                name: "future_task_labels");

            migrationBuilder.DropTable(
                name: "future_tasks");

            migrationBuilder.DropIndex(
                name: "IX_prompts_FutureTaskId",
                table: "prompts");

            migrationBuilder.DropColumn(
                name: "FutureTaskId",
                table: "prompts");
        }
    }
}
