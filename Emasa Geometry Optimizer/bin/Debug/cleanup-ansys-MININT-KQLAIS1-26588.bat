@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 7544)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 26588)

del /F cleanup-ansys-MININT-KQLAIS1-26588.bat
