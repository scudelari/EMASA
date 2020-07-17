@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 23912)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 13204)

del /F cleanup-ansys-MININT-KQLAIS1-13204.bat
