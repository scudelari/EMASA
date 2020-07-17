@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 2000)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 30524)

del /F cleanup-ansys-MININT-KQLAIS1-30524.bat
