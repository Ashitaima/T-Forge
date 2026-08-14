using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TForge.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchPlayerTeamId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Додаємо колонку як nullable, щоб наявні рядки не отримали team 0
            migrationBuilder.AddColumn<int>(
                name: "TeamId",
                table: "match_players",
                type: "integer",
                nullable: true);

            // 2. Заповнюємо історію: якщо поточна команда гравця брала участь у матчі —
            //    беремо її, інакше приймаємо домашню команду як найкраще припущення
            migrationBuilder.Sql(@"
                UPDATE match_players mp
                SET ""TeamId"" = COALESCE(
                    (SELECT p.""TeamId""
                     FROM players p
                     JOIN matches m ON m.""Id"" = mp.""MatchId""
                     WHERE p.""Id"" = mp.""PlayerId""
                       AND (p.""TeamId"" = m.""HomeTeamId"" OR p.""TeamId"" = m.""AwayTeamId"")),
                    (SELECT m.""HomeTeamId"" FROM matches m WHERE m.""Id"" = mp.""MatchId"")
                );
            ");

            // 3. Тепер колонка гарантовано заповнена — робимо її обовʼязковою
            migrationBuilder.AlterColumn<int>(
                name: "TeamId",
                table: "match_players",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_match_players_TeamId",
                table: "match_players",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_match_players_teams_TeamId",
                table: "match_players",
                column: "TeamId",
                principalTable: "teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_match_players_teams_TeamId",
                table: "match_players");

            migrationBuilder.DropIndex(
                name: "IX_match_players_TeamId",
                table: "match_players");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "match_players");
        }
    }
}
