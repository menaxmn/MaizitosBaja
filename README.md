# Maizitos Baja 🌽

Punto de venta e inventario para un local de elotes, esquites y snacks.
**Proyecto Final de Bases de Datos** · UABC, Ingeniería en Software y Tecnologías Emergentes · Grupo 952 · Docente: Julia Guadalupe Juarez Hernandez

| Integrante | Matrícula | Rol |
|---|---|---|
| Fernando Alberto González Borbas | 379792 | Líder / Backend |
| Mia Citlalli Herrera Contreras | 2210784 | Base de datos |
| Ximena Antonia Reséndiz Félix | 2218417 | Frontend |

La aplicación (ASP.NET Core Razor Pages) opera sobre una base de datos relacional **MySQL 8** de 11 tablas. Las reglas críticas (caja abierta, stock no negativo, pagos que cuadran, corte cerrado inmutable) se hacen cumplir en la propia base de datos con restricciones, triggers y procedimientos almacenados.

## Estructura del repositorio

```
Aplicacion/MaizitosBaja/   Solución .NET (Web, Negocios, Datos, Entidades)
Sql/                       01_esquema · 02_datos · 03_consultas_vistas · 04_rutinas
Diagramas/                 DER en 5 notaciones (PNG/PDF) + fuentes .dot y script generador
Reporte/                   Reporte.pdf y Reporte.docx (editable)
Pruebas/                   Matriz_Pruebas.xlsx (casos, resultados e incidencias)
Presentacion/              Presentacion.pdf y .pptx
Video/                     Guion del video demostrativo
```

## Requisitos

- **MySQL 8.0+** (verificado con 8.0.46) y MySQL Workbench o el cliente `mysql`.
- **.NET SDK 10** (`dotnet --version`).
- Git.

## Instalación y configuración

### 1. Base de datos

Ejecutar los scripts **en este orden** (el 01 borra y recrea la base `MaizitosBaja`):

```bash
mysql -u root -p < Sql/01_esquema.sql
mysql -u root -p < Sql/02_datos.sql
mysql -u root -p < Sql/03_consultas_vistas.sql
mysql -u root -p --force < Sql/04_rutinas.sql
```

> `04_rutinas.sql` incluye **pruebas inválidas que provocan errores a propósito** (11 en total). En Workbench activa “continuar después de errores” o ejecuta cada prueba por separado; en consola usa `--force`.
> Para reiniciar la base se vuelve a correr 01 → 04. El script 04 abre un corte con la fecha del día, por lo que debe ejecutarse en una base recién creada.

Usuario recomendado para la aplicación (en lugar de `root`):

```sql
CREATE USER 'maizitos_app'@'localhost' IDENTIFIED BY 'cambia_esta_contraseña';
GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON MaizitosBaja.* TO 'maizitos_app'@'localhost';
```

### 2. Cadena de conexión (User Secrets, sin contraseñas en el repositorio)

```bash
cd Aplicacion/MaizitosBaja/MaizitosBaja.Web
dotnet user-secrets set "ConnectionStrings:MaizitosBaja" "Server=localhost;Port=3306;Database=MaizitosBaja;User ID=maizitos_app;Password=cambia_esta_contraseña;"
```

### 3. Ejecutar la aplicación

```bash
cd Aplicacion/MaizitosBaja/MaizitosBaja.Web
dotnet run --launch-profile http      # http://localhost:5111
```

Los datos del ticket (nombre, teléfono, redes) se configuran en la sección `Negocio` de `appsettings.json`.

## Módulos de la aplicación

| Módulo | Qué hace |
|---|---|
| Vender | Orden con platillos y artículos sueltos, artículo elegido, notas, descuento, consumo personal y pago mixto (`sp_RegistrarVenta`) |
| Ticket | Ticket de la venta |
| Caja | Abrir/cerrar corte (`sp_AbrirCorte`, `sp_CerrarCorte`), resumen (`vw_ResumenCorte`) y cancelar ventas (`sp_CancelarVenta`) |
| Productos · Inventario · Proveedores | CRUD con baja lógica, búsqueda y filtros |
| Movimientos | Entrada, salida, ajuste e historial con filtros |

## Base de datos en pocas palabras

- **11 tablas**: Categoria, Proveedor, Usuario, Articulo, Producto, CorteCaja, Venta, Pago, DetalleVenta, DetalleVentaArticulo, MovimientoInventario.
- **2 vistas**: `vw_InventarioAlertas`, `vw_ResumenCorte`.
- **4 triggers**: `trg_Movimiento_BI`, `trg_Movimiento_AI` (único lugar donde cambia el stock), `trg_Venta_BI`, `trg_Corte_BU`.
- **1 función** `fn_EfectivoEsperado` y **4 procedimientos** (`sp_AbrirCorte`, `sp_RegistrarVenta`, `sp_CancelarVenta`, `sp_CerrarCorte`).
- 10 consultas documentadas, transacciones COMMIT/ROLLBACK, 23 `CHECK`, 15 claves foráneas.

Todo está explicado en `Reporte/Reporte.pdf`.

## Diagramas

`Diagramas/` contiene el DER en **Crow's Foot, Classic, Connect to Columns, UML e IDEF1X** (PNG y PDF), generados desde la base real. Para tener el modelo editable en MySQL Workbench: *File → Import → Reverse Engineer MySQL Create Script* con `Sql/01_esquema.sql` y luego *Model → Relationship Notation*.

## Respaldo

```bash
mysqldump -u root -p --routines --triggers --single-transaction MaizitosBaja > respaldo.sql
```

## Datos de prueba

Todos los datos de `02_datos.sql` son ficticios (proveedores genéricos, un solo usuario `admin`).
