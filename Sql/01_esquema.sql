-- =====================================================
--  Maizitos Baja - Punto de venta e inventario
--  01_esquema.sql : creación de la base y sus tablas
--  MySQL 8
-- =====================================================

-- OJO: borra la base si ya existe (solo para pruebas)
DROP DATABASE IF EXISTS MaizitosBaja;
CREATE DATABASE MaizitosBaja CHARACTER SET utf8mb4 COLLATE utf8mb4_spanish_ci;
USE MaizitosBaja;

-- Categorías del inventario, agrupadas en 3 tipos
CREATE TABLE Categoria (
    CategoriaId  INT AUTO_INCREMENT PRIMARY KEY,
    Nombre       VARCHAR(50) NOT NULL,
    Tipo         ENUM('Comida','Bebida','Material') NOT NULL,
    Activo       BOOLEAN     NOT NULL DEFAULT TRUE,
    CONSTRAINT UQ_Categoria_Nombre UNIQUE (Nombre)
);

-- A quién le compran
CREATE TABLE Proveedor (
    ProveedorId  INT AUTO_INCREMENT PRIMARY KEY,
    Nombre       VARCHAR(100) NOT NULL,
    Telefono     VARCHAR(15)  NULL,
    Correo       VARCHAR(100) NULL,
    Activo       BOOLEAN      NOT NULL DEFAULT TRUE,
    CONSTRAINT UQ_Proveedor_Nombre UNIQUE (Nombre)
);

-- Cuenta del sistema (Nidia y su hija usan la misma)
CREATE TABLE Usuario (
    UsuarioId      INT AUTO_INCREMENT PRIMARY KEY,
    NombreUsuario  VARCHAR(30)  NOT NULL,
    NombreCompleto VARCHAR(100) NOT NULL,
    Rol            ENUM('Administrador') NOT NULL DEFAULT 'Administrador',
    Activo         BOOLEAN      NOT NULL DEFAULT TRUE,
    FechaRegistro  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_Usuario_NombreUsuario UNIQUE (NombreUsuario)
);

-- Todo lo que hay en el inventario (comida, bebida y material)
CREATE TABLE Articulo (
    ArticuloId    INT AUTO_INCREMENT PRIMARY KEY,
    CategoriaId   INT           NOT NULL,
    ProveedorId   INT           NULL,
    Nombre        VARCHAR(60)   NOT NULL,              -- Coca, Tostitos, Vaso 12 oz
    Variante      VARCHAR(40)   NOT NULL DEFAULT '',   -- Zero, Verdes, Limón ('' si no tiene)
    Unidad        ENUM('Pieza','Paquete') NOT NULL DEFAULT 'Pieza',
    Stock         INT           NOT NULL DEFAULT 0,
    StockMinimo   INT           NOT NULL DEFAULT 0,    -- alerta de inventario
    SeVendeSuelto BOOLEAN       NOT NULL DEFAULT FALSE,
    PrecioSuelto  DECIMAL(10,2) NULL,
    Activo        BOOLEAN       NOT NULL DEFAULT TRUE,
    FechaRegistro DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_Articulo UNIQUE (CategoriaId, Nombre, Variante),
    CONSTRAINT CK_Articulo_Stock    CHECK (Stock >= 0),
    CONSTRAINT CK_Articulo_StockMin CHECK (StockMinimo >= 0),
    CONSTRAINT CK_Articulo_Precio   CHECK (
        (SeVendeSuelto = FALSE AND PrecioSuelto IS NULL) OR
        (SeVendeSuelto = TRUE  AND PrecioSuelto > 0)),
    CONSTRAINT FK_Articulo_Categoria FOREIGN KEY (CategoriaId)
        REFERENCES Categoria (CategoriaId),
    CONSTRAINT FK_Articulo_Proveedor FOREIGN KEY (ProveedorId)
        REFERENCES Proveedor (ProveedorId)
);

-- Platillos del menú
CREATE TABLE Producto (
    ProductoId    INT AUTO_INCREMENT PRIMARY KEY,
    Nombre        VARCHAR(80)   NOT NULL,
    Descripcion   VARCHAR(250)  NULL,
    Precio        DECIMAL(10,2) NOT NULL,
    Activo        BOOLEAN       NOT NULL DEFAULT TRUE,
    FechaRegistro DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_Producto_Nombre UNIQUE (Nombre),
    CONSTRAINT CK_Producto_Precio CHECK (Precio > 0)
);

-- Apertura y cierre de caja (un corte por día)
CREATE TABLE CorteCaja (
    CorteId         INT AUTO_INCREMENT PRIMARY KEY,
    UsuarioId       INT           NOT NULL,
    FechaApertura   DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaCorte      DATE          AS (DATE(FechaApertura)) STORED,  -- se calcula sola
    FondoInicial    DECIMAL(10,2) NOT NULL DEFAULT 0,
    FechaCierre     DATETIME      NULL,
    EfectivoContado DECIMAL(10,2) NULL,
    Estado          ENUM('Abierto','Cerrado') NOT NULL DEFAULT 'Abierto',
    Observaciones   VARCHAR(200)  NULL,
    CONSTRAINT UQ_Corte_Fecha UNIQUE (FechaCorte),
    CONSTRAINT CK_Corte_Fondo CHECK (FondoInicial >= 0),
    CONSTRAINT CK_Corte_Estado CHECK (
        (Estado = 'Abierto' AND FechaCierre IS NULL AND EfectivoContado IS NULL) OR
        (Estado = 'Cerrado' AND FechaCierre IS NOT NULL AND EfectivoContado >= 0)),
    CONSTRAINT CK_Corte_Fechas CHECK (FechaCierre IS NULL OR FechaCierre >= FechaApertura),
    CONSTRAINT FK_Corte_Usuario FOREIGN KEY (UsuarioId)
        REFERENCES Usuario (UsuarioId)
);

-- Encabezado de cada venta (el "ticket")
CREATE TABLE Venta (
    VentaId          INT AUTO_INCREMENT PRIMARY KEY,
    CorteId          INT           NOT NULL,
    UsuarioId        INT           NOT NULL,
    Fecha            DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    NombreOrden      VARCHAR(50)   NULL,
    Tipo             ENUM('Venta','ConsumoPersonal') NOT NULL DEFAULT 'Venta',
    Subtotal         DECIMAL(10,2) NOT NULL,
    Descuento        DECIMAL(10,2) NOT NULL DEFAULT 0,
    MotivoDescuento  VARCHAR(100)  NULL,
    Total            DECIMAL(10,2) NOT NULL,
    EfectivoRecibido DECIMAL(10,2) NULL,     -- billete que dio el cliente
    Cambio           DECIMAL(10,2) NULL,
    Estado           ENUM('Pagada','Cancelada') NOT NULL DEFAULT 'Pagada',
    CONSTRAINT CK_Venta_Subtotal  CHECK (Subtotal >= 0),
    CONSTRAINT CK_Venta_Descuento CHECK (Descuento >= 0 AND Descuento <= Subtotal),
    CONSTRAINT CK_Venta_Total     CHECK (Total = Subtotal - Descuento),
    CONSTRAINT CK_Venta_Motivo    CHECK (Descuento = 0 OR MotivoDescuento IS NOT NULL),
    CONSTRAINT CK_Venta_Consumo   CHECK (
        Tipo = 'Venta' OR (Descuento = 0 AND EfectivoRecibido IS NULL)),
    CONSTRAINT CK_Venta_Cambio CHECK (
        (EfectivoRecibido IS NULL AND Cambio IS NULL) OR
        (EfectivoRecibido > 0 AND Cambio >= 0 AND Cambio < EfectivoRecibido)),
    CONSTRAINT FK_Venta_Corte   FOREIGN KEY (CorteId)   REFERENCES CorteCaja (CorteId),
    CONSTRAINT FK_Venta_Usuario FOREIGN KEY (UsuarioId) REFERENCES Usuario (UsuarioId)
);

-- Pagos de cada venta (permite pago mixto)
CREATE TABLE Pago (
    PagoId     INT AUTO_INCREMENT PRIMARY KEY,
    VentaId    INT           NOT NULL,
    MetodoPago ENUM('Efectivo','Tarjeta','Transferencia') NOT NULL,
    Monto      DECIMAL(10,2) NOT NULL,
    CONSTRAINT UQ_Pago_Venta_Metodo UNIQUE (VentaId, MetodoPago),
    CONSTRAINT CK_Pago_Monto CHECK (Monto > 0),
    CONSTRAINT FK_Pago_Venta FOREIGN KEY (VentaId)
        REFERENCES Venta (VentaId) ON DELETE CASCADE
);

-- Renglones de cada venta: un platillo del menú O un artículo suelto
CREATE TABLE DetalleVenta (
    DetalleVentaId INT AUTO_INCREMENT PRIMARY KEY,
    VentaId        INT           NOT NULL,
    ProductoId     INT           NULL,     -- si es platillo del menú
    ArticuloId     INT           NULL,     -- si es venta suelta
    Cantidad       INT           NOT NULL,
    PrecioUnitario DECIMAL(10,2) NOT NULL,
    Subtotal       DECIMAL(10,2) NOT NULL,
    Nota           VARCHAR(150)  NULL,     -- indicaciones: "sin mayonesa", "sin chile"
    CONSTRAINT CK_Detalle_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_Detalle_Precio   CHECK (PrecioUnitario >= 0),
    CONSTRAINT CK_Detalle_Subtotal CHECK (Subtotal = Cantidad * PrecioUnitario),
    CONSTRAINT CK_Detalle_Origen CHECK (
        (ProductoId IS NOT NULL AND ArticuloId IS NULL) OR
        (ProductoId IS NULL     AND ArticuloId IS NOT NULL)),
    CONSTRAINT FK_Detalle_Venta    FOREIGN KEY (VentaId)
        REFERENCES Venta (VentaId) ON DELETE CASCADE,
    CONSTRAINT FK_Detalle_Producto FOREIGN KEY (ProductoId) REFERENCES Producto (ProductoId),
    CONSTRAINT FK_Detalle_Articulo FOREIGN KEY (ArticuloId) REFERENCES Articulo (ArticuloId)
);

-- Lo que el cliente eligió para su platillo (papitas, sopa, etc.)
CREATE TABLE DetalleVentaArticulo (
    DetalleVentaId INT NOT NULL,
    ArticuloId     INT NOT NULL,
    Cantidad       INT NOT NULL DEFAULT 1,
    CONSTRAINT PK_DetalleVentaArticulo PRIMARY KEY (DetalleVentaId, ArticuloId),
    CONSTRAINT CK_DVA_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT FK_DVA_Detalle  FOREIGN KEY (DetalleVentaId)
        REFERENCES DetalleVenta (DetalleVentaId) ON DELETE CASCADE,
    CONSTRAINT FK_DVA_Articulo FOREIGN KEY (ArticuloId) REFERENCES Articulo (ArticuloId)
);

-- Historial de todo lo que entra y sale del inventario
CREATE TABLE MovimientoInventario (
    MovimientoId  INT AUTO_INCREMENT PRIMARY KEY,
    ArticuloId    INT          NOT NULL,
    Tipo          ENUM('Inicial','Entrada','Venta','ConsumoPersonal',
                       'Uso','Merma','Ajuste','Cancelacion') NOT NULL,
    Cantidad      INT          NOT NULL,   -- positiva suma, negativa resta
    StockAnterior INT          NULL,       -- los llena un trigger (auditoría)
    StockNuevo    INT          NULL,
    Fecha         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UsuarioId     INT          NOT NULL,
    ProveedorId   INT          NULL,       -- solo en Entrada
    VentaId       INT          NULL,       -- solo si viene de una venta
    Observaciones VARCHAR(200) NULL,
    CONSTRAINT CK_Mov_Cantidad CHECK (Cantidad <> 0),
    CONSTRAINT CK_Mov_Signo CHECK (
        (Tipo IN ('Inicial','Entrada','Cancelacion')           AND Cantidad > 0) OR
        (Tipo IN ('Venta','ConsumoPersonal','Uso','Merma')     AND Cantidad < 0) OR
        (Tipo = 'Ajuste')),
    CONSTRAINT CK_Mov_Proveedor CHECK (ProveedorId IS NULL OR Tipo = 'Entrada'),
    CONSTRAINT CK_Mov_Venta CHECK (
        (Tipo IN ('Venta','ConsumoPersonal','Cancelacion') AND VentaId IS NOT NULL) OR
        (Tipo NOT IN ('Venta','ConsumoPersonal','Cancelacion') AND VentaId IS NULL)),
    CONSTRAINT FK_Mov_Articulo  FOREIGN KEY (ArticuloId)  REFERENCES Articulo (ArticuloId),
    CONSTRAINT FK_Mov_Usuario   FOREIGN KEY (UsuarioId)   REFERENCES Usuario (UsuarioId),
    CONSTRAINT FK_Mov_Proveedor FOREIGN KEY (ProveedorId) REFERENCES Proveedor (ProveedorId),
    CONSTRAINT FK_Mov_Venta     FOREIGN KEY (VentaId)     REFERENCES Venta (VentaId)
);

-- Índices para las búsquedas más frecuentes
CREATE INDEX IX_Venta_Fecha        ON Venta (Fecha);
CREATE INDEX IX_Mov_Articulo_Fecha ON MovimientoInventario (ArticuloId, Fecha);