@echo off
set LOCALHOST=%COMPUTERNAME%
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 17016)
if /i "%LOCALHOST%"=="MININT-KQLAIS1" (taskkill /f /pid 11692)

del /F cleanup-ansys-MININT-KQLAIS1-11692.bat
