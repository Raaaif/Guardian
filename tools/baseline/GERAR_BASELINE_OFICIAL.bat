@echo off
setlocal
cd /d "%~dp0"
title Guardian - Gerar Baseline Oficial

echo ========================================
echo  GUARDIAN - BASELINE OFICIAL CS 1.6
echo ========================================
echo.
echo Antes de continuar:
echo 1. Abra a Steam.
echo 2. Counter-Strike ^> Propriedades.
echo 3. Arquivos instalados.
echo 4. Verificar integridade dos arquivos.
echo 5. Feche o CS e aguarde a verificacao terminar.
echo.
pause

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0gerar_baseline_oficial.ps1"

if errorlevel 1 (
  echo.
  echo Falha ao gerar a baseline.
  pause
  exit /b 1
)

echo.
echo Baseline concluida.
pause
