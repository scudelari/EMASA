@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 32804)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 25976)

del /F cleanup-ansys-MININT-KQLAIS1-25976.bat
