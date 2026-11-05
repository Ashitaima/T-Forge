using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TForge.Migrations
{
    /// <inheritdoc />
    public partial class AddChessPredictionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeControl",
                table: "tournaments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "player_game_profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeControl",
                table: "duels",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WhitePlayerId",
                table: "duels",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeControl",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "player_game_profiles");

            migrationBuilder.DropColumn(
                name: "TimeControl",
                table: "duels");

            migrationBuilder.DropColumn(
                name: "WhitePlayerId",
                table: "duels");
        }
    }
}
