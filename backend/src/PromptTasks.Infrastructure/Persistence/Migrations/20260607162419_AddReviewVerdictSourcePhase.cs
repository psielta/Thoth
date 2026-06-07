using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PromptTasks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewVerdictSourcePhase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewVerdictSourcePhaseName",
                table: "prompt_workflows",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewVerdictSourcePhaseName",
                table: "prompt_workflows");
        }
    }
}
