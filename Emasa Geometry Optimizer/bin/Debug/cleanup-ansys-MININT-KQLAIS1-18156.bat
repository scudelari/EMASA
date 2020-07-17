@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 30556)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 18156)

del /F cleanup-ansys-MININT-KQLAIS1-18156.bat
