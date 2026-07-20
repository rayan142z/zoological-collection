using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zoolog.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFavoritesCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_collection_favorites_collection_id",
                table: "collection_favorites",
                column: "collection_id");

           
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            

            // Hier löschen wir den Index
            migrationBuilder.DropIndex(
                name: "IX_collection_favorites_collection_id",
                table: "collection_favorites");
        }
    }
}
