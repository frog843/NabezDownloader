@echo off
setlocal enabledelayedexpansion
set TARGET=%~1
set NEW=%~2

if "%TARGET%"=="" exit /b 1
if "%NEW%"=="" exit /b 1

set COUNT=0
:wait
tasklist /fi "imagename eq %~nx1" | find /i "%~nx1" >nul
if not errorlevel 1 (
    if %COUNT% lss 40 (
        timeout /t 1 /nobreak >nul
        set /a COUNT+=1
        goto wait
    )
)

if exist "%TARGET%" del /f /q "%TARGET%"
move /y "%NEW%" "%TARGET%" >nul

if exist "%TARGET%" (
    start "" "%TARGET%"
)
exit /b 0
