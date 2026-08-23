using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TForge.Migrations
{
    /// <inheritdoc />
    public partial class MatchDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "match_drafts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    Game = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "BO1"),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    FirstTurnTeamId = table.Column<int>(type: "integer", nullable: false),
                    CurrentStepIndex = table.Column<int>(type: "integer", nullable: false),
                    TurnDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TurnSeconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_drafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_match_drafts_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "draft_steps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DraftId = table.Column<int>(type: "integer", nullable: false),
                    StepIndex = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ActingTeamId = table.Column<int>(type: "integer", nullable: true),
                    ChosenValue = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ChosenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WasAutoPicked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_draft_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_draft_steps_match_drafts_DraftId",
                        column: x => x.DraftId,
                        principalTable: "match_drafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_draft_steps_DraftId_StepIndex",
                table: "draft_steps",
                columns: new[] { "DraftId", "StepIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_match_drafts_MatchId",
                table: "match_drafts",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_match_drafts_Status_TurnDeadline",
                table: "match_drafts",
                columns: new[] { "Status", "TurnDeadline" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "draft_steps");

            migrationBuilder.DropTable(
                name: "match_drafts");
        }
    }
}
