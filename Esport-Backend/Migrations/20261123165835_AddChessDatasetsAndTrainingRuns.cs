using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TForge.Migrations
{
    /// <inheritdoc />
    public partial class AddChessDatasetsAndTrainingRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chess_datasets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    StoredPath = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Profile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RowCount = table.Column<int>(type: "integer", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    GamesByTimeControlJson = table.Column<string>(type: "text", nullable: false),
                    IsTimeOrdered = table.Column<bool>(type: "boolean", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chess_datasets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chess_datasets_users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chess_training_runs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DatasetId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Queued"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    QueuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModelPath = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    RowsTrain = table.Column<int>(type: "integer", nullable: true),
                    RowsTest = table.Column<int>(type: "integer", nullable: true),
                    MetricsJson = table.Column<string>(type: "text", nullable: true),
                    Log = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chess_training_runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chess_training_runs_chess_datasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "chess_datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chess_training_runs_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chess_datasets_UploadedByUserId",
                table: "chess_datasets",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_chess_training_runs_CreatedByUserId",
                table: "chess_training_runs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_chess_training_runs_DatasetId",
                table: "chess_training_runs",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_chess_training_runs_IsActive",
                table: "chess_training_runs",
                column: "IsActive",
                unique: true,
                filter: "\"IsActive\"");

            migrationBuilder.CreateIndex(
                name: "IX_chess_training_runs_Status",
                table: "chess_training_runs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chess_training_runs");

            migrationBuilder.DropTable(
                name: "chess_datasets");
        }
    }
}
