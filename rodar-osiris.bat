@echo off
setlocal

set "APP_PORT=13453"
set "DB_PORT=13450"
set "APP_ROOT=%~dp0"

echo === Osiris: liberando porta %APP_PORT% ===
call npx --yes kill-port %APP_PORT%

cd /d "%APP_ROOT%"

echo === Osiris: subindo PostgreSQL e Seq ===
docker compose up -d
if errorlevel 1 (
    echo.
    echo Falha ao subir os containers Docker.
    pause
    exit /b 1
)

echo === Osiris: aguardando PostgreSQL em localhost:%DB_PORT% ===
powershell -NoProfile -ExecutionPolicy Bypass -Command "$deadline=(Get-Date).AddSeconds(45); do { try { $tcp=[Net.Sockets.TcpClient]::new('127.0.0.1',%DB_PORT%); $tcp.Dispose(); exit 0 } catch { Start-Sleep -Seconds 1 } } while ((Get-Date) -lt $deadline); exit 1"
if errorlevel 1 (
    echo.
    echo PostgreSQL nao ficou disponivel em localhost:%DB_PORT%.
    pause
    exit /b 1
)

echo === Osiris: aplicando migrations ===
dotnet ef database update --project src/Osiris.Infrastructure --startup-project src/Osiris.Web
if errorlevel 1 (
    echo.
    echo Falha ao aplicar migrations.
    pause
    exit /b 1
)

echo === Osiris: iniciando Web em http://localhost:%APP_PORT% ===
dotnet run --project src/Osiris.Web --launch-profile http

echo.
pause
