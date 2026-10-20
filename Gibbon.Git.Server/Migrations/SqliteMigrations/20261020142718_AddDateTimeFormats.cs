using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gibbon.Git.Server.Migrations.SqliteMigrations
{
    /// <inheritdoc />
    public partial class AddDateTimeFormats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"UserSettings\" SET DateFormat = NULL");

            migrationBuilder.AlterColumn<int>(
                name: "DateFormat",
                table: "UserSettings",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeFormat",
                table: "UserSettings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DateFormat",
                table: "ServerSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TimeFormat",
                table: "ServerSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeFormat",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "DateFormat",
                table: "ServerSettings");

            migrationBuilder.DropColumn(
                name: "TimeFormat",
                table: "ServerSettings");

            migrationBuilder.AlterColumn<string>(
                name: "DateFormat",
                table: "UserSettings",
                type: "TEXT",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
