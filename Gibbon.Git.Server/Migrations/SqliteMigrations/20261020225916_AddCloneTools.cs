using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gibbon.Git.Server.Migrations.SqliteMigrations
{
    /// <inheritdoc />
    public partial class AddCloneTools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredCloneTool",
                table: "UserSettings",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomCloneTools",
                table: "ServerSettings",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnabledCloneTools",
                table: "ServerSettings",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredCloneTool",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "CustomCloneTools",
                table: "ServerSettings");

            migrationBuilder.DropColumn(
                name: "EnabledCloneTools",
                table: "ServerSettings");
        }
    }
}
