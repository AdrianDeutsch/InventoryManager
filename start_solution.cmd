@echo off
dotnet tool install Microsoft.dotnet-interactive --version 1.0.712001
dotnet restore --no-cache
IF ERRORLEVEL 1 GOTO error
ren nuget.config.lab nuget.config
dotnet restore --no-cache
IF ERRORLEVEL 1 GOTO error
dotnet build
IF ERRORLEVEL 1 GOTO error
start InventoryManager.sln
GOTO end

:error
pause
:end
