-- =====================================================
--  Maizitos Baja - 04_rutinas_pruebas.sql
--  Triggers, función, procedimientos almacenados y sus pruebas
--
--  ORDEN PARA REINICIAR LA BASE: 01 -> 02 -> 03 -> 04
--  (el 02 carga datos históricos sin triggers; si se vuelve a
--   correr el 02 hay que correr antes el 01)
--
--  IMPORTANTE (Workbench): la sección de PRUEBAS INVÁLIDAS
--  provoca errores a propósito. Antes de ejecutar todo el script,
--  activa el botón "Toggle whether execution of SQL script should
--  continue after failed statements" (círculo rojo de la barra),
--  o ejecuta cada prueba inválida por separado (Ctrl + Enter).
-- =====================================================
USE MaizitosBaja;

DROP TRIGGER   IF EXISTS trg_Movimiento_BI;
DROP TRIGGER   IF EXISTS trg_Movimiento_AI;
DROP TRIGGER   IF EXISTS trg_Venta_BI;
DROP FUNCTION  IF EXISTS fn_EfectivoEsperado;
DROP PROCEDURE IF EXISTS sp_AbrirCorte;
DROP PROCEDURE IF EXISTS sp_RegistrarVenta;
DROP PROCEDURE IF EXISTS sp_CancelarVenta;
DROP PROCEDURE IF EXISTS sp_CerrarCorte;

DELIMITER $$

-- =====================================================
-- TRIGGER 1 (antes de insertar un movimiento de inventario)
-- Evento:    BEFORE INSERT en MovimientoInventario
-- Condición: el stock resultante no puede ser negativo
-- Efecto:    si no alcanza, rechaza la operación con un mensaje;
--            si alcanza, llena StockAnterior y StockNuevo (auditoría)
-- =====================================================
CREATE TRIGGER trg_Movimiento_BI
BEFORE INSERT ON MovimientoInventario
FOR EACH ROW
BEGIN
    DECLARE v_stock  INT;
    DECLARE v_nombre VARCHAR(110);
    DECLARE v_msg    VARCHAR(255);

    SELECT Stock, TRIM(CONCAT(Nombre, ' ', Variante))
      INTO v_stock, v_nombre
      FROM Articulo
     WHERE ArticuloId = NEW.ArticuloId
       FOR UPDATE;

    IF v_stock IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El artículo no existe';
    END IF;

    IF v_stock + NEW.Cantidad < 0 THEN
        SET v_msg = CONCAT('Stock insuficiente de ', v_nombre,
                           ': hay ', v_stock, ' y se necesitan ', -NEW.Cantidad);
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_msg;
    END IF;

    SET NEW.StockAnterior = v_stock;
    SET NEW.StockNuevo    = v_stock + NEW.Cantidad;
END$$

-- =====================================================
-- TRIGGER 2 (después de insertar un movimiento de inventario)
-- Evento:    AFTER INSERT en MovimientoInventario
-- Condición: siempre
-- Efecto:    actualiza Articulo.Stock con el stock nuevo.
--            ES EL ÚNICO LUGAR donde cambia el stock: la aplicación
--            y los procedimientos solo registran movimientos, así
--            nunca se descuenta dos veces.
-- =====================================================
CREATE TRIGGER trg_Movimiento_AI
AFTER INSERT ON MovimientoInventario
FOR EACH ROW
BEGIN
    UPDATE Articulo
       SET Stock = NEW.StockNuevo
     WHERE ArticuloId = NEW.ArticuloId;
END$$

-- =====================================================
-- TRIGGER 3 (antes de insertar una venta)
-- Evento:    BEFORE INSERT en Venta
-- Condición: el corte de la venta debe existir y estar Abierto
-- Efecto:    rechaza ventas en una caja cerrada
-- =====================================================
CREATE TRIGGER trg_Venta_BI
BEFORE INSERT ON Venta
FOR EACH ROW
BEGIN
    DECLARE v_estado VARCHAR(10);

    SELECT Estado INTO v_estado
      FROM CorteCaja
     WHERE CorteId = NEW.CorteId;

    IF v_estado IS NULL OR v_estado <> 'Abierto' THEN
        SIGNAL SQLSTATE '45000'
           SET MESSAGE_TEXT = 'No se pueden registrar ventas: la caja está cerrada o no existe';
    END IF;
END$$

-- =====================================================
-- FUNCIÓN: fn_EfectivoEsperado(corte)
-- Devuelve cuánto efectivo debe haber en caja:
-- fondo inicial + pagos en efectivo de ventas pagadas del corte.
-- =====================================================
CREATE FUNCTION fn_EfectivoEsperado(p_CorteId INT)
RETURNS DECIMAL(10,2)
READS SQL DATA
BEGIN
    DECLARE v_fondo    DECIMAL(10,2);
    DECLARE v_efectivo DECIMAL(10,2);

    SELECT FondoInicial INTO v_fondo
      FROM CorteCaja
     WHERE CorteId = p_CorteId;

    IF v_fondo IS NULL THEN
        RETURN NULL;
    END IF;

    SELECT COALESCE(SUM(p.Monto), 0) INTO v_efectivo
      FROM Pago p
      JOIN Venta v ON v.VentaId = p.VentaId
     WHERE v.CorteId    = p_CorteId
       AND v.Tipo       = 'Venta'
       AND v.Estado     = 'Pagada'
       AND p.MetodoPago = 'Efectivo';

    RETURN v_fondo + v_efectivo;
END$$

-- =====================================================
-- PROCEDIMIENTO: sp_AbrirCorte
-- Abre la caja del día con su fondo inicial.
-- Errores controlados: fondo negativo, ya hay caja abierta,
-- ya se hizo el corte de hoy.
-- =====================================================
CREATE PROCEDURE sp_AbrirCorte(
    IN  p_UsuarioId    INT,
    IN  p_FondoInicial DECIMAL(10,2),
    OUT p_CorteId      INT)
BEGIN
    SET p_CorteId = NULL;

    IF p_FondoInicial IS NULL OR p_FondoInicial < 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El fondo inicial no puede ser negativo';
    END IF;

    IF EXISTS (SELECT 1 FROM CorteCaja WHERE Estado = 'Abierto') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ya hay una caja abierta. Ciérrala antes de abrir otra';
    END IF;

    IF EXISTS (SELECT 1 FROM CorteCaja WHERE FechaCorte = CURDATE()) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ya se hizo el corte de hoy';
    END IF;

    INSERT INTO CorteCaja (UsuarioId, FondoInicial)
    VALUES (p_UsuarioId, p_FondoInicial);

    SET p_CorteId = LAST_INSERT_ID();
END$$

-- =====================================================
-- PROCEDIMIENTO: sp_RegistrarVenta  (operación de negocio principal)
-- Registra TODA la venta en una sola transacción:
--   Venta + DetalleVenta + DetalleVentaArticulo + Pago + MovimientoInventario
-- (el stock lo actualizan los triggers).
-- Si algo falla, hace ROLLBACK y no se guarda nada.
--
-- p_Items (JSON): lista de renglones
--   [{"tipo":"producto","id":18,"cantidad":1,"nota":"sin chile",
--     "elegidos":[{"id":17,"cantidad":1}]},
--    {"tipo":"articulo","id":26,"cantidad":2}]
--   "nota" es opcional (indicaciones del cliente para el platillo)
-- p_Pagos (JSON): lista de pagos (vacía en consumo personal)
--   [{"metodo":"Tarjeta","monto":100},{"metodo":"Efectivo","monto":75}]
-- =====================================================
CREATE PROCEDURE sp_RegistrarVenta(
    IN  p_UsuarioId        INT,
    IN  p_NombreOrden      VARCHAR(50),
    IN  p_Tipo             VARCHAR(20),
    IN  p_Descuento        DECIMAL(10,2),
    IN  p_MotivoDescuento  VARCHAR(100),
    IN  p_EfectivoRecibido DECIMAL(10,2),
    IN  p_Items            JSON,
    IN  p_Pagos            JSON,
    OUT p_VentaId          INT)
BEGIN
    DECLARE v_corte     INT;
    DECLARE v_n         INT;
    DECLARE v_i         INT DEFAULT 0;
    DECLARE v_item      JSON;
    DECLARE v_tipoItem  VARCHAR(10);
    DECLARE v_id        INT;
    DECLARE v_cant      INT;
    DECLARE v_precio    DECIMAL(10,2);
    DECLARE v_nombre    VARCHAR(110);
    DECLARE v_nota      VARCHAR(150);
    DECLARE v_subtotal  DECIMAL(10,2) DEFAULT 0;
    DECLARE v_total     DECIMAL(10,2);
    DECLARE v_numPagos  INT DEFAULT 0;
    DECLARE v_sumPagos  DECIMAL(10,2) DEFAULT 0;
    DECLARE v_efectivo  DECIMAL(10,2) DEFAULT 0;
    DECLARE v_recibido  DECIMAL(10,2);
    DECLARE v_cambio    DECIMAL(10,2);
    DECLARE v_det       INT;
    DECLARE v_tipoMov   VARCHAR(20);
    DECLARE v_malos     INT;
    DECLARE v_msg       VARCHAR(255);

    -- Cualquier error: deshacer todo y avisar a quien llamó
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET p_VentaId   = NULL;
    SET p_Descuento = COALESCE(p_Descuento, 0);
    SET p_Pagos     = COALESCE(p_Pagos, JSON_ARRAY());

    IF p_Tipo IS NULL OR p_Tipo NOT IN ('Venta', 'ConsumoPersonal') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Tipo de venta no válido (Venta o ConsumoPersonal)';
    END IF;

    IF p_Items IS NULL OR JSON_LENGTH(p_Items) = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La venta no tiene productos';
    END IF;

    START TRANSACTION;

    -- Caja abierta
    SELECT CorteId INTO v_corte
      FROM CorteCaja
     WHERE Estado = 'Abierto'
     ORDER BY CorteId DESC
     LIMIT 1;

    IF v_corte IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No hay caja abierta. Abre la caja antes de vender';
    END IF;

    -- ---------- Pasada 1: validar renglones y calcular subtotal ----------
    SET v_n = JSON_LENGTH(p_Items);
    WHILE v_i < v_n DO
        SET v_item     = JSON_EXTRACT(p_Items, CONCAT('$[', v_i, ']'));
        SET v_tipoItem = JSON_UNQUOTE(JSON_EXTRACT(v_item, '$.tipo'));
        SET v_id       = JSON_EXTRACT(v_item, '$.id');
        SET v_cant     = JSON_EXTRACT(v_item, '$.cantidad');
        SET v_precio   = NULL;

        IF v_cant IS NULL OR v_cant <= 0 THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cantidad de cada renglón debe ser mayor a 0';
        END IF;

        IF v_tipoItem = 'producto' THEN
            SELECT Precio INTO v_precio
              FROM Producto
             WHERE ProductoId = v_id AND Activo = TRUE;

            IF v_precio IS NULL THEN
                SET v_msg = CONCAT('El platillo con id ', v_id, ' no existe o no está disponible');
                SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_msg;
            END IF;

            SELECT COUNT(*) INTO v_malos
              FROM JSON_TABLE(v_item, '$.elegidos[*]'
                     COLUMNS (id INT PATH '$.id')) e
              LEFT JOIN Articulo a ON a.ArticuloId = e.id AND a.Activo = TRUE
             WHERE a.ArticuloId IS NULL;

            IF v_malos > 0 THEN
                SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Uno de los artículos elegidos no existe o no está activo';
            END IF;

        ELSEIF v_tipoItem = 'articulo' THEN
            SELECT PrecioSuelto INTO v_precio
              FROM Articulo
             WHERE ArticuloId = v_id AND Activo = TRUE AND SeVendeSuelto = TRUE;

            IF v_precio IS NULL THEN
                SET v_msg = CONCAT('El artículo con id ', v_id, ' no existe, no está activo o no se vende suelto');
                SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_msg;
            END IF;

        ELSE
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Tipo de renglón no válido (producto o articulo)';
        END IF;

        SET v_subtotal = v_subtotal + v_cant * v_precio;
        SET v_i = v_i + 1;
    END WHILE;

    IF p_Descuento < 0 OR p_Descuento > v_subtotal THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El descuento no puede ser negativo ni mayor al subtotal';
    END IF;

    SET v_total = v_subtotal - p_Descuento;

    -- ---------- Validar pagos ----------
    SELECT COUNT(*),
           COALESCE(SUM(pg.monto), 0),
           COALESCE(SUM(CASE WHEN pg.metodo = 'Efectivo' THEN pg.monto END), 0)
      INTO v_numPagos, v_sumPagos, v_efectivo
      FROM JSON_TABLE(p_Pagos, '$[*]'
             COLUMNS (metodo VARCHAR(20)   PATH '$.metodo',
                      monto  DECIMAL(10,2) PATH '$.monto')) pg;

    IF p_Tipo = 'ConsumoPersonal' THEN
        IF v_numPagos > 0 OR p_Descuento > 0 THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El consumo personal no lleva pagos ni descuento';
        END IF;
        SET v_recibido = NULL;
        SET v_cambio   = NULL;
    ELSE
        IF v_sumPagos <> v_total THEN
            SET v_msg = CONCAT('Los pagos ($', v_sumPagos, ') no cuadran con el total ($', v_total, ')');
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_msg;
        END IF;

        IF v_efectivo > 0 THEN
            SET v_recibido = COALESCE(p_EfectivoRecibido, v_efectivo);
            IF v_recibido < v_efectivo THEN
                SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El efectivo recibido es menor a lo que se paga en efectivo';
            END IF;
            SET v_cambio = v_recibido - v_efectivo;
        ELSE
            SET v_recibido = NULL;
            SET v_cambio   = NULL;
        END IF;
    END IF;

    -- ---------- Encabezado ----------
    INSERT INTO Venta (CorteId, UsuarioId, NombreOrden, Tipo, Subtotal, Descuento,
                       MotivoDescuento, Total, EfectivoRecibido, Cambio)
    VALUES (v_corte, p_UsuarioId, p_NombreOrden, p_Tipo, v_subtotal, p_Descuento,
            p_MotivoDescuento, v_total, v_recibido, v_cambio);

    SET p_VentaId = LAST_INSERT_ID();
    SET v_tipoMov = IF(p_Tipo = 'Venta', 'Venta', 'ConsumoPersonal');

    -- ---------- Pasada 2: guardar renglones y movimientos ----------
    SET v_i = 0;
    WHILE v_i < v_n DO
        SET v_item     = JSON_EXTRACT(p_Items, CONCAT('$[', v_i, ']'));
        SET v_tipoItem = JSON_UNQUOTE(JSON_EXTRACT(v_item, '$.tipo'));
        SET v_id       = JSON_EXTRACT(v_item, '$.id');
        SET v_cant     = JSON_EXTRACT(v_item, '$.cantidad');

        -- Nota opcional: solo si viene como texto y no está vacía
        SET v_nota = NULL;
        IF JSON_TYPE(JSON_EXTRACT(v_item, '$.nota')) = 'STRING' THEN
            SET v_nota = NULLIF(TRIM(LEFT(JSON_UNQUOTE(JSON_EXTRACT(v_item, '$.nota')), 150)), '');
        END IF;

        IF v_tipoItem = 'producto' THEN
            SELECT Precio, Nombre INTO v_precio, v_nombre
              FROM Producto
             WHERE ProductoId = v_id;

            INSERT INTO DetalleVenta (VentaId, ProductoId, ArticuloId, Cantidad, PrecioUnitario, Subtotal, Nota)
            VALUES (p_VentaId, v_id, NULL, v_cant, v_precio, v_cant * v_precio, v_nota);

            SET v_det = LAST_INSERT_ID();

            -- Lo que eligió el cliente
            INSERT INTO DetalleVentaArticulo (DetalleVentaId, ArticuloId, Cantidad)
            SELECT v_det, e.id, e.cantidad
              FROM JSON_TABLE(v_item, '$.elegidos[*]'
                     COLUMNS (id       INT PATH '$.id',
                              cantidad INT PATH '$.cantidad' DEFAULT '1' ON EMPTY)) e;

            -- Sale del inventario (los triggers validan y actualizan el stock)
            INSERT INTO MovimientoInventario (ArticuloId, Tipo, Cantidad, UsuarioId, VentaId, Observaciones)
            SELECT e.id, v_tipoMov, -e.cantidad, p_UsuarioId, p_VentaId, CONCAT('Elegido en ', v_nombre)
              FROM JSON_TABLE(v_item, '$.elegidos[*]'
                     COLUMNS (id       INT PATH '$.id',
                              cantidad INT PATH '$.cantidad' DEFAULT '1' ON EMPTY)) e;
        ELSE
            SELECT PrecioSuelto INTO v_precio
              FROM Articulo
             WHERE ArticuloId = v_id;

            INSERT INTO DetalleVenta (VentaId, ProductoId, ArticuloId, Cantidad, PrecioUnitario, Subtotal, Nota)
            VALUES (p_VentaId, NULL, v_id, v_cant, v_precio, v_cant * v_precio, v_nota);

            INSERT INTO MovimientoInventario (ArticuloId, Tipo, Cantidad, UsuarioId, VentaId, Observaciones)
            VALUES (v_id, v_tipoMov, -v_cant, p_UsuarioId, p_VentaId, 'Venta suelta');
        END IF;

        SET v_i = v_i + 1;
    END WHILE;

    -- ---------- Pagos ----------
    INSERT INTO Pago (VentaId, MetodoPago, Monto)
    SELECT p_VentaId, pg.metodo, pg.monto
      FROM JSON_TABLE(p_Pagos, '$[*]'
             COLUMNS (metodo VARCHAR(20)   PATH '$.metodo',
                      monto  DECIMAL(10,2) PATH '$.monto')) pg;

    COMMIT;
END$$

-- =====================================================
-- PROCEDIMIENTO: sp_CancelarVenta
-- Marca la venta como Cancelada y regresa al inventario
-- todo lo que había salido por esa venta (en una transacción).
-- Errores controlados: no existe, ya cancelada, corte cerrado.
-- =====================================================
CREATE PROCEDURE sp_CancelarVenta(
    IN p_VentaId   INT,
    IN p_UsuarioId INT,
    IN p_Motivo    VARCHAR(200))
BEGIN
    DECLARE v_estado      VARCHAR(10);
    DECLARE v_estadoCorte VARCHAR(10);

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    SELECT v.Estado, c.Estado
      INTO v_estado, v_estadoCorte
      FROM Venta v
      JOIN CorteCaja c ON c.CorteId = v.CorteId
     WHERE v.VentaId = p_VentaId
       FOR UPDATE;

    IF v_estado IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La venta no existe';
    END IF;

    IF v_estado = 'Cancelada' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La venta ya estaba cancelada';
    END IF;

    IF v_estadoCorte = 'Cerrado' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No se puede cancelar una venta de un corte ya cerrado';
    END IF;

    UPDATE Venta
       SET Estado = 'Cancelada'
     WHERE VentaId = p_VentaId;

    -- Regresar al inventario lo que salió por esta venta
    INSERT INTO MovimientoInventario (ArticuloId, Tipo, Cantidad, UsuarioId, VentaId, Observaciones)
    SELECT m.ArticuloId, 'Cancelacion', -SUM(m.Cantidad), p_UsuarioId, p_VentaId,
           COALESCE(p_Motivo, 'Venta cancelada')
      FROM MovimientoInventario m
     WHERE m.VentaId = p_VentaId
       AND m.Tipo IN ('Venta', 'ConsumoPersonal')
     GROUP BY m.ArticuloId;

    COMMIT;
END$$

-- =====================================================
-- PROCEDIMIENTO: sp_CerrarCorte
-- Cierra la caja abierta con el efectivo contado y devuelve
-- la diferencia (negativo = falta, positivo = sobra).
-- También regresa el resumen del corte para mostrarlo en pantalla.
-- =====================================================
CREATE PROCEDURE sp_CerrarCorte(
    IN  p_EfectivoContado DECIMAL(10,2),
    IN  p_Observaciones   VARCHAR(200),
    OUT p_Diferencia      DECIMAL(10,2))
BEGIN
    DECLARE v_corte INT;

    SET p_Diferencia = NULL;

    IF p_EfectivoContado IS NULL OR p_EfectivoContado < 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El efectivo contado no puede ser negativo';
    END IF;

    SELECT CorteId INTO v_corte
      FROM CorteCaja
     WHERE Estado = 'Abierto'
     ORDER BY CorteId DESC
     LIMIT 1;

    IF v_corte IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No hay caja abierta para cerrar';
    END IF;

    UPDATE CorteCaja
       SET Estado          = 'Cerrado',
           FechaCierre     = NOW(),
           EfectivoContado = p_EfectivoContado,
           Observaciones   = p_Observaciones
     WHERE CorteId = v_corte;

    SET p_Diferencia = p_EfectivoContado - fn_EfectivoEsperado(v_corte);

    SELECT * FROM vw_ResumenCorte WHERE CorteId = v_corte;
END$$

DELIMITER ;


-- =====================================================
-- PRUEBAS VÁLIDAS
-- =====================================================

-- V1. Función: efectivo esperado
--     Corte 1 = 755 (500 + 255)   Corte 2 = 675 (500 + 175)
SELECT fn_EfectivoEsperado(1) AS EsperadoCorte1,
       fn_EfectivoEsperado(2) AS EsperadoCorte2;

-- V2. Venta normal en efectivo: Tostilocos (elige Tostitos verdes)
--     + 2 Coca Zero sueltas = $190. Paga con $500, cambio $310.
--     Los Tostilocos llevan la nota "sin cacahuate".
CALL sp_RegistrarVenta(1, 'Prueba V2', 'Venta', 0, NULL, 500,
     '[{"tipo":"producto","id":18,"cantidad":1,"nota":"sin cacahuate","elegidos":[{"id":17,"cantidad":1}]},
       {"tipo":"articulo","id":26,"cantidad":2}]',
     '[{"metodo":"Efectivo","monto":190}]',
     @ventaV2);

-- V3. Pago mixto: Buldak carbonara $175 = $100 tarjeta + $75 efectivo.
--     Da un billete de $100 -> cambio $25.
CALL sp_RegistrarVenta(1, 'Prueba V3', 'Venta', 0, NULL, 100,
     '[{"tipo":"producto","id":11,"cantidad":1,"elegidos":[{"id":11,"cantidad":1}]}]',
     '[{"metodo":"Tarjeta","monto":100},{"metodo":"Efectivo","monto":75}]',
     @ventaV3);

-- Revisar las dos ventas y la auditoría de inventario que llenó el trigger
SELECT VentaId, NombreOrden, Subtotal, Total, EfectivoRecibido, Cambio, Estado
  FROM Venta
 WHERE VentaId IN (@ventaV2, @ventaV3);

-- La nota quedó guardada en el renglón de los Tostilocos
SELECT VentaId, ProductoId, ArticuloId, Cantidad, Nota
  FROM DetalleVenta
 WHERE VentaId = @ventaV2;

SELECT m.VentaId, TRIM(CONCAT(a.Nombre, ' ', a.Variante)) AS Articulo,
       m.Tipo, m.Cantidad, m.StockAnterior, m.StockNuevo
  FROM MovimientoInventario m
  JOIN Articulo a ON a.ArticuloId = m.ArticuloId
 WHERE m.VentaId IN (@ventaV2, @ventaV3);
-- Esperado: Tostitos verdes 9 -> 8, Coca Zero 12 -> 10, Buldak carbonara 7 -> 6

-- Efectivo esperado del corte 2 ahora: 675 + 190 + 75 = 940
SELECT fn_EfectivoEsperado(2) AS EsperadoCorte2;

-- V4. Cancelar la venta V2: regresan Tostitos y Cocas
CALL sp_CancelarVenta(@ventaV2, 1, 'Prueba de cancelación');

SELECT TRIM(CONCAT(Nombre, ' ', Variante)) AS Articulo, Stock
  FROM Articulo
 WHERE ArticuloId IN (17, 26);
-- Esperado: Tostitos verdes 9, Coca Zero 12

-- Efectivo esperado del corte 2 sin la venta cancelada: 940 - 190 = 750
SELECT fn_EfectivoEsperado(2) AS EsperadoCorte2;

-- V5. Consumo personal: 1 Paleta de leche, sin pagos
CALL sp_RegistrarVenta(1, 'Prueba V5', 'ConsumoPersonal', 0, NULL, NULL,
     '[{"tipo":"articulo","id":1,"cantidad":1}]',
     NULL,
     @ventaV5);

SELECT VentaId, NombreOrden, Tipo, Total,
       (SELECT COUNT(*) FROM Pago p WHERE p.VentaId = v.VentaId) AS Pagos
  FROM Venta v
 WHERE VentaId = @ventaV5;
-- Esperado: ConsumoPersonal, 0 pagos


-- =====================================================
-- PRUEBAS INVÁLIDAS (cada una DEBE marcar error en rojo)
-- Recuerda activar "continuar después de errores" o
-- ejecutarlas una por una con Ctrl + Enter.
-- =====================================================

-- I1. Vender más de lo que hay (Sonny: hay 2)
--     -> Trigger: "Stock insuficiente de Sonny: hay 2 y se necesitan 5"
CALL sp_RegistrarVenta(1, 'Inválida I1', 'Venta', 0, NULL, NULL,
     '[{"tipo":"articulo","id":38,"cantidad":5}]',
     '[{"metodo":"Efectivo","monto":125}]',
     @x);

-- I2. Los pagos no cuadran con el total
--     -> "Los pagos ($40.00) no cuadran con el total ($50.00)"
CALL sp_RegistrarVenta(1, 'Inválida I2', 'Venta', 0, NULL, NULL,
     '[{"tipo":"producto","id":1,"cantidad":1}]',
     '[{"metodo":"Efectivo","monto":40}]',
     @x);

-- I3. Descuento sin motivo
--     -> CHECK de la tabla: "Check constraint 'CK_Venta_Motivo' is violated"
CALL sp_RegistrarVenta(1, 'Inválida I3', 'Venta', 10, NULL, NULL,
     '[{"tipo":"producto","id":1,"cantidad":1}]',
     '[{"metodo":"Efectivo","monto":40}]',
     @x);

-- I4. Platillo de temporada desactivado (Churros locos)
--     -> "El platillo con id 19 no existe o no está disponible"
UPDATE Producto SET Activo = FALSE WHERE ProductoId = 19;
CALL sp_RegistrarVenta(1, 'Inválida I4', 'Venta', 0, NULL, NULL,
     '[{"tipo":"producto","id":19,"cantidad":1}]',
     '[{"metodo":"Efectivo","monto":85}]',
     @x);
UPDATE Producto SET Activo = TRUE WHERE ProductoId = 19;

-- I5. Vender un vaso (material, no se vende suelto)
--     -> "El artículo con id 40 no existe, no está activo o no se vende suelto"
CALL sp_RegistrarVenta(1, 'Inválida I5', 'Venta', 0, NULL, NULL,
     '[{"tipo":"articulo","id":40,"cantidad":1}]',
     '[{"metodo":"Efectivo","monto":10}]',
     @x);

-- I6. Insertar una venta directo en el corte 1 (cerrado)
--     -> Trigger: "No se pueden registrar ventas: la caja está cerrada o no existe"
INSERT INTO Venta (CorteId, UsuarioId, NombreOrden, Subtotal, Total)
VALUES (1, 1, 'Inválida I6', 50, 50);

-- I7. Merma mayor al stock (Buldak limón: hay 2)
--     -> Trigger: "Stock insuficiente de Buldak Limón: hay 2 y se necesitan 10"
INSERT INTO MovimientoInventario (ArticuloId, Tipo, Cantidad, UsuarioId, Observaciones)
VALUES (15, 'Merma', -10, 1, 'Inválida I7');

-- I8. Abrir caja cuando ya hay una abierta
--     -> "Ya hay una caja abierta. Ciérrala antes de abrir otra"
CALL sp_AbrirCorte(1, 500, @x);

-- I9. Cancelar una venta que ya estaba cancelada (venta 6)
--     -> "La venta ya estaba cancelada"
CALL sp_CancelarVenta(6, 1, 'Inválida I9');

-- I10. Cerrar caja con efectivo negativo
--     -> "El efectivo contado no puede ser negativo"
CALL sp_CerrarCorte(-100, 'Inválida I10', @x);


-- =====================================================
-- COMPROBACIÓN FINAL: ninguna prueba inválida dejó datos
-- =====================================================
SELECT
    (SELECT COUNT(*) FROM Venta                WHERE NombreOrden   LIKE 'Inválida%') AS VentasInvalidas,
    (SELECT COUNT(*) FROM MovimientoInventario WHERE Observaciones LIKE 'Inválida%') AS MovimientosInvalidos,
    (SELECT Stock FROM Articulo WHERE ArticuloId = 38) AS StockSonny,
    (SELECT Stock FROM Articulo WHERE ArticuloId = 15) AS StockBuldakLimon,
    (SELECT Estado FROM CorteCaja WHERE CorteId = 2)   AS EstadoCorte2;
-- Esperado: 0, 0, 2, 2, 'Abierto'