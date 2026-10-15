using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConceptSessionScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sessions_scope",
                table: "sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sessions_scope_type",
                table: "sessions");

            migrationBuilder.AddColumn<string>(
                name: "scope_concept_slug",
                table: "sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sessions_scope",
                table: "sessions",
                sql: "(scope_type = 'all' AND scope_domain_slug IS NULL AND scope_topic_slug IS NULL AND scope_concept_slug IS NULL) OR (scope_type = 'domain' AND scope_domain_slug IS NOT NULL AND scope_topic_slug IS NULL AND scope_concept_slug IS NULL) OR (scope_type = 'topic' AND scope_domain_slug IS NOT NULL AND scope_topic_slug IS NOT NULL AND scope_concept_slug IS NULL) OR (scope_type = 'concept' AND scope_domain_slug IS NOT NULL AND scope_topic_slug IS NOT NULL AND scope_concept_slug IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sessions_scope_type",
                table: "sessions",
                sql: "scope_type IN ('all','domain','topic','concept')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sessions_scope",
                table: "sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sessions_scope_type",
                table: "sessions");

            // Keep concept sessions (and their attempts) by widening them to their topic before the old checks return.
            migrationBuilder.Sql("UPDATE sessions SET scope_type = 'topic', scope_concept_slug = NULL WHERE scope_type = 'concept';");

            migrationBuilder.DropColumn(
                name: "scope_concept_slug",
                table: "sessions");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sessions_scope",
                table: "sessions",
                sql: "(scope_type = 'all' AND scope_domain_slug IS NULL AND scope_topic_slug IS NULL) OR (scope_type = 'domain' AND scope_domain_slug IS NOT NULL AND scope_topic_slug IS NULL) OR (scope_type = 'topic' AND scope_domain_slug IS NOT NULL AND scope_topic_slug IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sessions_scope_type",
                table: "sessions",
                sql: "scope_type IN ('all','domain','topic')");
        }
    }
}
