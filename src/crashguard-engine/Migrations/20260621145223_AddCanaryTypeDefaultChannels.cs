using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crashguard.Engine.Migrations
{
    /// <inheritdoc />
    public partial class AddCanaryTypeDefaultChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "canary_type_channels",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    canary_type_id = table.Column<int>(type: "INTEGER", nullable: false),
                    channel_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_canary_type_channels", x => x.id);
                    table.ForeignKey(
                        name: "fk_canary_type_channels_canary_types_canary_type_id",
                        column: x => x.canary_type_id,
                        principalTable: "canary_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_canary_type_channels_channels_channel_id",
                        column: x => x.channel_id,
                        principalTable: "channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_canary_type_channels_canary_type_id",
                table: "canary_type_channels",
                column: "canary_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_canary_type_channels_canary_type_id_channel_id",
                table: "canary_type_channels",
                columns: new[] { "canary_type_id", "channel_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_canary_type_channels_channel_id",
                table: "canary_type_channels",
                column: "channel_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "canary_type_channels");
        }
    }
}
