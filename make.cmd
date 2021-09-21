@echo off
cls
TITLE Make of AutoBUS services

set mypath=%~dp0
set make=%mypath%make

set logfile=%make%\makeErrors.log

taskkill /F /IM dotnet.exe /FI "WINDOWTITLE eq AutoBUS*.*"

mkdir %make%\
rmdir %make%\ /s /q

REM Main

mkdir %make%\AutoBUS.Main\
rmdir %mypath%AutoBUS.Main\bin /s /q
rmdir %mypath%AutoBUS.Main\obj /s /q
echo.
echo Publication of Main
cd %mypath%AutoBUS.Main\
dotnet publish -c Release -o "%make%\AutoBUS.Main\bin\" | findstr /c:"error CS">%logfile%
if "!errorlevel!"=="0" (
 echo ----------------------------------------------------
 echo ERREUR DE PUBLICATION - AutoBUS.Main
 echo ----------------------------------------------------
 type %logfile%
 del %logfile%
 rmdir %make%\ /s /q
 GOTO:EOF
)

cd %mypath%

echo.
echo Copie Main Installer
xcopy %mypath%AutoBUS.Main\Installer\*.cmd %make%\AutoBUS.Main\ /B/V/Y/E
xcopy %mypath%AutoBUS.Main\Installer\*.sh %make%\AutoBUS.Main\ /B/V/Y/E
xcopy %mypath%AutoBUS.Main\Installer\*.service %make%\AutoBUS.Main\ /B/V/Y/E

REM Worker

mkdir %make%\AutoBUS.Worker\
rmdir %mypath%AutoBUS.Worker\bin /s /q
rmdir %mypath%AutoBUS.Worker\obj /s /q
echo.
echo Publication of Worker
cd %mypath%AutoBUS.Worker\
dotnet publish -c Release -o "%make%\AutoBUS.Worker\bin\" | findstr /c:"error CS">%logfile%
if "!errorlevel!"=="0" (
 echo ----------------------------------------------------
 echo ERREUR DE PUBLICATION - AutoBUS.Worker
 echo ----------------------------------------------------
 type %logfile%
 del %logfile%
 rmdir %make%\ /s /q
 GOTO:EOF
)

cd %mypath%

echo.
echo Copie Worker Installer
xcopy %mypath%AutoBUS.Worker\Installer\*.cmd %make%\AutoBUS.Worker\ /B/V/Y/E
xcopy %mypath%AutoBUS.Worker\Installer\*.sh %make%\AutoBUS.Worker\ /B/V/Y/E
xcopy %mypath%AutoBUS.Worker\Installer\*.service %make%\AutoBUS.Worker\ /B/V/Y/E

del %logfile%


xcopy %mypath%README.md %make%\ /B/V/Y/E
