-- =====================================================
--  Maizitos Baja - 02_datos.sql : datos de prueba
--  Ejecutar DESPUÉS de 01_esquema.sql
-- =====================================================
USE MaizitosBaja;

-- Vaciar todas las tablas para poder ejecutar este script las veces que sea
SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE MovimientoInventario;
TRUNCATE TABLE DetalleVentaArticulo;
TRUNCATE TABLE DetalleVenta;
TRUNCATE TABLE Pago;
TRUNCATE TABLE Venta;
TRUNCATE TABLE CorteCaja;
TRUNCATE TABLE Producto;
TRUNCATE TABLE Articulo;
TRUNCATE TABLE Categoria;
TRUNCATE TABLE Proveedor;
TRUNCATE TABLE Usuario;
SET FOREIGN_KEY_CHECKS = 1;

-- ---------- Usuario ----------
INSERT INTO Usuario (NombreUsuario, NombreCompleto) VALUES
('admin', 'Dueña');

-- ---------- Proveedores (genéricos de prueba) ----------
INSERT INTO Proveedor (Nombre, Telefono) VALUES
('Distribuidora de refrescos', '6461000001'),   -- 1
('Distribuidora de botanas',   '6461000002'),   -- 2
('Desechables Ensenada',       '6461000003'),   -- 3
('Paletería proveedora',       '6461000004');   -- 4

-- ---------- Categorías ----------
INSERT INTO Categoria (Nombre, Tipo) VALUES
('Paletas',  'Comida'),     -- 1
('Maruchan', 'Comida'),     -- 2
('Buldak',   'Comida'),     -- 3
('Sabritas', 'Comida'),     -- 4
('Sodas',    'Bebida'),     -- 5
('Jugos',    'Bebida'),     -- 6
('Agua',     'Bebida'),     -- 7
('Vasos',    'Material'),   -- 8
('Tapas',    'Material');   -- 9

-- ---------- Artículos del inventario ----------
-- Stock = conteo inicial de prueba (la dueña capturará el real)
INSERT INTO Articulo
 (CategoriaId, ProveedorId, Nombre, Variante, Unidad, Stock, StockMinimo, SeVendeSuelto, PrecioSuelto) VALUES
-- Paletas
(1, 4, 'Paleta',    'Leche',        'Pieza',   20, 5, TRUE, 30.00),   -- 1
(1, 4, 'Paleta',    'Esquimo',      'Pieza',   15, 5, TRUE, 30.00),   -- 2
(1, 4, 'Paleta',    'Agua',         'Pieza',   25, 5, TRUE, 20.00),   -- 3
-- Maruchan (precio suelto POR CONFIRMAR)
(2, 2, 'Maruchan',  'Camarón',      'Pieza',   12, 4, TRUE, 25.00),   -- 4
(2, 2, 'Maruchan',  'Limón',        'Pieza',   10, 4, TRUE, 25.00),   -- 5
(2, 2, 'Maruchan',  'Piquín',       'Pieza',    8, 4, TRUE, 25.00),   -- 6
(2, 2, 'Maruchan',  'Habanero',     'Pieza',    6, 4, TRUE, 25.00),   -- 7
(2, 2, 'Maruchan',  'Pollo',        'Pieza',   10, 4, TRUE, 25.00),   -- 8
(2, 2, 'Maruchan',  'Pollo picante','Pieza',    5, 4, TRUE, 25.00),   -- 9
(2, 2, 'Maruchan',  'Carne y queso','Pieza',    3, 4, TRUE, 25.00),   -- 10 (stock bajo)
-- Buldak (precio suelto POR CONFIRMAR)
(3, 2, 'Buldak',    'Carbonara',    'Pieza',    8, 3, TRUE, 70.00),   -- 11
(3, 2, 'Buldak',    'Yakisoba',     'Pieza',    5, 3, TRUE, 70.00),   -- 12
(3, 2, 'Buldak',    'Original',     'Pieza',    6, 3, TRUE, 70.00),   -- 13
(3, 2, 'Buldak',    'Habanero',     'Pieza',    4, 3, TRUE, 70.00),   -- 14
(3, 2, 'Buldak',    'Limón',        'Pieza',    2, 3, TRUE, 70.00),   -- 15 (stock bajo)
(3, 2, 'Buldak',    'Roja',         'Pieza',    5, 3, TRUE, 70.00),   -- 16
-- Sabritas (precio suelto POR CONFIRMAR)
(4, 2, 'Tostitos',  'Verdes',       'Pieza',   10, 4, TRUE, 25.00),   -- 17
(4, 2, 'Tostitos',  'Morados',      'Pieza',   10, 4, TRUE, 25.00),   -- 18
(4, 2, 'Doritos',   'Rojos',        'Pieza',    8, 4, TRUE, 25.00),   -- 19
(4, 2, 'Ruffles',   'Verdes',       'Pieza',    8, 4, TRUE, 25.00),   -- 20
(4, 2, 'Dinamitas', 'Verdes',       'Pieza',    6, 3, TRUE, 25.00),   -- 21
(4, 2, 'Dinamitas', 'Morados',      'Pieza',    6, 3, TRUE, 25.00),   -- 22
(4, 2, 'Cheetos',   'Morados',      'Pieza',    8, 4, TRUE, 25.00),   -- 23
(4, 2, 'Crujientes','Morados',      'Pieza',    5, 3, TRUE, 25.00),   -- 24
-- Sodas
(5, 1, 'Coca',      'Regular',      'Pieza',   24, 6, TRUE, 35.00),   -- 25
(5, 1, 'Coca',      'Zero',         'Pieza',   12, 6, TRUE, 35.00),   -- 26
(5, 1, 'Topochico', 'Normal',       'Pieza',   12, 4, TRUE, 35.00),   -- 27
(5, 1, 'Topochico', 'Naranja',      'Pieza',    6, 4, TRUE, 35.00),   -- 28
(5, 1, 'Topochico', 'Limón',        'Pieza',    6, 4, TRUE, 35.00),   -- 29
(5, 1, 'Fanta',     'Fresa',        'Pieza',    6, 4, TRUE, 35.00),   -- 30
(5, 1, 'Sidral',    '',             'Pieza',    6, 4, TRUE, 35.00),   -- 31
(5, 1, 'Sprite',    '',             'Pieza',    8, 4, TRUE, 35.00),   -- 32
-- Jugos
(6, 1, 'Fuze Tea',  'Durazno',      'Pieza',    6, 3, TRUE, 35.00),   -- 33
(6, 1, 'Fuze Tea',  'Limón',        'Pieza',    6, 3, TRUE, 35.00),   -- 34
(6, 1, 'Fuze Tea',  'Frutos rojos', 'Pieza',    6, 3, TRUE, 35.00),   -- 35
(6, 1, 'Capri-Sun', '',             'Pieza',   10, 4, TRUE, 25.00),   -- 36
(6, 1, 'Del Valle', 'Naranja',      'Pieza',    6, 3, TRUE, 35.00),   -- 37
(6, 1, 'Sonny',     '',             'Pieza',    2, 3, TRUE, 25.00),   -- 38 (stock bajo)
-- Agua
(7, 1, 'Agua natural', '',          'Pieza',   24, 6, TRUE, 10.00),   -- 39
-- Vasos (por paquete, no se venden sueltos)
(8, 3, 'Vaso',      '8 oz',         'Paquete',  6, 3, FALSE, NULL),   -- 40
(8, 3, 'Vaso',      '10 oz',        'Paquete',  5, 3, FALSE, NULL),   -- 41
(8, 3, 'Vaso',      '12 oz',        'Paquete',  4, 3, FALSE, NULL),   -- 42
(8, 3, 'Vaso',      '1/2 L',        'Paquete',  3, 3, FALSE, NULL),   -- 43 (stock bajo)
(8, 3, 'Vaso',      '1 L',          'Paquete',  4, 3, FALSE, NULL),   -- 44
-- Tapas
(9, 3, 'Tapa',      '8 oz',         'Paquete',  5, 3, FALSE, NULL),   -- 45
(9, 3, 'Tapa',      '10 oz',        'Paquete',  5, 3, FALSE, NULL),   -- 46
(9, 3, 'Tapa',      '12 oz',        'Paquete',  4, 3, FALSE, NULL),   -- 47
(9, 3, 'Tapa',      '1/2 L',        'Paquete',  2, 3, FALSE, NULL),   -- 48 (stock bajo)
(9, 3, 'Tapa',      '1 L',          'Paquete',  4, 3, FALSE, NULL);   -- 49

-- ---------- Productos del menú ----------
INSERT INTO Producto (Nombre, Descripcion, Precio) VALUES
('Vaso de elote chico',      'Vaso 8 oz. Mantequilla, mayonesa, sal y limón',            50.00),  -- 1
('Vaso de elote mediano',    'Vaso 10 oz. Mantequilla, mayonesa, sal y limón',           60.00),  -- 2
('Vaso de elote grande',     'Vaso 12 oz. Mantequilla, mayonesa, sal y limón',           80.00),  -- 3
('Elote entero',             'Queso, mayonesa, mantequilla, sal y limón',                65.00),  -- 4
('Elote revolcado',          'Entero o en vaso. Mantequilla, mayonesa, limón, Cheetos Flaming, queso nachos', 100.00), -- 5
('Maruchan volcán',          'Sopa Maruchan con jugo y grano de elote, mayonesa, mantequilla, queso cotija, queso de nachos y papitas a elegir', 150.00), -- 6
('Tostielotes',              NULL,                                                       110.00), -- 7
('Nacho elotes',             NULL,                                                       110.00), -- 8
('Nachos',                   NULL,                                                        85.00), -- 9
('Cochinada',                NULL,                                                       100.00), -- 10
('Buldak carbonara',         'Ramen cocido con jugo de elote, grano de elote, mayonesa, mantequilla, queso cotija, queso de nachos, chipotle y papitas a elegir', 175.00), -- 11
('Charola para compartir',   'Grano de elote sazonado con mantequilla, mayonesa, queso y 2 papitas a elegir', 190.00), -- 12
('Papitas con queso de nachos', NULL,                                                     50.00), -- 13
('Papa elote con ramen',     'Papa cocida, elote desgranado, queso cheddar, mayonesa, mantequilla, chipotle, queso de nachos', 180.00), -- 14
('Papa elote sin ramen',     'Papa cocida, elote desgranado, queso cheddar, mayonesa, mantequilla, chipotle, queso de nachos', 130.00), -- 15
('Vaso cheto',               NULL,                                                       120.00), -- 16
('Tostijumbo',               'Sabritas grandes, elote, mayonesa, queso, mantequilla, limón', 160.00), -- 17
('Tostilocos',               'Tostitos, cacahuate, rielito, cuerito, pepino, limón, sal, chamoy, valentina, clamato', 120.00), -- 18
('Churros locos',            NULL,                                                        85.00); -- 19

-- ---------- Cortes de caja ----------
INSERT INTO CorteCaja (UsuarioId, FechaApertura, FondoInicial, FechaCierre, EfectivoContado, Estado, Observaciones) VALUES
(1, '2026-11-17 13:00:00', 500.00, '2026-11-17 21:00:00', 750.00, 'Cerrado', 'Faltaron $5 en el conteo'), -- 1 (ayer)
(1, '2026-11-18 13:00:00', 500.00, NULL, NULL, 'Abierto', NULL);                                          -- 2 (hoy)

-- ---------- Ventas ----------
INSERT INTO Venta (CorteId, UsuarioId, Fecha, NombreOrden, Tipo, Subtotal, Descuento, MotivoDescuento, Total, EfectivoRecibido, Cambio, Estado) VALUES
(1, 1, '2026-11-17 13:20:00', 'Ana',      'Venta',           155.00,  0.00, NULL,       155.00, 200.00, 45.00, 'Pagada'),    -- 1 efectivo
(1, 1, '2026-11-17 14:05:00', 'Luis',     'Venta',           155.00,  0.00, NULL,       155.00, NULL,   NULL,  'Pagada'),    -- 2 tarjeta
(1, 1, '2026-11-17 15:30:00', 'Tía Rosa', 'Venta',           150.00, 30.00, 'Familiar', 120.00, NULL,   NULL,  'Pagada'),    -- 3 transferencia con descuento
(1, 1, '2026-11-17 17:10:00', 'Familia',  'ConsumoPersonal',  60.00,  0.00, NULL,        60.00, NULL,   NULL,  'Pagada'),    -- 4 consumo personal (sin pago)
(1, 1, '2026-11-17 18:00:00', 'Karla',    'Venta',           260.00,  0.00, NULL,       260.00, 100.00,  0.00, 'Pagada'),    -- 5 PAGO MIXTO
(1, 1, '2026-11-17 19:30:00', 'Pedro',    'Venta',            60.00,  0.00, NULL,        60.00, 100.00, 40.00, 'Cancelada'), -- 6 cancelada
(2, 1, '2026-11-18 13:15:00', 'Sofi',     'Venta',           175.00,  0.00, NULL,       175.00, 200.00, 25.00, 'Pagada'),    -- 7 efectivo
(2, 1, '2026-11-18 14:00:00', 'Mario',    'Venta',           160.00,  0.00, NULL,       160.00, NULL,   NULL,  'Pagada');    -- 8 tarjeta

-- ---------- Pagos (una venta puede tener varios) ----------
INSERT INTO Pago (VentaId, MetodoPago, Monto) VALUES
(1, 'Efectivo',      155.00),
(2, 'Tarjeta',       155.00),
(3, 'Transferencia', 120.00),
(5, 'Tarjeta',       160.00),   -- Karla: $160 con tarjeta
(5, 'Efectivo',      100.00),   --        + $100 en efectivo
(6, 'Efectivo',       60.00),
(7, 'Efectivo',      175.00),
(8, 'Tarjeta',       160.00);

-- ---------- Detalle de ventas (platillo O artículo suelto) ----------
INSERT INTO DetalleVenta (VentaId, ProductoId, ArticuloId, Cantidad, PrecioUnitario, Subtotal) VALUES
(1, 2,    NULL, 2,  60.00, 120.00),  -- 1  2 vasos de elote medianos
(1, NULL, 25,   1,  35.00,  35.00),  -- 2  Coca regular suelta
(2, 18,   NULL, 1, 120.00, 120.00),  -- 3  Tostilocos
(2, NULL, 29,   1,  35.00,  35.00),  -- 4  Topochico limón
(3, 6,    NULL, 1, 150.00, 150.00),  -- 5  Maruchan volcán
(4, 1,    NULL, 1,  50.00,  50.00),  -- 6  Vaso de elote chico (consumo)
(4, NULL, 39,   1,  10.00,  10.00),  -- 7  Agua (consumo)
(5, 12,   NULL, 1, 190.00, 190.00),  -- 8  Charola para compartir
(5, NULL, 32,   2,  35.00,  70.00),  -- 9  2 Sprite
(6, NULL, 9,    1,  25.00,  25.00),  -- 10 Maruchan pollo picante suelta
(6, NULL, 33,   1,  35.00,  35.00),  -- 11 Fuze Tea durazno
(7, 11,   NULL, 1, 175.00, 175.00),  -- 12 Buldak carbonara
(8, 17,   NULL, 1, 160.00, 160.00);  -- 13 Tostijumbo

-- ---------- Lo que eligieron para cada platillo ----------
INSERT INTO DetalleVentaArticulo (DetalleVentaId, ArticuloId, Cantidad) VALUES
(3,  17, 1),   -- Tostilocos         -> Tostitos verdes
(5,  4,  1),   -- Maruchan volcán    -> Maruchan camarón
(5,  19, 1),   --                    -> Doritos rojos
(8,  20, 1),   -- Charola            -> Ruffles verdes
(8,  23, 1),   --                    -> Cheetos morados
(12, 11, 1),   -- Buldak carbonara   -> Buldak carbonara
(13, 18, 1);   -- Tostijumbo         -> Tostitos morados

-- ---------- Movimientos de inventario ----------
-- Conteo inicial: un movimiento por cada artículo con su stock
INSERT INTO MovimientoInventario (ArticuloId, Tipo, Cantidad, StockAnterior, StockNuevo, Fecha, UsuarioId, Observaciones)
SELECT ArticuloId, 'Inicial', Stock, 0, Stock, '2026-11-17 12:00:00', 1, 'Conteo inicial'
FROM Articulo
WHERE Stock > 0;

-- Entradas, uso, merma, ventas, consumo y cancelación
INSERT INTO MovimientoInventario (ArticuloId, Tipo, Cantidad, Fecha, UsuarioId, ProveedorId, VentaId, Observaciones) VALUES
(25, 'Entrada',         24, '2026-11-17 12:30:00', 1, 1,    NULL, 'Compra de 1 caja de Coca'),
(41, 'Uso',             -1, '2026-11-17 13:05:00', 1, NULL, NULL, 'Se abrió un paquete de vasos 10 oz'),
(1,  'Merma',           -2, '2026-11-17 16:00:00', 1, NULL, NULL, 'Se derritieron'),
(25, 'Venta',           -1, '2026-11-17 13:20:00', 1, NULL, 1,    NULL),
(17, 'Venta',           -1, '2026-11-17 14:05:00', 1, NULL, 2,    NULL),
(29, 'Venta',           -1, '2026-11-17 14:05:00', 1, NULL, 2,    NULL),
(4,  'Venta',           -1, '2026-11-17 15:30:00', 1, NULL, 3,    NULL),
(19, 'Venta',           -1, '2026-11-17 15:30:00', 1, NULL, 3,    NULL),
(39, 'ConsumoPersonal', -1, '2026-11-17 17:10:00', 1, NULL, 4,    NULL),
(20, 'Venta',           -1, '2026-11-17 18:00:00', 1, NULL, 5,    NULL),
(23, 'Venta',           -1, '2026-11-17 18:00:00', 1, NULL, 5,    NULL),
(32, 'Venta',           -2, '2026-11-17 18:00:00', 1, NULL, 5,    NULL),
(9,  'Venta',           -1, '2026-11-17 19:30:00', 1, NULL, 6,    NULL),
(33, 'Venta',           -1, '2026-11-17 19:30:00', 1, NULL, 6,    NULL),
(9,  'Cancelacion',      1, '2026-11-17 19:40:00', 1, NULL, 6,    'Cliente canceló'),
(33, 'Cancelacion',      1, '2026-11-17 19:40:00', 1, NULL, 6,    'Cliente canceló'),
(11, 'Venta',           -1, '2026-11-18 13:15:00', 1, NULL, 7,    NULL),
(18, 'Venta',           -1, '2026-11-18 14:00:00', 1, NULL, 8,    NULL);

-- Recalcular el stock a partir de los movimientos (así siempre cuadra)
UPDATE Articulo a
SET a.Stock = (SELECT SUM(m.Cantidad)
               FROM MovimientoInventario m
               WHERE m.ArticuloId = a.ArticuloId)
WHERE a.ArticuloId > 0;

-- ---------- Comprobaciones ----------
-- Artículos con stock bajo (deben salir 5)
SELECT CONCAT(Nombre, ' ', Variante) AS Articulo, Stock, StockMinimo
FROM Articulo
WHERE Stock <= StockMinimo;

-- Dinero vendido por corte (corte 1 = 690, corte 2 = 335)
SELECT CorteId, SUM(Total) AS TotalVendido
FROM Venta
WHERE Tipo = 'Venta' AND Estado = 'Pagada'
GROUP BY CorteId;

-- Dinero por método de pago (Efectivo 430, Tarjeta 475, Transferencia 120)
SELECT p.MetodoPago, SUM(p.Monto) AS Total
FROM Pago p
JOIN Venta v ON v.VentaId = p.VentaId
WHERE v.Estado = 'Pagada'
GROUP BY p.MetodoPago;