using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketLine.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptImagePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptImagePath",
                table: "SaleInvoices",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptImagePath",
                table: "SaleInvoices");
        }
    }
}
