using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSApi.Migrations
{
    /// <inheritdoc />
    public partial class AddLatestVersionAndUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CmsEntityVersions_CmsEntityId",
                table: "CmsEntityVersions");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "CmsEntities");

            migrationBuilder.AddColumn<int>(
                name: "LatestVersion",
                table: "CmsEntities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CmsEntityVersions_CmsEntityId_Version",
                table: "CmsEntityVersions",
                columns: new[] { "CmsEntityId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CmsEntityVersions_CmsEntityId_Version",
                table: "CmsEntityVersions");

            migrationBuilder.DropColumn(
                name: "LatestVersion",
                table: "CmsEntities");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "CmsEntities",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CmsEntityVersions_CmsEntityId",
                table: "CmsEntityVersions",
                column: "CmsEntityId");
        }
    }
}
