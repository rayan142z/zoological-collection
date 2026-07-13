using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zoolog.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           

            migrationBuilder.CreateTable(
                name: "collection_favorites",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false),
                    collection_id = table.Column<int>(type: "int", nullable: false),
                    favorited_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collection_favorites", x => new { x.user_id, x.collection_id });
                    table.ForeignKey(
                        name: "FK_collection_favorites_collection_collection_id",
                        column: x => x.collection_id,
                        principalTable: "collection",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_collection_favorites_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "collection_favorites");

            migrationBuilder.DropTable(
                name: "loan");

            migrationBuilder.DropTable(
                name: "specimen");

            migrationBuilder.DropTable(
                name: "collection");

            migrationBuilder.DropTable(
                name: "location");

            migrationBuilder.DropTable(
                name: "taxonomy");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
