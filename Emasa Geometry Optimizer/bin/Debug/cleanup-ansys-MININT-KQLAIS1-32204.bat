@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 12936)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 32204)

del /F cleanup-ansys-MININT-KQLAIS1-32204.bat
