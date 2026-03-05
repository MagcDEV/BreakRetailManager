using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BreakRetailManager.Inventory.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropProductStockQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StockQuantity",
                schema: "inventory",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                schema: "inventory",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
