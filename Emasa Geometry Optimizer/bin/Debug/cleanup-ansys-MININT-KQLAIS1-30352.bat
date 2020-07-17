@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 25384)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 30352)

del /F cleanup-ansys-MININT-KQLAIS1-30352.bat
