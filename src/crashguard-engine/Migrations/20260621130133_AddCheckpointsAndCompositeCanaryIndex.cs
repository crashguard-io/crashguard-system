using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crashguard.Engine.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckpointsAndCompositeCanaryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_canaries_canary_type",
                table: "canaries");

            migrationBuilder.DropIndex(
                name: "ix_canaries_reference_id",
                table: "canaries");

            migrationBuilder.CreateTable(
                name: "canary_checkpoints",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    canary_id = table.Column<int>(type: "INTEGER", nullable: false),
                    stage = table.Column<string>(type: "TEXT", nullable: false),
                    metadata = table.Column<string>(type: "TEXT", nullable: true),
                    recorded_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_canary_checkpoints", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_canaries_canary_type_reference_id",
                table: "canaries",
                columns: new[] { "canary_type", "reference_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_canary_checkpoints_canary_id",
                table: "canary_checkpoints",
                column: "canary_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "canary_checkpoints");

            migrationBuilder.DropIndex(
                name: "ix_canaries_canary_type_reference_id",
                table: "canaries");

            migrationBuilder.CreateIndex(
                name: "ix_canaries_canary_type",
                table: "canaries",
                column: "canary_type");

            migrationBuilder.CreateIndex(
                name: "ix_canaries_reference_id",
                table: "canaries",
                column: "reference_id",
                unique: true);
        }
    }
}
