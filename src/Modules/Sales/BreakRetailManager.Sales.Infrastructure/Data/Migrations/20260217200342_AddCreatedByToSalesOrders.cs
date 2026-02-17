using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BreakRetailManager.Sales.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByToSalesOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByDisplayName",
                schema: "sales",
                table: "SalesOrders",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByObjectId",
                schema: "sales",
                table: "SalesOrders",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByDisplayName",
                schema: "sales",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "CreatedByObjectId",
                schema: "sales",
                table: "SalesOrders");
        }
    }
}
