using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thoth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiChatAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_chat_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkingDirectoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    PromptId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Temperature = table.Column<double>(type: "double precision", nullable: false),
                    ThinkingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ThinkingBudget = table.Column<int>(type: "integer", nullable: true),
                    ThinkingLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GeminiCacheName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CacheExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CachedThroughSequence = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_chat_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_user_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Temperature = table.Column<double>(type: "double precision", nullable: false),
                    ThinkingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ThinkingBudget = table.Column<int>(type: "integer", nullable: true),
                    ThinkingLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_user_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_chat_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    PromptTokens = table.Column<int>(type: "integer", nullable: true),
                    CandidateTokens = table.Column<int>(type: "integer", nullable: true),
                    CachedTokens = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_chat_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_chat_messages_ai_chat_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ai_chat_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_chat_messages_SessionId_Sequence",
                table: "ai_chat_messages",
                columns: new[] { "SessionId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_chat_sessions_OwnerId_UpdatedAtUtc",
                table: "ai_chat_sessions",
                columns: new[] { "OwnerId", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_chat_sessions_PromptId",
                table: "ai_chat_sessions",
                column: "PromptId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_user_settings_OwnerId",
                table: "ai_user_settings",
                column: "OwnerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_chat_messages");

            migrationBuilder.DropTable(
                name: "ai_user_settings");

            migrationBuilder.DropTable(
                name: "ai_chat_sessions");
        }
    }
}
