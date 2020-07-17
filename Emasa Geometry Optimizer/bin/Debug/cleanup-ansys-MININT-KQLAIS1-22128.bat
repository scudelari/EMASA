@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 31764)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 22128)

del /F cleanup-ansys-MININT-KQLAIS1-22128.bat
