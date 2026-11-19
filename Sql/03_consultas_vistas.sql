-- =====================================================
--  Maizitos Baja - 03_consultas_vistas.sql
--  Vistas, consultas documentadas y transacciones
--  Ejecutar DESPUÉS de 01_esquema.sql y 02_datos.sql
-- =====================================================
USE MaizitosBaja;

-- Limpia las ventas de prueba que deja la sección de transacciones
-- (así este script se puede ejecutar varias veces).
-- El ON DELETE CASCADE borra también su detalle y sus pagos.
DELETE FROM Venta
WHERE VentaId > 0 AND NombreOrden IN ('Prueba COMMIT', 'Prueba ROLLBACK');


-- =====================================================
-- VISTA 1: Alertas de inventario
-- Propósito: el Dashboard muestra qué se está acabando.
-- Solo artículos y categorías activos (lo de temporada no estorba).
-- =====================================================
CREATE OR REPLACE VIEW vw_InventarioAlertas AS
SELECT a.ArticuloId,
       c.Tipo,
       c.Nombre AS Categoria,
       a.Nombre,
       a.Variante,
       a.Unidad,
       a.Stock,
       a.StockMinimo,
       CASE WHEN a.Stock = 0 THEN 'Agotado' ELSE 'Stock bajo' END AS Estado
FROM Articulo a
JOIN Categoria c ON c.CategoriaId = a.CategoriaId
WHERE a.Activo = TRUE
  AND c.Activo = TRUE
  AND a.Stock <= a.StockMinimo;


-- =====================================================
-- VISTA 2: Resumen de corte de caja
-- Propósito: al cerrar el día, la dueña ve cuánto se vendió por
-- método de pago, cuánto efectivo debería haber y si sobra o falta.
-- Lee los montos de la tabla Pago (permite pago mixto).
-- Solo cuenta ventas pagadas (no consumo personal ni canceladas).
-- =====================================================
CREATE OR REPLACE VIEW vw_ResumenCorte AS
SELECT c.CorteId,
       c.FechaCorte,
       c.Estado,
       c.FondoInicial,
       COUNT(DISTINCT v.VentaId)                                                    AS NumVentas,
       COALESCE(SUM(CASE WHEN p.MetodoPago = 'Efectivo'      THEN p.Monto END), 0) AS VentasEfectivo,
       COALESCE(SUM(CASE WHEN p.MetodoPago = 'Tarjeta'       THEN p.Monto END), 0) AS VentasTarjeta,
       COALESCE(SUM(CASE WHEN p.MetodoPago = 'Transferencia' THEN p.Monto END), 0) AS VentasTransferencia,
       COALESCE(SUM(p.Monto), 0)                                                    AS TotalVendido,
       c.FondoInicial
         + COALESCE(SUM(CASE WHEN p.MetodoPago = 'Efectivo' THEN p.Monto END), 0)  AS EfectivoEsperado,
       c.EfectivoContado,
       c.EfectivoContado - (c.FondoInicial
         + COALESCE(SUM(CASE WHEN p.MetodoPago = 'Efectivo' THEN p.Monto END), 0)) AS Diferencia
FROM CorteCaja c
LEFT JOIN Venta v ON v.CorteId = c.CorteId
                 AND v.Tipo    = 'Venta'
                 AND v.Estado  = 'Pagada'
LEFT JOIN Pago p  ON p.VentaId = v.VentaId
GROUP BY c.CorteId, c.FechaCorte, c.Estado, c.FondoInicial, c.EfectivoContado;

-- Demostración de las vistas
SELECT * FROM vw_InventarioAlertas ORDER BY Tipo, Categoria;   -- 5 artículos
SELECT * FROM vw_ResumenCorte;                                  -- corte 1: -5 de diferencia


-- =====================================================
-- CONSULTAS DOCUMENTADAS (10)
-- =====================================================

-- -----------------------------------------------------
-- C1 (JOIN). Ticket de una venta: qué se vendió en la venta 5.
-- Une Venta, DetalleVenta, Producto y Articulo. Cada renglón es
-- un platillo o un artículo suelto, por eso los LEFT JOIN.
-- Resultado esperado: Charola $190 y 2 Sprite $70.
-- -----------------------------------------------------
SELECT v.VentaId,
       v.NombreOrden,
       COALESCE(p.Nombre, CONCAT(a.Nombre, ' ', a.Variante)) AS Concepto,
       d.Cantidad,
       d.PrecioUnitario,
       d.Subtotal
FROM Venta v
JOIN DetalleVenta d ON d.VentaId    = v.VentaId
LEFT JOIN Producto p ON p.ProductoId = d.ProductoId
LEFT JOIN Articulo a ON a.ArticuloId = d.ArticuloId
WHERE v.VentaId = 5
ORDER BY d.DetalleVentaId;

-- -----------------------------------------------------
-- C2 (JOIN). Inventario completo con su categoría y proveedor,
-- ordenado por tipo y categoría (pantalla de Inventario).
-- Resultado esperado: 49 artículos activos.
-- -----------------------------------------------------
SELECT c.Tipo,
       c.Nombre  AS Categoria,
       a.Nombre,
       a.Variante,
       a.Stock,
       a.Unidad,
       pr.Nombre AS Proveedor
FROM Articulo a
JOIN Categoria c       ON c.CategoriaId  = a.CategoriaId
LEFT JOIN Proveedor pr ON pr.ProveedorId = a.ProveedorId
WHERE a.Activo = TRUE
ORDER BY c.Tipo, c.Nombre, a.Nombre, a.Variante;

-- -----------------------------------------------------
-- C3 (JOIN, 5 tablas). Qué eligieron los clientes en sus platillos.
-- Resultado esperado: 7 renglones (Tostilocos -> Tostitos Verdes, etc.).
-- -----------------------------------------------------
SELECT v.Fecha,
       v.NombreOrden,
       p.Nombre                         AS Platillo,
       CONCAT(a.Nombre, ' ', a.Variante) AS Eligio,
       dva.Cantidad
FROM DetalleVentaArticulo dva
JOIN DetalleVenta d ON d.DetalleVentaId = dva.DetalleVentaId
JOIN Venta v        ON v.VentaId        = d.VentaId
JOIN Producto p     ON p.ProductoId     = d.ProductoId
JOIN Articulo a     ON a.ArticuloId     = dva.ArticuloId
ORDER BY v.Fecha;

-- -----------------------------------------------------
-- C4 (Subconsulta con NOT EXISTS). Platillos del menú que nunca
-- se han vendido. Sirve para decidir qué quitar o promocionar.
-- Resultado esperado: 12 platillos.
-- -----------------------------------------------------
SELECT p.Nombre, p.Precio
FROM Producto p
WHERE p.Activo = TRUE
  AND NOT EXISTS (SELECT 1
                  FROM DetalleVenta d
                  JOIN Venta v ON v.VentaId = d.VentaId
                  WHERE d.ProductoId = p.ProductoId
                    AND v.Estado = 'Pagada')
ORDER BY p.Nombre;

-- -----------------------------------------------------
-- C5 (Subconsulta escalar). Ventas mayores al promedio.
-- Promedio de ventas pagadas = 1025 / 6 = 170.83
-- Resultado esperado: Karla $260 y Sofi $175.
-- -----------------------------------------------------
SELECT VentaId, NombreOrden, Fecha, Total
FROM Venta
WHERE Tipo = 'Venta'
  AND Estado = 'Pagada'
  AND Total > (SELECT AVG(Total)
               FROM Venta
               WHERE Tipo = 'Venta' AND Estado = 'Pagada')
ORDER BY Total DESC;

-- -----------------------------------------------------
-- C6 (Agregación + GROUP BY). Dinero por método de pago.
-- Resultado esperado: Tarjeta 475, Efectivo 430, Transferencia 120.
-- -----------------------------------------------------
SELECT p.MetodoPago,
       COUNT(*)     AS NumPagos,
       SUM(p.Monto) AS Total
FROM Pago p
JOIN Venta v ON v.VentaId = p.VentaId
WHERE v.Estado = 'Pagada'
GROUP BY p.MetodoPago
ORDER BY Total DESC;

-- -----------------------------------------------------
-- C7 (Agregación + GROUP BY + HAVING). Categorías con 5 o más
-- artículos diferentes y cuántas piezas/paquetes tienen en total.
-- Resultado esperado: Sabritas 8, Sodas 8, Maruchan 7, Buldak 6,
-- Jugos 6, Vasos 5, Tapas 5.
-- -----------------------------------------------------
SELECT c.Tipo,
       c.Nombre     AS Categoria,
       COUNT(*)     AS Articulos,
       SUM(a.Stock) AS Existencia
FROM Categoria c
JOIN Articulo a ON a.CategoriaId = c.CategoriaId
GROUP BY c.CategoriaId, c.Tipo, c.Nombre
HAVING COUNT(*) >= 5
ORDER BY Articulos DESC, Categoria;

-- -----------------------------------------------------
-- C8 (Agregación + GROUP BY + HAVING). Lo que más dinero deja:
-- platillos y sueltos con $100 o más de ventas.
-- Resultado esperado: Charola 190, Buldak 175, Tostijumbo 160,
-- Maruchan volcán 150, Tostilocos 120, Vaso mediano 120.
-- -----------------------------------------------------
SELECT COALESCE(p.Nombre, CONCAT(a.Nombre, ' ', a.Variante)) AS Concepto,
       SUM(d.Cantidad) AS Piezas,
       SUM(d.Subtotal) AS Importe
FROM DetalleVenta d
JOIN Venta v         ON v.VentaId    = d.VentaId
LEFT JOIN Producto p ON p.ProductoId = d.ProductoId
LEFT JOIN Articulo a ON a.ArticuloId = d.ArticuloId
WHERE v.Tipo = 'Venta' AND v.Estado = 'Pagada'
GROUP BY Concepto
HAVING SUM(d.Subtotal) >= 100
ORDER BY Importe DESC;

-- -----------------------------------------------------
-- C9 (Libre: UNION). Dinero que NO entró a caja:
-- consumo personal y descuentos a familiares/amigos.
-- Resultado esperado: Consumo personal 1 / $60; Descuentos 1 / $30.
-- -----------------------------------------------------
SELECT 'Consumo personal' AS Concepto,
       COUNT(*)           AS Veces,
       SUM(Total)         AS Monto
FROM Venta
WHERE Tipo = 'ConsumoPersonal'
UNION ALL
SELECT 'Descuentos',
       COUNT(*),
       SUM(Descuento)
FROM Venta
WHERE Descuento > 0 AND Estado = 'Pagada';

-- -----------------------------------------------------
-- C10 (Libre: función de ventana). Kardex de la Coca regular:
-- cada movimiento y cómo iba quedando el stock.
-- Resultado esperado: 24 -> 48 -> 47.
-- -----------------------------------------------------
SELECT m.Fecha,
       m.Tipo,
       m.Cantidad,
       SUM(m.Cantidad) OVER (ORDER BY m.Fecha, m.MovimientoId) AS StockAcumulado,
       m.Observaciones
FROM MovimientoInventario m
WHERE m.ArticuloId = 25
ORDER BY m.Fecha, m.MovimientoId;


-- =====================================================
-- TRANSACCIONES
-- Una venta toca 3 tablas (Venta, DetalleVenta, Pago).
-- Deben guardarse todas o ninguna.
-- =====================================================

-- T1: Transacción confirmada (COMMIT)
-- Vaso de elote grande $80 pagado con tarjeta, en el corte de hoy.
START TRANSACTION;
    INSERT INTO Venta (CorteId, UsuarioId, NombreOrden, Tipo, Subtotal, Descuento, Total)
    VALUES (2, 1, 'Prueba COMMIT', 'Venta', 80.00, 0.00, 80.00);
    SET @venta = LAST_INSERT_ID();

    INSERT INTO DetalleVenta (VentaId, ProductoId, ArticuloId, Cantidad, PrecioUnitario, Subtotal)
    VALUES (@venta, 3, NULL, 1, 80.00, 80.00);

    INSERT INTO Pago (VentaId, MetodoPago, Monto)
    VALUES (@venta, 'Tarjeta', 80.00);
COMMIT;

-- T2: Caso controlado con ROLLBACK
-- Se empieza a registrar una venta, pero se cancela antes de confirmar
-- (por ejemplo, el cliente se arrepiente o falla el cobro con tarjeta).
START TRANSACTION;
    INSERT INTO Venta (CorteId, UsuarioId, NombreOrden, Tipo, Subtotal, Descuento, Total)
    VALUES (2, 1, 'Prueba ROLLBACK', 'Venta', 50.00, 0.00, 50.00);
    SET @venta = LAST_INSERT_ID();

    INSERT INTO DetalleVenta (VentaId, ProductoId, ArticuloId, Cantidad, PrecioUnitario, Subtotal)
    VALUES (@venta, 1, NULL, 1, 50.00, 50.00);
ROLLBACK;

-- Comprobación: solo debe existir 'Prueba COMMIT' (con su detalle y pago).
-- 'Prueba ROLLBACK' no se guardó en ninguna tabla.
SELECT v.VentaId, v.NombreOrden, v.Total,
       (SELECT COUNT(*) FROM DetalleVenta d WHERE d.VentaId = v.VentaId) AS Renglones,
       (SELECT COUNT(*) FROM Pago p        WHERE p.VentaId = v.VentaId) AS Pagos
FROM Venta v
WHERE v.NombreOrden LIKE 'Prueba%';