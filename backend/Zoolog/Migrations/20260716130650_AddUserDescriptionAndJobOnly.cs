using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zoolog.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDescriptionAndJobOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "job",
                table: "users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_specimen_collection_CollectionId1",
                table: "specimen");

            migrationBuilder.DropIndex(
                name: "IX_specimen_CollectionId1",
                table: "specimen");

            migrationBuilder.DropColumn(
                name: "description",
                table: "users");

            migrationBuilder.DropColumn(
                name: "job",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CollectionId1",
                table: "specimen");

            migrationBuilder.AlterColumn<DateTime>(
                name: "favorited_at",
                table: "collection_favorites",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "getdate()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.CreateIndex(
                name: "IX_collection_favorites_collection_id",
                table: "collection_favorites",
                column: "collection_id");

            migrationBuilder.AddForeignKey(
                name: "FK_collection_favorites_collection_collection_id",
                table: "collection_favorites",
                column: "collection_id",
                principalTable: "collection",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_collection_favorites_users_user_id",
                table: "collection_favorites",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
