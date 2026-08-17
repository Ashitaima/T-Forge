using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TForge.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingsAndTournamentInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInviteOnly",
                table: "tournaments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "player_rating_changes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Game = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    Delta = table.Column<int>(type: "integer", nullable: false),
                    RatingBefore = table.Column<int>(type: "integer", nullable: false),
                    RatingAfter = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_rating_changes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_player_rating_changes_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_player_rating_changes_players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_ratings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Game = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Peak = table.Column<int>(type: "integer", nullable: false),
                    MatchesRated = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_ratings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_player_ratings_players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_rating_changes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Game = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    Delta = table.Column<int>(type: "integer", nullable: false),
                    RatingBefore = table.Column<int>(type: "integer", nullable: false),
                    RatingAfter = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_rating_changes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_rating_changes_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_rating_changes_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_ratings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Game = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Peak = table.Column<int>(type: "integer", nullable: false),
                    MatchesRated = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_ratings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_ratings_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tournament_invitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TournamentId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    InitiatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RespondedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tournament_invitations_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tournament_invitations_tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_rating_changes_MatchId",
                table: "player_rating_changes",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_player_rating_changes_PlayerId_MatchId",
                table: "player_rating_changes",
                columns: new[] { "PlayerId", "MatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_ratings_PlayerId_Game",
                table: "player_ratings",
                columns: new[] { "PlayerId", "Game" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_rating_changes_MatchId",
                table: "team_rating_changes",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_team_rating_changes_TeamId_MatchId",
                table: "team_rating_changes",
                columns: new[] { "TeamId", "MatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_ratings_TeamId_Game",
                table: "team_ratings",
                columns: new[] { "TeamId", "Game" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tournament_invitations_TeamId",
                table: "tournament_invitations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_invitations_TournamentId_TeamId",
                table: "tournament_invitations",
                columns: new[] { "TournamentId", "TeamId" },
                unique: true,
                filter: "\"Status\" = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_rating_changes");

            migrationBuilder.DropTable(
                name: "player_ratings");

            migrationBuilder.DropTable(
                name: "team_rating_changes");

            migrationBuilder.DropTable(
                name: "team_ratings");

            migrationBuilder.DropTable(
                name: "tournament_invitations");

            migrationBuilder.DropColumn(
                name: "IsInviteOnly",
                table: "tournaments");
        }
    }
}
