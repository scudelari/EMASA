@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 26688)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 22124)

del /F cleanup-ansys-MININT-KQLAIS1-22124.bat
