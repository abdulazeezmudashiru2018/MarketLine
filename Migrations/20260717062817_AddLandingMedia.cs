using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketLine.Migrations
{
    /// <inheritdoc />
    public partial class AddLandingMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LandingMediaItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slot = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MediaType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LandingMediaItems", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LandingMediaItems");
        }
    }
}
