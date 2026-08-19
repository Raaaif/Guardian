@echo off
setlocal
cd /d "%~dp0"
title Guardian - Gerar Executavel

echo ========================================
echo        GUARDIAN - GERAR EXECUTAVEL
echo ========================================
echo.

dotnet publish Guardian.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  /p:PublishSingleFile=true ^
  /p:PublishTrimmed=false

if errorlevel 1 (
  echo.
  echo O build falhou. Veja os erros acima.
  pause
  exit /b 1
)

echo.
echo ========================================
echo BUILD CONCLUIDO COM SUCESSO
echo ========================================
echo.
echo EXE:
echo %CD%\bin\Release\net8.0-windows\win-x64\publish\Guardian.exe
echo.
start "" "%CD%\bin\Release\net8.0-windows\win-x64\publish"
pause
