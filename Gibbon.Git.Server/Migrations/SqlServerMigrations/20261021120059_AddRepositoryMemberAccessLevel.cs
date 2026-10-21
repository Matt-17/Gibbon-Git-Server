using Gibbon.Git.Server.Repositories;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gibbon.Git.Server.Migrations.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class AddRepositoryMemberAccessLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "UserRepository_Permission",
                type: "int",
                nullable: false,
                defaultValue: (int)RepositoryAccessLevel.Push);

            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "TeamRepository_Permission",
                type: "int",
                nullable: false,
                defaultValue: (int)RepositoryAccessLevel.Push);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "UserRepository_Permission");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "TeamRepository_Permission");
        }
    }
}
