@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 22976)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 17940)

del /F cleanup-ansys-MININT-KQLAIS1-17940.bat
