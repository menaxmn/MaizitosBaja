# Instalar Maizitos Baja en esta computadora

Requisitos: Windows, MySQL con la base `MaizitosBaja` creada (scripts de `Sql/`)
y el SDK de .NET 10 (viene con Visual Studio).

1. Doble clic en **Instalar-en-mi-computadora.bat**.
2. La primera vez se abre el Bloc de notas: escribe la contraseña de MySQL,
   guarda (Ctrl+S) y cierra.
3. Abre **Maizitos Baja** desde el escritorio.

- El sistema queda en `C:\MaizitosBaja\App` y se usa en `http://localhost:5050`.
- Se abre en una ventana sin barra de direcciones. Si das doble clic otra vez
  al ícono estando abierto, solo se vuelve a mostrar la ventana.
- Para apagarlo: botón **Cerrar sistema**, abajo en la barra lateral.
- Para actualizarlo después de cambiar el código: vuelve a correr el `.bat`.
  La contraseña ya no se pide (se queda en `appsettings.Production.json`).
- `appsettings.Production.json` tiene la contraseña: **no se sube al repositorio**
  (está en `.gitignore`).
