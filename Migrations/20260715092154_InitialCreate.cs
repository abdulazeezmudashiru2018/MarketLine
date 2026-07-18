using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MarketLine.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SaleDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Sales",
                columns: new[] { "Id", "Amount", "AvatarUrl", "CustomerName", "SaleDate" },
                values: new object[,]
                {
                    { 1, 3200m, null, "John Smith", new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, 6150m, null, "Sara Williams", new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, 2980m, null, "Michael Brown", new DateTime(2026, 7, 14, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 4, 4750m, null, "Emily Johnson", new DateTime(2026, 7, 14, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 5, 15230m, null, "David Lee", new DateTime(2026, 7, 13, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 6, 1250m, null, "Grace Adams", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Sales");
        }
    }
}
