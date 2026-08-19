@echo off
setlocal
cd /d "%~dp0"

title Guardian - Gerar Instalador

echo ========================================
echo       GUARDIAN - INSTALADOR
echo ========================================
echo.

echo [1/2] Gerando executavel...
call "..\publish.bat"
if errorlevel 1 (
  echo.
  echo Falha ao gerar o executavel.
  pause
  exit /b 1
)

echo.
echo [2/2] Compilando instalador...

set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"

if not exist "%ISCC%" (
  echo.
  echo Inno Setup 6 nao foi encontrado.
  echo Instale o Inno Setup e tente novamente.
  echo.
  echo Depois da instalacao, execute este arquivo outra vez.
  pause
  exit /b 1
)

"%ISCC%" "Guardian.iss"
if errorlevel 1 (
  echo.
  echo Falha ao compilar o instalador.
  pause
  exit /b 1
)

echo.
echo ========================================
echo INSTALADOR GERADO COM SUCESSO
echo ========================================
echo.
echo Arquivo:
echo %CD%\output\Guardian_Setup_v7_1.exe
echo.
start "" "%CD%\output"
pause
