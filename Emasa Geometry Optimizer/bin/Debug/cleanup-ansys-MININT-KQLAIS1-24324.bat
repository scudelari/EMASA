@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 26240)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 24324)

del /F cleanup-ansys-MININT-KQLAIS1-24324.bat
