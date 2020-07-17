@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 9240)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 14424)

del /F cleanup-ansys-MININT-KQLAIS1-14424.bat
