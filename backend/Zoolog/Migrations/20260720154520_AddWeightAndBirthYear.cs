using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zoolog.Migrations
{
    /// <inheritdoc />
    public partial class AddWeightAndBirthYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "birth_year",
                table: "specimen",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "weight",
                table: "specimen",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "birth_year",
                table: "specimen");

            migrationBuilder.DropColumn(
                name: "weight",
                table: "specimen");
        }
    }
}
