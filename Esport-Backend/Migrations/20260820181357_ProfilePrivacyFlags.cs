using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TForge.Migrations
{
    /// <inheritdoc />
    public partial class ProfilePrivacyFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNameHidden",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAgeHidden",
                table: "players",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCountryHidden",
                table: "players",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsNameHidden",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsAgeHidden",
                table: "players");

            migrationBuilder.DropColumn(
                name: "IsCountryHidden",
                table: "players");
        }
    }
}
