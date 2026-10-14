using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainTopicUkTitles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "title_uk",
                table: "topics",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title_uk",
                table: "domains",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "title_uk",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "title_uk",
                table: "domains");
        }
    }
}
