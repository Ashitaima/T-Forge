using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TForge.Migrations
{
    /// <inheritdoc />
    public partial class AddDuels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "duels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChallengerPlayerId = table.Column<int>(type: "integer", nullable: false),
                    OpponentPlayerId = table.Column<int>(type: "integer", nullable: false),
                    Game = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "BO1"),
                    Message = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ChallengerScore = table.Column<int>(type: "integer", nullable: false),
                    OpponentScore = table.Column<int>(type: "integer", nullable: false),
                    WinnerPlayerId = table.Column<int>(type: "integer", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_duels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_duels_players_ChallengerPlayerId",
                        column: x => x.ChallengerPlayerId,
                        principalTable: "players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_duels_players_OpponentPlayerId",
                        column: x => x.OpponentPlayerId,
                        principalTable: "players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_duels_ChallengerPlayerId_OpponentPlayerId",
                table: "duels",
                columns: new[] { "ChallengerPlayerId", "OpponentPlayerId" },
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_duels_OpponentPlayerId",
                table: "duels",
                column: "OpponentPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_duels_Status",
                table: "duels",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "duels");
        }
    }
}
