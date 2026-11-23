@echo off
setlocal
title Maizitos Baja - Instalar en esta computadora

rem ===================================================================
rem  Publica el sistema en C:\MaizitosBaja\App y crea el acceso directo
rem  "Maizitos Baja" en el escritorio. Se puede correr las veces que
rem  quieras: cada vez instala la version mas nueva del codigo.
rem ===================================================================

set "DESTINO=C:\MaizitosBaja\App"
set "PROYECTO=%~dp0..\Aplicacion\MaizitosBaja\MaizitosBaja.Web\MaizitosBaja.Web.csproj"

echo.
echo  1/4  Cerrando el sistema si estaba abierto...
taskkill /IM MaizitosBaja.Web.exe /F >nul 2>&1
timeout /t 1 /nobreak >nul

echo  2/4  Publicando (esto tarda un poco la primera vez)...
dotnet publish "%PROYECTO%" -c Release -r win-x64 --self-contained true -o "%DESTINO%" --nologo -v quiet
if errorlevel 1 (
    echo.
    echo  ERROR: no se pudo publicar. Revisa que el proyecto compile en Visual Studio.
    pause
    exit /b 1
)

echo  3/4  Revisando la configuracion de la base de datos...
if not exist "%DESTINO%\appsettings.Production.json" (
    copy "%~dp0appsettings.Production.ejemplo.json" "%DESTINO%\appsettings.Production.json" >nul
    echo.
    echo  Se abrira el Bloc de notas: escribe la contrasena de MySQL donde dice
    echo  ESCRIBE_AQUI_TU_CONTRASENA, guarda con Ctrl+S y cierra el Bloc de notas.
    echo.
    pause
    notepad "%DESTINO%\appsettings.Production.json"
)

echo  4/4  Creando el acceso directo en el escritorio...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$e=[Environment]::GetFolderPath('Desktop'); $s=(New-Object -ComObject WScript.Shell).CreateShortcut((Join-Path $e 'Maizitos Baja.lnk')); $s.TargetPath='%DESTINO%\MaizitosBaja.Web.exe'; $s.WorkingDirectory='%DESTINO%'; $s.IconLocation='%DESTINO%\MaizitosBaja.Web.exe,0'; $s.Description='Punto de venta Maizitos Baja'; $s.Save()"

echo.
echo  Listo. Abre "Maizitos Baja" desde el escritorio.
echo.
pause
