using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crashguard.Engine.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailSmtpSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "smtp_from_address",
                table: "settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "smtp_from_name",
                table: "settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "smtp_host",
                table: "settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "smtp_password",
                table: "settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "smtp_port",
                table: "settings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "smtp_use_tls",
                table: "settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "smtp_username",
                table: "settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "settings",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "smtp_from_address", "smtp_from_name", "smtp_host", "smtp_password", "smtp_port", "smtp_use_tls", "smtp_username" },
                values: new object[] { null, null, null, null, null, true, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "smtp_from_address",
                table: "settings");

            migrationBuilder.DropColumn(
                name: "smtp_from_name",
                table: "settings");

            migrationBuilder.DropColumn(
                name: "smtp_host",
                table: "settings");

            migrationBuilder.DropColumn(
                name: "smtp_password",
                table: "settings");

            migrationBuilder.DropColumn(
                name: "smtp_port",
                table: "settings");

            migrationBuilder.DropColumn(
                name: "smtp_use_tls",
                table: "settings");

            migrationBuilder.DropColumn(
                name: "smtp_username",
                table: "settings");
        }
    }
}
