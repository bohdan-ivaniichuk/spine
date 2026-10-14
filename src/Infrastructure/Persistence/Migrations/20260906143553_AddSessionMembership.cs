using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewPrep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "new_limit",
                table: "sessions",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<string[]>(
                name: "question_ids",
                table: "sessions",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<int>(
                name: "total_limit",
                table: "sessions",
                type: "integer",
                nullable: false,
                defaultValue: 20);

            migrationBuilder.CreateIndex(
                name: "ix_attempts_session_id_item_id",
                table: "attempts",
                columns: new[] { "session_id", "item_id" },
                unique: true,
                filter: "session_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_attempts_session_id_item_id",
                table: "attempts");

            migrationBuilder.DropColumn(
                name: "new_limit",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "question_ids",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "total_limit",
                table: "sessions");
        }
    }
}
