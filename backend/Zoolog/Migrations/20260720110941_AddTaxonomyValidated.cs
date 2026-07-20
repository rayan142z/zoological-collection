using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zoolog.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxonomyValidated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Validated",
                table: "taxonomy",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE taxonomy SET Validated = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Validated",
                table: "taxonomy");
        }
    }
}
