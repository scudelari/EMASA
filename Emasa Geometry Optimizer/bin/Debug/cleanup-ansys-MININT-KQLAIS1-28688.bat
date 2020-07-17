@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 29684)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 28688)

del /F cleanup-ansys-MININT-KQLAIS1-28688.bat
