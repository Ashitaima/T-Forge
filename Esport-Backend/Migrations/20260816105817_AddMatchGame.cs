using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TForge.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Game",
                table: "matches",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Наявні матчі успадковують дисципліну свого турніру — те саме правило,
            // за яким сервер заповнює її для нових матчів.
            migrationBuilder.Sql(
                @"UPDATE matches SET ""Game"" = t.""Game""
                  FROM tournaments t
                  WHERE matches.""TournamentId"" = t.""Id"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Game",
                table: "matches");
        }
    }
}
