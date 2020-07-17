@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 32008)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 6780)

del /F cleanup-ansys-MININT-KQLAIS1-6780.bat
