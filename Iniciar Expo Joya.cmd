@echo off
setlocal
set "APPDIR=%~dp0"
if not exist "%APPDIR%CorreosOutlook.WinForms.exe" set "APPDIR=%~dp0artifacts\publish\win-x64\"
if not exist "%APPDIR%CorreosOutlook.WinForms.exe" (
  echo No se encontro la aplicacion publicada.
  echo Extraiga el ZIP completo antes de abrir este archivo.
  echo En el proyecto, consulte INICIO_RAPIDO.md para generar la publicacion.
  pause
  exit /b 1
)
if /i "%~1"=="--smoke-test" (
  start "" /wait /D "%APPDIR%" "%APPDIR%CorreosOutlook.WinForms.exe" --smoke-test
  if errorlevel 1 exit /b 1
  exit /b 0
)
start "" /D "%APPDIR%" "%APPDIR%CorreosOutlook.WinForms.exe"
exit /b 0
