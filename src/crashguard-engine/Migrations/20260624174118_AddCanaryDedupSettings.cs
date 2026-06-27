using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crashguard.Engine.Migrations
{
    /// <inheritdoc />
    public partial class AddCanaryDedupSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "dedup_interval",
                table: "canary_types",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "renotify_interval",
                table: "canary_types",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "canary_alert_batches",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    canary_type_id = table.Column<int>(type: "INTEGER", nullable: false),
                    opened_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_failure_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_notified_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    count = table.Column<int>(type: "INTEGER", nullable: false),
                    count_since_last_notify = table.Column<int>(type: "INTEGER", nullable: false),
                    last_canary_id = table.Column<int>(type: "INTEGER", nullable: false),
                    is_open = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_canary_alert_batches", x => x.id);
                    table.ForeignKey(
                        name: "fk_canary_alert_batches_canary_types_canary_type_id",
                        column: x => x.canary_type_id,
                        principalTable: "canary_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_canary_alert_batches_canary_type_id_is_open",
                table: "canary_alert_batches",
                columns: new[] { "canary_type_id", "is_open" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "canary_alert_batches");

            migrationBuilder.DropColumn(
                name: "dedup_interval",
                table: "canary_types");

            migrationBuilder.DropColumn(
                name: "renotify_interval",
                table: "canary_types");
        }
    }
}
