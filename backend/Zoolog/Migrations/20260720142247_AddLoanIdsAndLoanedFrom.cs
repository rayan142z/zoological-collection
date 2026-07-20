using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zoolog.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanIdsAndLoanedFrom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "loaned_to",
                table: "loan",
                type: "int",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<int>(
                name: "LoanedFrom",
                table: "loan",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoanedFrom",
                table: "loan");

            migrationBuilder.AlterColumn<string>(
                name: "loaned_to",
                table: "loan",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 255);
        }
    }
}
