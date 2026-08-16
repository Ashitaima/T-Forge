using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TForge.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchTrackerUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrackerUrl",
                table: "matches",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrackerUrl",
                table: "matches");
        }
    }
}
