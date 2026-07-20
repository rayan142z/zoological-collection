using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zoolog.Migrations
{
    /// <inheritdoc />
    public partial class AddFromCollectionToLoan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.AddColumn<string>(
                name: "FromCollection",
                table: "loan",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FromCollection",
                table: "loan");

        }
    }
}
