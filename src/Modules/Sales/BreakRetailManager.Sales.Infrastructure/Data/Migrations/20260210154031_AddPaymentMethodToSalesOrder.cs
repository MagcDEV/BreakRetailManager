using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BreakRetailManager.Sales.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodToSalesOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                schema: "sales",
                table: "SalesOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                schema: "sales",
                table: "SalesOrders");
        }
    }
}
