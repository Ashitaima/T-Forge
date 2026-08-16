using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TForge.Migrations
{
    /// <inheritdoc />
    public partial class FixShadowForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_matches_teams_TeamId",
                table: "matches");

            migrationBuilder.DropForeignKey(
                name: "FK_matches_teams_TeamId1",
                table: "matches");

            migrationBuilder.DropForeignKey(
                name: "FK_players_users_UserId1",
                table: "players");

            migrationBuilder.DropForeignKey(
                name: "FK_teams_users_UserId",
                table: "teams");

            migrationBuilder.DropForeignKey(
                name: "FK_tournaments_users_UserId",
                table: "tournaments");

            migrationBuilder.DropIndex(
                name: "IX_tournaments_UserId",
                table: "tournaments");

            migrationBuilder.DropIndex(
                name: "IX_teams_UserId",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "IX_players_UserId1",
                table: "players");

            migrationBuilder.DropIndex(
                name: "IX_matches_TeamId",
                table: "matches");

            migrationBuilder.DropIndex(
                name: "IX_matches_TeamId1",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "players");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "TeamId1",
                table: "matches");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "tournaments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "teams",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "players",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamId",
                table: "matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamId1",
                table: "matches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tournaments_UserId",
                table: "tournaments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_teams_UserId",
                table: "teams",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_players_UserId1",
                table: "players",
                column: "UserId1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matches_TeamId",
                table: "matches",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_matches_TeamId1",
                table: "matches",
                column: "TeamId1");

            migrationBuilder.AddForeignKey(
                name: "FK_matches_teams_TeamId",
                table: "matches",
                column: "TeamId",
                principalTable: "teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_matches_teams_TeamId1",
                table: "matches",
                column: "TeamId1",
                principalTable: "teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_players_users_UserId1",
                table: "players",
                column: "UserId1",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_teams_users_UserId",
                table: "teams",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tournaments_users_UserId",
                table: "tournaments",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}
