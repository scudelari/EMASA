@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 30296)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 32520)

del /F cleanup-ansys-MININT-KQLAIS1-32520.bat
