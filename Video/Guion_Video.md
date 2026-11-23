# Guion del video demostrativo (5–8 min)

Graben pantalla con la app y MySQL Workbench abiertos. Reparto sugerido: **F** = Fernando, **M** = Mia, **X** = Ximena. Cada quien habla su parte (el video no sustituye la defensa presencial).

Preparación antes de grabar: base recreada con 01 → 03 (+ 04 si se quiere mostrar el cierre), app corriendo, caja abierta, Workbench con `Sql/04_rutinas.sql` abierto.

| Tiempo | Quién | Qué mostrar | Qué decir |
|---|---|---|---|
| 0:00–0:30 | F | Portada / README | Nombre del proyecto, equipo y problema: ventas anotadas a mano, pagos mixtos, inventario sin control. |
| 0:30–1:15 | M | Diagrama Crow's Foot (`Diagramas/01_DER_CrowsFoot.png`) | 11 entidades, relaciones principales; DetalleVentaArticulo es N:M; Pago y DetalleVenta dependen de Venta. Mencionar que existen las 5 notaciones. |
| 1:15–1:50 | M | Reporte, sección 5 (normalización) | Hoja plana → 1FN, 2FN, 3FN; desnormalizaciones controladas (Total, Stock). |
| 1:50–3:30 | X | App: **Vender** → orden con platillo + artículo elegido → pago → **Ticket** | Cinco tablas en una transacción; mostrar el ticket. |
| 3:30–4:00 | X | **Productos**: crear, buscar y desactivar | CRUD con baja lógica para no perder historial. |
| 4:00–4:30 | X | **Movimientos > Historial** con filtros | Reporte multitabla con StockAnterior/StockNuevo. |
| 4:30–5:30 | F | Workbench: `SELECT` de Venta, Pago y MovimientoInventario | Comprobar que lo vendido está en la BD y que el stock bajó una sola vez (trigger `trg_Movimiento_AI`). |
| 5:30–6:30 | F | Workbench: pruebas inválidas I1 (stock insuficiente) e I11 (corte cerrado) | Mostrar el mensaje de error y que no quedó nada guardado. |
| 6:30–7:00 | M | Workbench: vista `vw_ResumenCorte` y una consulta (C8 o C10) | Efectivo esperado y diferencia; consulta con GROUP BY/ventana. |
| 7:00–7:40 | X | Cerrar y reabrir la app; recargar Productos/Caja | Persistencia: los datos siguen ahí y coinciden con la BD. |
| 7:40–8:00 | F | Cierre | Resumen, incidencias resueltas y aprendizaje. |

Consejos: resolución legible (zoom al 125 %), cerrar notificaciones, ensayar una vez y mantenerse entre 5 y 8 minutos.
