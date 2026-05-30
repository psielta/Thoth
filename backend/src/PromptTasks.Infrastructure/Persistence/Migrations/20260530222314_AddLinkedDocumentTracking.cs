using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PromptTasks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkedDocumentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_linked_documents_PromptId",
                table: "linked_documents");

            migrationBuilder.RenameColumn(
                name: "RelativePath",
                table: "linked_documents",
                newName: "AbsolutePath");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkingDirectoryId",
                table: "linked_documents",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "AbsolutePathKey",
                table: "linked_documents",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE linked_documents SET \"AbsolutePathKey\" = lower(\"AbsolutePath\") WHERE \"AbsolutePathKey\" = '';");

            migrationBuilder.AddColumn<int>(
                name: "CurrentVersion",
                table: "linked_documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "linked_documents",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocumentType",
                table: "linked_documents",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "linked_documents",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSyncedAtUtc",
                table: "linked_documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "linked_documents",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "linked_document_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linked_document_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_linked_document_versions_linked_documents_LinkedDocumentId",
                        column: x => x.LinkedDocumentId,
                        principalTable: "linked_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_linked_documents_PromptId_AbsolutePathKey",
                table: "linked_documents",
                columns: new[] { "PromptId", "AbsolutePathKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linked_documents_Status",
                table: "linked_documents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_linked_document_versions_LinkedDocumentId_CreatedAtUtc",
                table: "linked_document_versions",
                columns: new[] { "LinkedDocumentId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_linked_document_versions_LinkedDocumentId_VersionNumber",
                table: "linked_document_versions",
                columns: new[] { "LinkedDocumentId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "linked_document_versions");

            migrationBuilder.DropIndex(
                name: "IX_linked_documents_PromptId_AbsolutePathKey",
                table: "linked_documents");

            migrationBuilder.DropIndex(
                name: "IX_linked_documents_Status",
                table: "linked_documents");

            migrationBuilder.DropColumn(
                name: "AbsolutePathKey",
                table: "linked_documents");

            migrationBuilder.DropColumn(
                name: "CurrentVersion",
                table: "linked_documents");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "linked_documents");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "linked_documents");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "linked_documents");

            migrationBuilder.DropColumn(
                name: "LastSyncedAtUtc",
                table: "linked_documents");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "linked_documents");

            migrationBuilder.RenameColumn(
                name: "AbsolutePath",
                table: "linked_documents",
                newName: "RelativePath");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkingDirectoryId",
                table: "linked_documents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_linked_documents_PromptId",
                table: "linked_documents",
                column: "PromptId");
        }
    }
}
