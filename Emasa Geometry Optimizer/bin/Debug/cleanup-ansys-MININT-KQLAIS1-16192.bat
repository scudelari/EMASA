@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 11332)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 16192)

del /F cleanup-ansys-MININT-KQLAIS1-16192.bat
