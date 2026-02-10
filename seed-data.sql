-- BreakRetailManager Seed Data for Testing
-- Run after the app has created the database (first startup)

-- Providers
DECLARE @provBebidas UNIQUEIDENTIFIER = NEWID();
DECLARE @provAlimentos UNIQUEIDENTIFIER = NEWID();
DECLARE @provLimpieza UNIQUEIDENTIFIER = NEWID();

INSERT INTO inventory.Providers (Id, Name, ContactName, Phone, Email, Address, CreatedAt) VALUES
(@provBebidas,   'Distribuidora Sur',    'Carlos Gómez', '011-4555-1234', 'carlos@distribuidorasur.com',    'Av. San Martín 890, CABA', SYSDATETIMEOFFSET()),
(@provAlimentos, 'Alimentos del Plata',  'María López',  '011-4666-5678', 'maria@alimentosdelplata.com',    'Calle Belgrano 456, Avellaneda', SYSDATETIMEOFFSET()),
(@provLimpieza,  'Limpia Hogar SRL',     'Juan Pérez',   '011-4777-9012', 'juan@limpiahogar.com',           'Ruta 3 Km 25, La Matanza', SYSDATETIMEOFFSET());

-- Locations
DECLARE @locThames    UNIQUEIDENTIFIER = NEWID();
DECLARE @locRoosevelt UNIQUEIDENTIFIER = NEWID();
DECLARE @locJuramento UNIQUEIDENTIFIER = NEWID();
DECLARE @locSanTelmo  UNIQUEIDENTIFIER = NEWID();

INSERT INTO inventory.Locations (Id, Name, Address, IsActive, CreatedAt) VALUES
(@locThames,    'Break Thames',    'Thames 2317, Palermo, CABA',            1, SYSDATETIMEOFFSET()),
(@locRoosevelt, 'Break Roosevelt', 'Av. Roosevelt 5200, Belgrano, CABA',    1, SYSDATETIMEOFFSET()),
(@locJuramento, 'Break Juramento', 'Juramento 1560, Belgrano, CABA',        1, SYSDATETIMEOFFSET()),
(@locSanTelmo,  'Break San Telmo', 'Defensa 1102, San Telmo, CABA',         1, SYSDATETIMEOFFSET());

-- Products
DECLARE @coca       UNIQUEIDENTIFIER = NEWID();
DECLARE @fanta      UNIQUEIDENTIFIER = NEWID();
DECLARE @agua       UNIQUEIDENTIFIER = NEWID();
DECLARE @galletitas UNIQUEIDENTIFIER = NEWID();
DECLARE @arroz      UNIQUEIDENTIFIER = NEWID();
DECLARE @fideos     UNIQUEIDENTIFIER = NEWID();
DECLARE @jabon      UNIQUEIDENTIFIER = NEWID();
DECLARE @lavandina  UNIQUEIDENTIFIER = NEWID();
DECLARE @yerba      UNIQUEIDENTIFIER = NEWID();
DECLARE @aceite     UNIQUEIDENTIFIER = NEWID();

DECLARE @now DATETIMEOFFSET = SYSDATETIMEOFFSET();

INSERT INTO inventory.Products (Id, Barcode, Name, [Description], Category, CostPrice, SalePrice, StockQuantity, ReorderLevel, ProviderId, CreatedAt, UpdatedAt) VALUES
(@coca,       '7790895000508', 'Coca-Cola 500ml',       'Gaseosa Coca-Cola 500ml',           'Bebidas',  500.00,  850.00,  0, 10, @provBebidas,   @now, @now),
(@fanta,      '7790895001505', 'Fanta Naranja 500ml',   'Gaseosa Fanta 500ml',               'Bebidas',  480.00,  800.00,  0, 10, @provBebidas,   @now, @now),
(@agua,       '7790895002502', 'Agua Mineral 1.5L',     'Agua mineral sin gas 1.5L',         'Bebidas',  350.00,  600.00,  0, 15, @provBebidas,   @now, @now),
(@galletitas, '7790040100008', 'Galletitas Traviata',   'Galletitas de agua Traviata 303g',  'Almacén',  400.00,  700.00,  0,  8, @provAlimentos, @now, @now),
(@arroz,      '7790040200005', 'Arroz Gallo Oro 1kg',   'Arroz largo fino 1kg',              'Almacén',  900.00, 1500.00,  0,  5, @provAlimentos, @now, @now),
(@fideos,     '7790040300002', 'Fideos Matarazzo 500g',  'Fideos spaghetti 500g',            'Almacén',  600.00, 1000.00,  0,  8, @provAlimentos, @now, @now),
(@jabon,      '7790060100003', 'Jabón Skip 800ml',      'Jabón líquido para ropa 800ml',    'Limpieza', 1200.00, 2000.00,  0,  5, @provLimpieza,  @now, @now),
(@lavandina,  '7790060200000', 'Lavandina Ayudín 1L',   'Lavandina concentrada 1L',         'Limpieza',  350.00,  600.00,  0, 10, @provLimpieza,  @now, @now),
(@yerba,      '7790040400009', 'Yerba Taragüí 1kg',     'Yerba mate con palo 1kg',          'Almacén', 1800.00, 3000.00,  0,  5, @provAlimentos, @now, @now),
(@aceite,     '7790040500006', 'Aceite Cocinero 1.5L',  'Aceite de girasol 1.5L',           'Almacén', 1500.00, 2500.00,  0,  5, @provAlimentos, @now, @now);

-- Location Stock (distribute stock across all 4 locations)
INSERT INTO inventory.LocationStocks (Id, LocationId, ProductId, Quantity, ReorderLevel) VALUES
-- Break Thames
(NEWID(), @locThames, @coca,       20, 10),
(NEWID(), @locThames, @fanta,      15, 10),
(NEWID(), @locThames, @agua,       25, 15),
(NEWID(), @locThames, @galletitas, 12,  8),
(NEWID(), @locThames, @arroz,       8,  5),
(NEWID(), @locThames, @fideos,     10,  8),
(NEWID(), @locThames, @jabon,       6,  5),
(NEWID(), @locThames, @lavandina,  14, 10),
(NEWID(), @locThames, @yerba,      10,  5),
(NEWID(), @locThames, @aceite,      7,  5),
-- Break Roosevelt
(NEWID(), @locRoosevelt, @coca,       18, 10),
(NEWID(), @locRoosevelt, @fanta,      12, 10),
(NEWID(), @locRoosevelt, @agua,       20, 15),
(NEWID(), @locRoosevelt, @galletitas, 10,  8),
(NEWID(), @locRoosevelt, @arroz,       6,  5),
(NEWID(), @locRoosevelt, @fideos,     14,  8),
(NEWID(), @locRoosevelt, @jabon,       8,  5),
(NEWID(), @locRoosevelt, @lavandina,  16, 10),
(NEWID(), @locRoosevelt, @yerba,       9,  5),
(NEWID(), @locRoosevelt, @aceite,      5,  5),
-- Break Juramento
(NEWID(), @locJuramento, @coca,       22, 10),
(NEWID(), @locJuramento, @fanta,      18, 10),
(NEWID(), @locJuramento, @agua,       30, 15),
(NEWID(), @locJuramento, @galletitas, 15,  8),
(NEWID(), @locJuramento, @arroz,      10,  5),
(NEWID(), @locJuramento, @fideos,     12,  8),
(NEWID(), @locJuramento, @jabon,       7,  5),
(NEWID(), @locJuramento, @lavandina,  20, 10),
(NEWID(), @locJuramento, @yerba,      12,  5),
(NEWID(), @locJuramento, @aceite,      9,  5),
-- Break San Telmo
(NEWID(), @locSanTelmo, @coca,       15, 10),
(NEWID(), @locSanTelmo, @fanta,      10, 10),
(NEWID(), @locSanTelmo, @agua,       18, 15),
(NEWID(), @locSanTelmo, @galletitas,  8,  8),
(NEWID(), @locSanTelmo, @arroz,       5,  5),
(NEWID(), @locSanTelmo, @fideos,      9,  8),
(NEWID(), @locSanTelmo, @jabon,       4,  5),
(NEWID(), @locSanTelmo, @lavandina,  11, 10),
(NEWID(), @locSanTelmo, @yerba,       7,  5),
(NEWID(), @locSanTelmo, @aceite,      6,  5);

-- Update global aggregate (Product.StockQuantity = SUM of all location stocks)
UPDATE p SET p.StockQuantity = agg.Total, p.UpdatedAt = SYSDATETIMEOFFSET()
FROM inventory.Products p
INNER JOIN (
    SELECT ProductId, SUM(Quantity) AS Total
    FROM inventory.LocationStocks
    GROUP BY ProductId
) agg ON p.Id = agg.ProductId;

PRINT 'Seed data inserted: 3 providers, 4 locations, 10 products, 40 location stocks.';
