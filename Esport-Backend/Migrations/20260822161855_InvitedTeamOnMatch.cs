using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TForge.Migrations
{
    /// <inheritdoc />
    public partial class InvitedTeamOnMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InvitedTeamId",
                table: "matches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_matches_InvitedTeamId",
                table: "matches",
                column: "InvitedTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_matches_teams_InvitedTeamId",
                table: "matches",
                column: "InvitedTeamId",
                principalTable: "teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_matches_teams_InvitedTeamId",
                table: "matches");

            migrationBuilder.DropIndex(
                name: "IX_matches_InvitedTeamId",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "InvitedTeamId",
                table: "matches");
        }
    }
}
