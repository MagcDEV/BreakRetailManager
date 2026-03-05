using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BreakRetailManager.Inventory.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductIsActiveAndProviderUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "inventory",
                table: "Providers",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "inventory",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "inventory",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "inventory",
                table: "Products");
        }
    }
}
