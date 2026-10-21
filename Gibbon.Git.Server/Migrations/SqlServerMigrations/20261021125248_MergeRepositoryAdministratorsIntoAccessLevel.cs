using Gibbon.Git.Server.Repositories;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gibbon.Git.Server.Migrations.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class MergeRepositoryAdministratorsIntoAccessLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                UPDATE UserRepository_Permission SET AccessLevel = {(int)RepositoryAccessLevel.Administer}
                WHERE EXISTS (SELECT 1 FROM UserRepository_Administrator a WHERE a.Repository_Id = UserRepository_Permission.Repository_Id AND a.User_Id = UserRepository_Permission.User_Id)
                """);
            migrationBuilder.Sql($"""
                INSERT INTO UserRepository_Permission (Repository_Id, User_Id, AccessLevel)
                SELECT a.Repository_Id, a.User_Id, {(int)RepositoryAccessLevel.Administer} FROM UserRepository_Administrator a
                WHERE NOT EXISTS (SELECT 1 FROM UserRepository_Permission p WHERE p.Repository_Id = a.Repository_Id AND p.User_Id = a.User_Id)
                """);

            migrationBuilder.DropTable(
                name: "UserRepository_Administrator");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserRepository_Administrator",
                columns: table => new
                {
                    Repository_Id = table.Column<int>(type: "int", nullable: false),
                    User_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRepository_Administrator", x => new { x.Repository_Id, x.User_Id });
                    table.ForeignKey(
                        name: "FK_UserRepository_Administrator_Repository_Repository_Id",
                        column: x => x.Repository_Id,
                        principalTable: "Repository",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRepository_Administrator_User_User_Id",
                        column: x => x.User_Id,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRepository_Administrator_User_Id",
                table: "UserRepository_Administrator",
                column: "User_Id");

            migrationBuilder.Sql($"""
                INSERT INTO UserRepository_Administrator (Repository_Id, User_Id)
                SELECT Repository_Id, User_Id FROM UserRepository_Permission WHERE AccessLevel = {(int)RepositoryAccessLevel.Administer}
                """);
            migrationBuilder.Sql($"UPDATE UserRepository_Permission SET AccessLevel = {(int)RepositoryAccessLevel.Push} WHERE AccessLevel = {(int)RepositoryAccessLevel.Administer}");
            migrationBuilder.Sql($"UPDATE TeamRepository_Permission SET AccessLevel = {(int)RepositoryAccessLevel.Push} WHERE AccessLevel = {(int)RepositoryAccessLevel.Administer}");
        }
    }
}
