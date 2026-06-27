using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crashguard.Engine.Migrations
{
    /// <inheritdoc />
    public partial class AddCanaryTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "canary_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    timeout = table.Column<int>(type: "INTEGER", nullable: false),
                    severity = table.Column<string>(type: "TEXT", nullable: false),
                    metadata_schema = table.Column<string>(type: "TEXT", nullable: true),
                    verifier_url = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_canary_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "canary_type_rules",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    canary_type_id = table.Column<int>(type: "INTEGER", nullable: false),
                    field = table.Column<string>(type: "TEXT", nullable: false),
                    @operator = table.Column<string>(name: "operator", type: "TEXT", nullable: false),
                    value = table.Column<string>(type: "TEXT", nullable: true),
                    severity = table.Column<string>(type: "TEXT", nullable: false),
                    channel = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_canary_type_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_canary_type_rules_canary_types_canary_type_id",
                        column: x => x.canary_type_id,
                        principalTable: "canary_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_canary_type_rules_canary_type_id",
                table: "canary_type_rules",
                column: "canary_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_canary_types_name",
                table: "canary_types",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "canary_type_rules");

            migrationBuilder.DropTable(
                name: "canary_types");
        }
    }
}
