@echo off
REM PurrNet Relay Server Startup Script for Windows
REM Usage: start-server.bat [port] [max-rooms] [tick-rate]

set PORT=%1
if "%PORT%"=="" set PORT=9050

set MAX_ROOMS=%2
if "%MAX_ROOMS%"=="" set MAX_ROOMS=1000

set TICK_RATE=%3
if "%TICK_RATE%"=="" set TICK_RATE=30

echo Starting PurrNet Relay Server...
echo Port: %PORT%
echo Max Rooms: %MAX_ROOMS%
echo Tick Rate: %TICK_RATE% Hz
echo.

dotnet run -- --port %PORT% --max-rooms %MAX_ROOMS% --tick-rate %TICK_RATE%

