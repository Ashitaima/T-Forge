using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TForge.Migrations
{
    /// <inheritdoc />
    public partial class AddLftMatchmaking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TeamOpeningId",
                table: "team_membership_requests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "team_openings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Game = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Position = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MinRank = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MinUtcOffsetMinutes = table.Column<int>(type: "integer", nullable: true),
                    MaxUtcOffsetMinutes = table.Column<int>(type: "integer", nullable: true),
                    Pitch = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Open"),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_openings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_openings_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lft_passes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    TeamOpeningId = table.Column<int>(type: "integer", nullable: false),
                    PassedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lft_passes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lft_passes_players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lft_passes_team_openings_TeamOpeningId",
                        column: x => x.TeamOpeningId,
                        principalTable: "team_openings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lft_passes_PlayerId_TeamOpeningId",
                table: "lft_passes",
                columns: new[] { "PlayerId", "TeamOpeningId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lft_passes_TeamOpeningId",
                table: "lft_passes",
                column: "TeamOpeningId");

            migrationBuilder.CreateIndex(
                name: "IX_team_openings_Status_Game",
                table: "team_openings",
                columns: new[] { "Status", "Game" });

            migrationBuilder.CreateIndex(
                name: "IX_team_openings_TeamId",
                table: "team_openings",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lft_passes");

            migrationBuilder.DropTable(
                name: "team_openings");

            migrationBuilder.DropColumn(
                name: "TeamOpeningId",
                table: "team_membership_requests");
        }
    }
}
