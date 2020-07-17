@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 33444)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 28340)

del /F cleanup-ansys-MININT-KQLAIS1-28340.bat
