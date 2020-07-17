@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 6116)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 28824)

del /F cleanup-ansys-MININT-KQLAIS1-28824.bat
