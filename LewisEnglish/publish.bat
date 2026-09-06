@echo off
REM ============================================================
REM  Lewis, English Speaking Coach - Generar ejecutable (.exe)
REM ============================================================
echo Publicando Lewis, English Speaking Coach...
echo.

dotnet publish LewisEnglish.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -o publish

echo.
if %ERRORLEVEL% EQU 0 (
    echo ============================================================
    echo  Listo. El ejecutable esta en la carpeta:  publish\LewisEnglish.exe
    echo ============================================================
) else (
    echo Ocurrio un error durante la publicacion.
)
pause
