using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gibbon.Git.Server.Migrations.SqliteMigrations
{
    /// <inheritdoc />
    public partial class AddDefaultBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultBranch",
                table: "ServerSettings",
                type: "TEXT",
                maxLength: 250,
                nullable: false,
                defaultValue: "master");

            // existing installations keep master, also those that never saved settings
            migrationBuilder.Sql("""
                INSERT INTO ServerSettings (AllowAnonymousPush, AllowUserRepositoryCreation, AllowAnonymousRegistration, DefaultLanguage, IsCommitAuthorAvatarVisible,
                    DisplayNameFormat, SortNameFormat, DateFormat, TimeFormat, DefaultRepositoryView, DefaultBranch)
                SELECT 0, 1, 0, 'en', 0, 0, 0, 0, 0, 0, 'master'
                WHERE NOT EXISTS (SELECT 1 FROM ServerSettings) AND EXISTS (SELECT 1 FROM Repository)
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultBranch",
                table: "ServerSettings");
        }
    }
}
