@echo off
dotnet restore --no-cache
IF ERRORLEVEL 1 GOTO error
dotnet build
IF ERRORLEVEL 1 GOTO error
start InventoryManager.sln
GOTO end

:error
pause
:end
