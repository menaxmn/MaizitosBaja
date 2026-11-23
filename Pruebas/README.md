# Pruebas

`Matriz_Pruebas.xlsx` contiene 35 casos con todos los campos (identificador, requisito/regla, precondición, entrada, pasos, esperado, observado, estado) y una hoja de incidencias.

- **PS-01 a PS-20**: pruebas a nivel de base de datos, ejecutadas en MySQL 8.0.46 (22-nov-2026) con resultado real.
- **PU-01 a PU-15**: pruebas de interfaz. **Pendientes**: ejecutarlas en la aplicación, llenar “Resultado observado” y “Estado” y pegar las capturas en el reporte.

Si una prueba falla, no se borra: se registra como incidencia con su corrección.
