using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thoth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "working_directories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    AbsolutePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    RespectGitignore = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_working_directories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_working_directories_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prompts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkingDirectoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    TargetAgent = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentVersion = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prompts_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_prompts_working_directories_WorkingDirectoryId",
                        column: x => x.WorkingDirectoryId,
                        principalTable: "working_directories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "linked_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkingDirectoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelativePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linked_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_linked_documents_prompts_PromptId",
                        column: x => x.PromptId,
                        principalTable: "prompts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_linked_documents_working_directories_WorkingDirectoryId",
                        column: x => x.WorkingDirectoryId,
                        principalTable: "working_directories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prompt_file_references",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelativePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    RawMention = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Exists = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_file_references", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prompt_file_references_prompts_PromptId",
                        column: x => x.PromptId,
                        principalTable: "prompts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prompt_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    TargetAgent = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ChangeNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prompt_versions_prompts_PromptId",
                        column: x => x.PromptId,
                        principalTable: "prompts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_linked_documents_PromptId",
                table: "linked_documents",
                column: "PromptId");

            migrationBuilder.CreateIndex(
                name: "IX_linked_documents_WorkingDirectoryId",
                table: "linked_documents",
                column: "WorkingDirectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_prompt_file_references_PromptId_RelativePath",
                table: "prompt_file_references",
                columns: new[] { "PromptId", "RelativePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prompt_versions_PromptId_VersionNumber",
                table: "prompt_versions",
                columns: new[] { "PromptId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prompts_OwnerId",
                table: "prompts",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_prompts_WorkingDirectoryId_Status",
                table: "prompts",
                columns: new[] { "WorkingDirectoryId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_prompts_WorkingDirectoryId_UpdatedAtUtc",
                table: "prompts",
                columns: new[] { "WorkingDirectoryId", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_working_directories_OwnerId_AbsolutePath",
                table: "working_directories",
                columns: new[] { "OwnerId", "AbsolutePath" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "linked_documents");

            migrationBuilder.DropTable(
                name: "prompt_file_references");

            migrationBuilder.DropTable(
                name: "prompt_versions");

            migrationBuilder.DropTable(
                name: "prompts");

            migrationBuilder.DropTable(
                name: "working_directories");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
