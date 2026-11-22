@echo off
chcp 65001 >nul
title StreamOps - Dung he thong
echo Dang dung cac tien trinh StreamOps tren cong 5081 va 5174...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5081" ^| findstr "LISTENING"') do (
    taskkill /F /PID %%a >nul 2>&1
)
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5174" ^| findstr "LISTENING"') do (
    taskkill /F /PID %%a >nul 2>&1
)
echo Da tat cac tien trinh StreamOps thanh cong!
pause
