using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thoth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPromptWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "prompt_workflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentPhaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentPhaseName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CurrentPhaseColor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CurrentActor = table.Column<int>(type: "integer", nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EnteredCurrentPhaseAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_workflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prompt_workflows_prompts_PromptId",
                        column: x => x.PromptId,
                        principalTable: "prompts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_templates_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prompt_workflow_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptWorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    PhaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    PhaseNameSnapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Actor = table.Column<int>(type: "integer", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_workflow_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prompt_workflow_events_prompt_workflows_PromptWorkflowId",
                        column: x => x.PromptWorkflowId,
                        principalTable: "prompt_workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prompt_workflow_phases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptWorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DefaultActor = table.Column<int>(type: "integer", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_workflow_phases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prompt_workflow_phases_prompt_workflows_PromptWorkflowId",
                        column: x => x.PromptWorkflowId,
                        principalTable: "prompt_workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_template_phases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DefaultActor = table.Column<int>(type: "integer", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_template_phases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_template_phases_workflow_templates_WorkflowTemplat~",
                        column: x => x.WorkflowTemplateId,
                        principalTable: "workflow_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_prompt_workflow_events_PromptWorkflowId_OccurredAtUtc",
                table: "prompt_workflow_events",
                columns: new[] { "PromptWorkflowId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_prompt_workflow_phases_PromptWorkflowId_OrderIndex",
                table: "prompt_workflow_phases",
                columns: new[] { "PromptWorkflowId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_prompt_workflows_PromptId",
                table: "prompt_workflows",
                column: "PromptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_template_phases_WorkflowTemplateId_OrderIndex",
                table: "workflow_template_phases",
                columns: new[] { "WorkflowTemplateId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_templates_OwnerId",
                table: "workflow_templates",
                column: "OwnerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prompt_workflow_events");

            migrationBuilder.DropTable(
                name: "prompt_workflow_phases");

            migrationBuilder.DropTable(
                name: "workflow_template_phases");

            migrationBuilder.DropTable(
                name: "prompt_workflows");

            migrationBuilder.DropTable(
                name: "workflow_templates");
        }
    }
}
