using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mode",
                table: "sessions",
                type: "text",
                nullable: false,
                defaultValueSql: "'regular'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sessions_mode",
                table: "sessions",
                sql: "mode IN ('regular','weak')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sessions_mode",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "mode",
                table: "sessions");
        }
    }
}
