@echo off
chcp 65001 >nul
title StreamOps - Khoi chay he thong quan ly Streamer Operations & Analytics
echo =====================================================================
echo           STREAMOPS: STREAMER OPERATIONS & ANALYTICS SYSTEM          
echo                     Ket noi Neon Cloud PostgreSQL                    
echo =====================================================================
echo.

cd /d "%~dp0"

echo [1/3] Khoi dong Backend API (.NET 9) tai cong 5081...
start "StreamOps API (.NET 9)" cmd /k "dotnet run --project src/api/StreamOps.Api --urls http://127.0.0.1:5081"

echo [2/3] Khoi dong Frontend Web (Vite + React) tai cong 5174...
start "StreamOps Web (React + Vite)" cmd /k "cd src/web && npm run dev -- --host 127.0.0.1 --port 5174"

echo [3/3] Cho he thong san sang va mo trinh duyet...
timeout /t 3 /nobreak >nul
start http://127.0.0.1:5174/

echo.
echo =====================================================================
echo He thong da khoi dong thanh cong!
echo - Web Dashboard: http://127.0.0.1:5174/
echo - Backend API:   http://127.0.0.1:5081/api/v1
echo - Database:      Neon PostgreSQL (STREAMOPS production branch)
echo.
echo Tai khoan Admin dang nhap:
echo - Email:    admin@streamops.local
echo - Mat khau: StreamOps@2026!
echo =====================================================================
pause
