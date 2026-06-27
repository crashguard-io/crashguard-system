using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crashguard.Engine.Migrations
{
    /// <inheritdoc />
    public partial class AddCanaryAlertBatchDestination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_canary_alert_batches_canary_type_id_is_open",
                table: "canary_alert_batches");

            migrationBuilder.AddColumn<string>(
                name: "channel",
                table: "canary_alert_batches",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "severity",
                table: "canary_alert_batches",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_canary_alert_batches_canary_type_id_channel_severity_is_open",
                table: "canary_alert_batches",
                columns: new[] { "canary_type_id", "channel", "severity", "is_open" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_canary_alert_batches_canary_type_id_channel_severity_is_open",
                table: "canary_alert_batches");

            migrationBuilder.DropColumn(
                name: "channel",
                table: "canary_alert_batches");

            migrationBuilder.DropColumn(
                name: "severity",
                table: "canary_alert_batches");

            migrationBuilder.CreateIndex(
                name: "ix_canary_alert_batches_canary_type_id_is_open",
                table: "canary_alert_batches",
                columns: new[] { "canary_type_id", "is_open" });
        }
    }
}
