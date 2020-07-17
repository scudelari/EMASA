@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 24516)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 32592)

del /F cleanup-ansys-MININT-KQLAIS1-32592.bat
