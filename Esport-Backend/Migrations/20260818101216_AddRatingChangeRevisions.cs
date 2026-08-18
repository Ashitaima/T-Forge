using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TForge.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingChangeRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_team_rating_changes_TeamId_MatchId",
                table: "team_rating_changes");

            migrationBuilder.DropIndex(
                name: "IX_player_rating_changes_PlayerId_MatchId",
                table: "player_rating_changes");

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "team_rating_changes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RecordedWinnerTeamId",
                table: "team_rating_changes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "team_rating_changes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "player_rating_changes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "player_rating_changes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Рядки, що вже лежать у базі, зроблено до появи дискримінатора:
            // усі вони — нарахування нульової ревізії. Без цього кроку
            // RateMatchAsync вважав би журнал порожнім і нарахував би все вдруге.
            migrationBuilder.Sql(
                "UPDATE team_rating_changes SET \"Kind\" = 'Applied' WHERE \"Kind\" = '';");

            migrationBuilder.Sql(
                "UPDATE player_rating_changes SET \"Kind\" = 'Applied' WHERE \"Kind\" = '';");

            // Результат, з якого рахували, відновлюємо з самого матчу: інших
            // нарахувань, окрім поточного результату, у старій базі бути не могло.
            migrationBuilder.Sql(
                "UPDATE team_rating_changes c SET \"RecordedWinnerTeamId\" = m.\"WinnerTeamId\" " +
                "FROM matches m WHERE m.\"Id\" = c.\"MatchId\" " +
                "AND c.\"RecordedWinnerTeamId\" IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_team_rating_changes_TeamId_MatchId_Revision",
                table: "team_rating_changes",
                columns: new[] { "TeamId", "MatchId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_rating_changes_PlayerId_MatchId_Revision",
                table: "player_rating_changes",
                columns: new[] { "PlayerId", "MatchId", "Revision" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_team_rating_changes_TeamId_MatchId_Revision",
                table: "team_rating_changes");

            migrationBuilder.DropIndex(
                name: "IX_player_rating_changes_PlayerId_MatchId_Revision",
                table: "player_rating_changes");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "team_rating_changes");

            migrationBuilder.DropColumn(
                name: "RecordedWinnerTeamId",
                table: "team_rating_changes");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "team_rating_changes");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "player_rating_changes");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "player_rating_changes");

            migrationBuilder.CreateIndex(
                name: "IX_team_rating_changes_TeamId_MatchId",
                table: "team_rating_changes",
                columns: new[] { "TeamId", "MatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_rating_changes_PlayerId_MatchId",
                table: "player_rating_changes",
                columns: new[] { "PlayerId", "MatchId" },
                unique: true);
        }
    }
}
