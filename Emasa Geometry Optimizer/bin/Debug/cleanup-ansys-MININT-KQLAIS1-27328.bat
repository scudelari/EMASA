@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 24424)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 27328)

del /F cleanup-ansys-MININT-KQLAIS1-27328.bat
