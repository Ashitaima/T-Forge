using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TForge.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TournamentId",
                table: "matches",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "match_challenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChallengerTeamId = table.Column<int>(type: "integer", nullable: false),
                    OpponentTeamId = table.Column<int>(type: "integer", nullable: false),
                    Game = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProposedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "BO1"),
                    Message = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    InitiatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RespondedByUserId = table.Column<int>(type: "integer", nullable: true),
                    MatchId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_challenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_match_challenges_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_match_challenges_teams_ChallengerTeamId",
                        column: x => x.ChallengerTeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_match_challenges_teams_OpponentTeamId",
                        column: x => x.OpponentTeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_match_challenges_ChallengerTeamId_OpponentTeamId",
                table: "match_challenges",
                columns: new[] { "ChallengerTeamId", "OpponentTeamId" },
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_match_challenges_MatchId",
                table: "match_challenges",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_match_challenges_OpponentTeamId",
                table: "match_challenges",
                column: "OpponentTeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "match_challenges");

            migrationBuilder.AlterColumn<int>(
                name: "TournamentId",
                table: "matches",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
