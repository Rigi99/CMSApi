using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMSApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedFieldName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CmsEntityVersions_CmsEntities_EntityId",
                table: "CmsEntityVersions");

            migrationBuilder.RenameColumn(
                name: "EntityId",
                table: "CmsEntityVersions",
                newName: "CmsEntityId");

            migrationBuilder.RenameIndex(
                name: "IX_CmsEntityVersions_EntityId",
                table: "CmsEntityVersions",
                newName: "IX_CmsEntityVersions_CmsEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_CmsEntityVersions_CmsEntities_CmsEntityId",
                table: "CmsEntityVersions",
                column: "CmsEntityId",
                principalTable: "CmsEntities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CmsEntityVersions_CmsEntities_CmsEntityId",
                table: "CmsEntityVersions");

            migrationBuilder.RenameColumn(
                name: "CmsEntityId",
                table: "CmsEntityVersions",
                newName: "EntityId");

            migrationBuilder.RenameIndex(
                name: "IX_CmsEntityVersions_CmsEntityId",
                table: "CmsEntityVersions",
                newName: "IX_CmsEntityVersions_EntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_CmsEntityVersions_CmsEntities_EntityId",
                table: "CmsEntityVersions",
                column: "EntityId",
                principalTable: "CmsEntities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
