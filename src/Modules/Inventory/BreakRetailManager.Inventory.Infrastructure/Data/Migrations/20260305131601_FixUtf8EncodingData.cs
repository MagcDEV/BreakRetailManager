using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BreakRetailManager.Inventory.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixUtf8EncodingData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "inventory",
                table: "LocationStocks",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            // Fix Latin-1 → UTF-8 mojibake in inventory tables.
            // Only updates rows containing the Ã character (mojibake marker).
            // Casts NVARCHAR → VARCHAR (Latin-1 bytes) → VARBINARY → reinterpret as UTF-8.
            const string fixSql = """
                -- inventory.Products
                UPDATE inventory.Products
                SET Name        = CAST(CAST(Name        AS VARCHAR(MAX)) AS NVARCHAR(200)),
                    Description = CAST(CAST(Description AS VARCHAR(MAX)) AS NVARCHAR(1000)),
                    Category    = CAST(CAST(Category    AS VARCHAR(MAX)) AS NVARCHAR(100)),
                    Barcode     = CAST(CAST(Barcode     AS VARCHAR(MAX)) AS NVARCHAR(100))
                WHERE Name LIKE N'%Ã%' OR Description LIKE N'%Ã%'
                   OR Category LIKE N'%Ã%' OR Barcode LIKE N'%Ã%';

                -- inventory.Providers
                UPDATE inventory.Providers
                SET Name        = CAST(CAST(Name        AS VARCHAR(MAX)) AS NVARCHAR(200)),
                    ContactName = CAST(CAST(ContactName AS VARCHAR(MAX)) AS NVARCHAR(200)),
                    Phone       = CAST(CAST(Phone       AS VARCHAR(MAX)) AS NVARCHAR(50)),
                    Email       = CAST(CAST(Email       AS VARCHAR(MAX)) AS NVARCHAR(200)),
                    Address     = CAST(CAST(Address     AS VARCHAR(MAX)) AS NVARCHAR(500))
                WHERE Name LIKE N'%Ã%' OR ContactName LIKE N'%Ã%'
                   OR Address LIKE N'%Ã%';

                -- inventory.Locations
                UPDATE inventory.Locations
                SET Name    = CAST(CAST(Name    AS VARCHAR(MAX)) AS NVARCHAR(200)),
                    Address = CAST(CAST(Address AS VARCHAR(MAX)) AS NVARCHAR(500))
                WHERE Name LIKE N'%Ã%' OR Address LIKE N'%Ã%';

                -- sales.SalesOrders (product names in order lines)
                UPDATE sales.SalesOrderLines
                SET ProductName = CAST(CAST(ProductName AS VARCHAR(MAX)) AS NVARCHAR(MAX))
                WHERE ProductName LIKE N'%Ã%';

                -- sales.Offers
                UPDATE sales.Offers
                SET Name        = CAST(CAST(Name        AS VARCHAR(MAX)) AS NVARCHAR(MAX)),
                    Description = CAST(CAST(Description AS VARCHAR(MAX)) AS NVARCHAR(MAX))
                WHERE Name LIKE N'%Ã%' OR Description LIKE N'%Ã%';
                """;

            migrationBuilder.Sql(fixSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "inventory",
                table: "LocationStocks");
        }
    }
}
