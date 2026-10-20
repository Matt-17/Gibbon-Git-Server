using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gibbon.Git.Server.Migrations.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class AddNameFormats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayNameFormat",
                table: "UserSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortNameFormat",
                table: "UserSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayNameFormat",
                table: "ServerSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SortNameFormat",
                table: "ServerSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayNameFormat",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "SortNameFormat",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "DisplayNameFormat",
                table: "ServerSettings");

            migrationBuilder.DropColumn(
                name: "SortNameFormat",
                table: "ServerSettings");
        }
    }
}
