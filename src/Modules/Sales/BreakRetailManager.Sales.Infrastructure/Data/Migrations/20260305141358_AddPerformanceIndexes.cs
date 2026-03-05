using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BreakRetailManager.Sales.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CreatedAt",
                schema: "sales",
                table: "SalesOrders",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_LocationId",
                schema: "sales",
                table: "SalesOrders",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_IsActive",
                schema: "sales",
                table: "Offers",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_CreatedAt",
                schema: "sales",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_LocationId",
                schema: "sales",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_Offers_IsActive",
                schema: "sales",
                table: "Offers");
        }
    }
}
