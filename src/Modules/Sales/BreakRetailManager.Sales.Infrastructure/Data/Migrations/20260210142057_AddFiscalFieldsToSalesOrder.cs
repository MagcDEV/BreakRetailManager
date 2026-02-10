using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BreakRetailManager.Sales.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalFieldsToSalesOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cae",
                schema: "sales",
                table: "SalesOrders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CaeExpirationDate",
                schema: "sales",
                table: "SalesOrders",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InvoiceNumber",
                schema: "sales",
                table: "SalesOrders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceType",
                schema: "sales",
                table: "SalesOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointOfSale",
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
                name: "Cae",
                schema: "sales",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "CaeExpirationDate",
                schema: "sales",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                schema: "sales",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "InvoiceType",
                schema: "sales",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "PointOfSale",
                schema: "sales",
                table: "SalesOrders");
        }
    }
}
