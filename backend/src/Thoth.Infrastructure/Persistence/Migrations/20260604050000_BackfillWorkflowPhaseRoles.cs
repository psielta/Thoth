using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thoth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillWorkflowPhaseRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE workflow_template_phases
                SET "Role" = CASE "Name"
                    WHEN 'Engenharia de prompt' THEN 1
                    WHEN 'Planejamento' THEN 2
                    WHEN 'Revisão do plano' THEN 3
                    WHEN 'Correção do plano' THEN 4
                    WHEN 'Implementação' THEN 5
                    WHEN 'Revisão de código' THEN 6
                    WHEN 'Correção da revisão' THEN 7
                    WHEN 'Teste prático' THEN 8
                    WHEN 'Atualizar branch com main' THEN 9
                    WHEN 'Commit/Merge' THEN 10
                    ELSE "Role"
                END
                WHERE "Role" IS NULL;

                UPDATE prompt_workflow_phases
                SET "Role" = CASE "Name"
                    WHEN 'Engenharia de prompt' THEN 1
                    WHEN 'Planejamento' THEN 2
                    WHEN 'Revisão do plano' THEN 3
                    WHEN 'Correção do plano' THEN 4
                    WHEN 'Implementação' THEN 5
                    WHEN 'Revisão de código' THEN 6
                    WHEN 'Correção da revisão' THEN 7
                    WHEN 'Teste prático' THEN 8
                    WHEN 'Atualizar branch com main' THEN 9
                    WHEN 'Commit/Merge' THEN 10
                    ELSE "Role"
                END
                WHERE "Role" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
