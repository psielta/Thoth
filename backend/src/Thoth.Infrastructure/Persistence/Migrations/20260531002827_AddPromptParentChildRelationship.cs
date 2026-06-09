using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thoth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPromptParentChildRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentPromptId",
                table: "prompts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_prompts_ParentPromptId_UpdatedAtUtc",
                table: "prompts",
                columns: new[] { "ParentPromptId", "UpdatedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_prompts_prompts_ParentPromptId",
                table: "prompts",
                column: "ParentPromptId",
                principalTable: "prompts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_prompts_prompts_ParentPromptId",
                table: "prompts");

            migrationBuilder.DropIndex(
                name: "IX_prompts_ParentPromptId_UpdatedAtUtc",
                table: "prompts");

            migrationBuilder.DropColumn(
                name: "ParentPromptId",
                table: "prompts");
        }
    }
}
